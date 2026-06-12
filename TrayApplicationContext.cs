namespace BackgroundMute;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private readonly MainForm _mainForm;
    private readonly AudioSessionService _audioService;
    private readonly ForegroundWatcher _foregroundWatcher;
    private readonly SettingsStore _settingsStore;
    private readonly System.Windows.Forms.Timer _maintenanceTimer;
    private readonly System.Windows.Forms.Timer _foregroundSettleTimer;
    private HashSet<string> _selectedProgramKeys;
    private bool _isExiting;

    private static Icon LoadApplicationIcon()
    {
        try
        {
            // Get the directory where the executable is located
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var exeDirectory = Path.GetDirectoryName(assembly.Location);
            
            // Load icon from exe directory
            string iconPath = Path.Combine(exeDirectory ?? "", "BackgroundMute.ico");
            if (File.Exists(iconPath))
            {
                return new Icon(iconPath);
            }
        }
        catch
        {
            // Fallback to system icon if load fails
        }
        return SystemIcons.Application;
    }

    public TrayApplicationContext()
    {
        _audioService = new AudioSessionService();
        _foregroundWatcher = new ForegroundWatcher();
        _settingsStore = new SettingsStore();
        _selectedProgramKeys = _settingsStore.LoadSelectedPrograms();

        _mainForm = new MainForm();
        MainForm = _mainForm;
        _mainForm.Icon = LoadApplicationIcon();
        _mainForm.FormClosing += OnMainFormFormClosing;
        _mainForm.SelectionChanged += OnSelectionChanged;
        _mainForm.RefreshRequested += RefreshProgramList;

        var menu = new ContextMenuStrip();
        menu.Items.Add("Open", null, (_, _) => ShowMainForm());
        menu.Items.Add("Exit", null, (_, _) => ExitThread());

        _notifyIcon = new NotifyIcon
        {
            Icon = LoadApplicationIcon(),
            Text = "BackgroundMute",
            Visible = true,
            ContextMenuStrip = menu
        };
        _notifyIcon.MouseClick += OnTrayIconClick;

        _foregroundWatcher.ForegroundChanged += OnForegroundChanged;

        _foregroundSettleTimer = new System.Windows.Forms.Timer
        {
            Interval = 350
        };
        _foregroundSettleTimer.Tick += (_, _) =>
        {
            _foregroundSettleTimer.Stop();
            try
            {
                ApplyPolicyForCurrentForeground();
            }
            catch
            {
                // Keep tray process alive if settle pass fails.
            }
        };

        _maintenanceTimer = new System.Windows.Forms.Timer
        {
            Interval = 3000
        };
        _maintenanceTimer.Tick += (_, _) =>
        {
            try
            {
                ApplyPolicyForCurrentForeground();
                if (_mainForm.Visible)
                {
                    RefreshProgramList();
                }
            }
            catch
            {
                // Keep the tray process alive even if one maintenance pass fails.
            }
        };
        _maintenanceTimer.Start();

        RefreshProgramList();
        ApplyPolicyForCurrentForeground();
    }

    protected override void ExitThreadCore()
    {
        _isExiting = true;
        _foregroundSettleTimer.Stop();
        _foregroundSettleTimer.Dispose();
        _maintenanceTimer.Stop();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _foregroundWatcher.Dispose();
        _audioService.RestoreAllMutedSessions();
        _mainForm.Close();
        _mainForm.Dispose();
        base.ExitThreadCore();
    }

    private void OnMainFormFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_isExiting)
        {
            return;
        }

        e.Cancel = true;
        _mainForm.Hide();
    }

    private void ShowMainForm()
    {
        RefreshProgramList();

        var cursor = Cursor.Position;
        _mainForm.Location = new Point(
            Math.Max(0, cursor.X - (_mainForm.Width / 2)),
            Math.Max(0, cursor.Y - _mainForm.Height - 12));

        _mainForm.Show();
        _mainForm.Activate();
    }

    private void OnTrayIconClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            if (_mainForm.Visible)
            {
                _mainForm.Hide();
            }
            else
            {
                ShowMainForm();
            }
        }
    }

    private void OnSelectionChanged(HashSet<string> selectedProgramKeys)
    {
        _selectedProgramKeys = selectedProgramKeys;
        _settingsStore.SaveSelectedPrograms(_selectedProgramKeys);
        ApplyPolicyForCurrentForeground();
    }

    private void RefreshProgramList()
    {
        var programs = _audioService.GetProgramEntries();
        _mainForm.SetPrograms(programs, _selectedProgramKeys);
    }

    private void OnForegroundChanged(int processId)
    {
        if (!_mainForm.IsHandleCreated)
        {
            ApplyPolicyForCurrentForeground();
            return;
        }

        _mainForm.BeginInvoke(() =>
        {
            try
            {
                // Re-read actual foreground at handling time instead of trusting the event PID.
                ApplyPolicyForCurrentForeground();
                _foregroundSettleTimer.Stop();
                _foregroundSettleTimer.Start();
            }
            catch
            {
                // Keep tray process alive if one foreground callback fails.
            }
        });
    }

    private void ApplyPolicyForCurrentForeground()
    {
        var processId = _foregroundWatcher.GetCurrentForegroundProcessId();
        var foregroundIdentity = _audioService.TryGetIdentityKeyForProcess(processId);
        _audioService.ApplyMutePolicy(_selectedProgramKeys, foregroundIdentity);
    }
}

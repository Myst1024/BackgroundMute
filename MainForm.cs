using Microsoft.Win32;

namespace BackgroundMute;

internal sealed class MainForm : Form
{
    private readonly CheckedListBox _programList;
    private readonly Button _refreshButton;
    private readonly Label _hintLabel;
    private readonly Label _countLabel;
    private readonly TableLayoutPanel _rootPanel;
    private readonly TableLayoutPanel _footerPanel;
    private readonly CheckBox _autorunCheckBox;

    private bool _isDarkTheme;

    public event Action<HashSet<string>>? SelectionChanged;
    public event Action? RefreshRequested;

    public MainForm()
    {
        Text = "BackgroundMute";
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = true;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        MinimumSize = new Size(1080, 840);
        ClientSize = new Size(1240, 1000);
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        Icon = null; // Will be set by TrayApplicationContext

        _rootPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 1,
            RowCount = 3
        };
        _rootPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _rootPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _rootPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _hintLabel = new Label
        {
            AutoSize = true,
            Text = "Check programs to auto-mute when they are in the background.",
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 8)
        };

        _programList = new CheckedListBox
        {
            CheckOnClick = true,
            Dock = DockStyle.Fill,
            IntegralHeight = false,
            ItemHeight = 24,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point),
            Padding = new Padding(4)
        };
        _programList.ItemCheck += OnItemCheck;

        _footerPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            AutoSize = true,
            Margin = new Padding(0, 8, 0, 0)
        };
        _footerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _footerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _footerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _footerPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _countLabel = new Label
        {
            AutoSize = true,
            Text = "0 programs",
            Margin = new Padding(0, 0, 12, 0)
        };

        _refreshButton = new Button
        {
            Text = "Refresh",
            Width = 90,
            Height = 28,
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Padding = new Padding(8, 2, 8, 2)
        };
        _refreshButton.FlatAppearance.BorderSize = 1;
        _refreshButton.Click += (_, _) => RefreshRequested?.Invoke();

        _autorunCheckBox = new CheckBox
        {
            AutoSize = true,
            Text = "Run on startup",
            Cursor = Cursors.Hand
        };
        _autorunCheckBox.CheckedChanged += OnAutorunCheckChanged;

        var buttonContainer = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        buttonContainer.Controls.Add(_countLabel);
        buttonContainer.Controls.Add(_refreshButton);

        _footerPanel.Controls.Add(buttonContainer, 0, 0);
        _footerPanel.Controls.Add(_autorunCheckBox, 2, 0);

        _rootPanel.Controls.Add(_hintLabel, 0, 0);
        _rootPanel.Controls.Add(_programList, 0, 1);
        _rootPanel.Controls.Add(_footerPanel, 0, 2);
        Controls.Add(_rootPanel);

        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        ApplyThemeFromSystem();
        
        // Initialize autorun checkbox state
        _autorunCheckBox.Checked = IsAutorunEnabled();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        // Ensure autorun state is synced when form is first shown
        _autorunCheckBox.Checked = IsAutorunEnabled();
    }

    private bool IsAutorunEnabled()
    {
        try
        {
            using (var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", false))
            {
                return key?.GetValue("BackgroundMute") != null;
            }
        }
        catch
        {
            return false;
        }
    }

    private void SetAutorunEnabled(bool enabled)
    {
        try
        {
            using (var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", true))
            {
                if (key == null)
                    return;

                if (enabled)
                {
                    key.SetValue("BackgroundMute", Application.ExecutablePath, RegistryValueKind.String);
                }
                else
                {
                    key.DeleteValue("BackgroundMute", false);
                }
            }
        }
        catch
        {
            // Silently fail if we can't write to registry
        }
    }

    private void OnAutorunCheckChanged(object? sender, EventArgs e)
    {
        SetAutorunEnabled(_autorunCheckBox.Checked);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        }

        base.Dispose(disposing);
    }

    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);
        Hide();
    }

    private void OnUserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category is UserPreferenceCategory.General
            or UserPreferenceCategory.Color
            or UserPreferenceCategory.VisualStyle)
        {
            if (IsHandleCreated)
            {
                BeginInvoke(ApplyThemeFromSystem);
            }
        }
    }

    private void ApplyThemeFromSystem()
    {
        var wantsDarkTheme = IsDarkThemeEnabled();
        if (wantsDarkTheme == _isDarkTheme)
        {
            return;
        }

        _isDarkTheme = wantsDarkTheme;
        var palette = wantsDarkTheme ? ThemePalette.Dark : ThemePalette.Light;

        ApplyPaletteToControl(this, palette);
        _refreshButton.FlatAppearance.BorderColor = palette.Border;

        Invalidate(true);
    }

    private static bool IsDarkThemeEnabled()
    {
        const string personalizePath = "Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize";
        const string appsUseLightThemeValue = "AppsUseLightTheme";

        using var personalizeKey = Registry.CurrentUser.OpenSubKey(personalizePath, false);
        var value = personalizeKey?.GetValue(appsUseLightThemeValue);

        return value switch
        {
            int intValue => intValue == 0,
            long longValue => longValue == 0,
            _ => false
        };
    }

    private static void ApplyPaletteToControl(Control control, ThemePalette palette)
    {
        switch (control)
        {
            case Form:
                control.BackColor = palette.Window;
                control.ForeColor = palette.Text;
                break;

            case TableLayoutPanel or FlowLayoutPanel:
                control.BackColor = palette.Window;
                control.ForeColor = palette.Text;
                break;

            case CheckedListBox listBox:
                listBox.BackColor = palette.Surface;
                listBox.ForeColor = palette.Text;
                listBox.BorderStyle = BorderStyle.FixedSingle;
                break;

            case Button or CheckBox:
                control.BackColor = palette.Surface;
                control.ForeColor = palette.Text;
                break;

            default:
                control.BackColor = palette.Window;
                control.ForeColor = palette.TextMuted;
                break;
        }

        foreach (Control child in control.Controls)
        {
            ApplyPaletteToControl(child, palette);
        }
    }

    public void SetPrograms(IReadOnlyList<ProgramEntry> programs, HashSet<string> selectedProgramKeys)
    {
        _programList.ItemCheck -= OnItemCheck;

        try
        {
            _programList.BeginUpdate();
            _programList.Items.Clear();

            foreach (var program in programs)
            {
                var label = program.IsPlayingAudio
                    ? $"{program.DisplayName} 🔊"
                    : program.DisplayName;
                var item = new ProgramListItem(label, program.IdentityKey);

                var isChecked = selectedProgramKeys.Contains(program.IdentityKey);
                _programList.Items.Add(item, isChecked);
            }

            _countLabel.Text = $"{programs.Count} programs";
        }
        finally
        {
            _programList.EndUpdate();
            _programList.ItemCheck += OnItemCheck;
        }
    }

    private void OnItemCheck(object? sender, ItemCheckEventArgs e)
    {
        BeginInvoke(() =>
        {
            var selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < _programList.Items.Count; i++)
            {
                var item = (ProgramListItem)_programList.Items[i];
                var checkedState = i == e.Index
                    ? e.NewValue == CheckState.Checked
                    : _programList.GetItemChecked(i);

                if (checkedState)
                {
                    selected.Add(item.IdentityKey);
                }
            }

            SelectionChanged?.Invoke(selected);
        });
    }

    private sealed record ProgramListItem(string Label, string IdentityKey)
    {
        public override string ToString() => Label;
    }

    private sealed record ThemePalette(Color Window, Color Surface, Color Text, Color TextMuted, Color Border)
    {
        public static ThemePalette Dark { get; } = new(
            Color.FromArgb(24, 27, 31),
            Color.FromArgb(34, 38, 43),
            Color.FromArgb(237, 240, 244),
            Color.FromArgb(165, 172, 181),
            Color.FromArgb(74, 82, 92));

        public static ThemePalette Light { get; } = new(
            Color.FromArgb(246, 248, 251),
            Color.White,
            Color.FromArgb(34, 40, 49),
            Color.FromArgb(78, 88, 101),
            Color.FromArgb(208, 214, 222));
    }
}

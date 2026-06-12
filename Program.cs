namespace BackgroundMute;

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => LogFatal(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogFatal(ex);
            }
        };

        Application.SetColorMode(SystemColorMode.System);
        ApplicationConfiguration.Initialize();

        try
        {
            Application.Run(new TrayApplicationContext());
        }
        catch (Exception ex)
        {
            LogFatal(ex);
            MessageBox.Show(
                $"BackgroundMute crashed on startup.\n\n{ex.Message}\n\nA log was written to %APPDATA%\\BackgroundMute\\crash.log",
                "BackgroundMute",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static void LogFatal(Exception ex)
    {
        try
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, "BackgroundMute");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "crash.log");
            File.AppendAllText(path, $"[{DateTime.Now:O}] {ex}\n\n");
        }
        catch
        {
            // Ignore logging failures.
        }
    }
}
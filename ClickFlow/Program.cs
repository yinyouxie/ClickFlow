using ClickFlow.Core;
using ClickFlow.UI;

namespace ClickFlow;

internal static class Program
{
    private const string InstanceName = "ClickFlow.SingleInstance.7B1D2C4A";

    [STAThread]
    private static void Main(string[] args)
    {
        var bypassMutex = string.Equals(
            Environment.GetEnvironmentVariable("CLICKFLOW_TEST_NO_MUTEX"),
            "1",
            StringComparison.OrdinalIgnoreCase);

        Mutex? mutex = null;
        var isFirstInstance = true;
        if (!bypassMutex)
        {
            mutex = new Mutex(true, InstanceName, out isFirstInstance);
            if (!isFirstInstance)
            {
                MessageBox.Show(
                    "轻录已经在运行了。\n\n请在任务栏中打开它，或按 F1 开始录制。",
                    "轻录 ClickFlow",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }
        }

        ApplicationConfiguration.Initialize();
        Application.SetDefaultFont(new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point));
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => CrashReporter.Report(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            CrashReporter.Report(e.ExceptionObject as Exception ?? new Exception("未知错误"));

        try
        {
            var initialFile = args.FirstOrDefault(IsMacroPath);
            Application.Run(new MainForm(initialFile));
        }
        catch (Exception ex)
        {
            CrashReporter.Report(ex);
        }
        finally
        {
            if (mutex != null && isFirstInstance)
            {
                mutex.ReleaseMutex();
            }

            mutex?.Dispose();
        }
    }

    private static bool IsMacroPath(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return false;
            }

            var extension = Path.GetExtension(path);
            return extension.Equals(MacroStore.Extension, StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".json", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}

internal static class CrashReporter
{
    public static void Report(Exception ex)
    {
        try
        {
            var dir = MacroStore.AppDataDirectory;
            Directory.CreateDirectory(dir);
            File.AppendAllText(
                Path.Combine(dir, "error.log"),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}\r\n\r\n");
        }
        catch
        {
            // Logging must never mask the original problem.
        }

        try
        {
            MessageBox.Show(
                "轻录遇到了一个意外问题，本次操作已停止。\n\n" +
                ex.Message +
                "\n\n错误日志已写入本地 ClickFlow 目录。",
                "轻录 ClickFlow",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch
        {
            // Ignore UI failures while already handling a failure.
        }
    }
}

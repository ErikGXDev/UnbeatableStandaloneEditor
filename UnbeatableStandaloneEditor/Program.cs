using System.Diagnostics;
using osu.Framework;
using UnbeatableStandaloneEditor.Update;

namespace UnbeatableStandaloneEditor;

class Program
{
    static void Main(string[] args)
    {
        void waitForProcessToClose()
        {
            int oldProcessId = 0;
            var oldProcessIdArg = args.FirstOrDefault(a => int.TryParse(a, out _));
            if (!string.IsNullOrEmpty(oldProcessIdArg) && int.TryParse(oldProcessIdArg, out int pid))
            {
                oldProcessId = pid;
            }

            if (oldProcessId > 0)
            {
                try
                {
                    var oldProcess = Process.GetProcessById(oldProcessId);
                    oldProcess.WaitForExit();
                }
                catch (ArgumentException)
                {
                    // Process already exited
                }
            }
        }

        try
        {

            if (args.Contains("--updated1"))
            {
                Thread.Sleep(1000);
                waitForProcessToClose();

                string targetDirectory = Path.Combine(AppContext.BaseDirectory, "..");

                var thisDirectory = AppContext.BaseDirectory;

                if (!thisDirectory.Contains("__download"))
                {
                    throw new InvalidOperationException("This executable is not running from the download folder.");
                }

                foreach (var file in Directory.GetFiles(thisDirectory))
                {
                    string fileName = Path.GetFileName(file);
                    string targetFilePath = Path.Combine(targetDirectory, fileName);
                    File.Copy(file, targetFilePath, true);
                }

                string targetExecutablePath = Path.Combine(targetDirectory, Updater.GetExecutableName());

                int currentProcessId = Environment.ProcessId;
                Process.Start(new ProcessStartInfo
                {
                    FileName = targetExecutablePath,
                    ArgumentList = { "--updated2", currentProcessId.ToString() },
                    UseShellExecute = true
                });

                // Exit the current application
                Environment.Exit(0);

                return;
            }

            if (args.Contains("--updated2"))
            {
                Thread.Sleep(1000);
                waitForProcessToClose();

                Updater.CleanUpTargetDirectory();
            }

        }
        catch (Exception ex)
        {
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "update_error.txt"), ex.ToString());
        }

        using var host = Host.GetSuitableDesktopHost("unbeatable-beatmap-editor", new HostOptions()
        {
            PortableInstallation = false,
            FriendlyGameName = "UNBEATABLE Standalone Beatmap Editor"
        });
        host.Run(new MainGame());
    }
}

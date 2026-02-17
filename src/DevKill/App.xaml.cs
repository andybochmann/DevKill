using System.IO;
using System.Windows;
using DevKill.Services;

namespace DevKill;

public partial class App : Application
{
    private static Mutex? _mutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Single-instance check
        _mutex = new Mutex(true, @"Global\DevKill_SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show("DevKill is already running.", "DevKill", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        // CLI mode: if numeric arguments are passed, kill processes on those ports and exit
        var args = Environment.GetCommandLineArgs().Skip(1).ToArray();
        var ports = args
            .Where(a => int.TryParse(a, out _))
            .Select(int.Parse)
            .ToList();

        if (ports.Count > 0)
        {
            RunCliMode(ports);
            Shutdown();
            return;
        }

        // GUI mode
        bool minimized = args.Any(a => a.Equals("--minimized", StringComparison.OrdinalIgnoreCase));

        var mainWindow = new Views.MainWindow();
        MainWindow = mainWindow;

        if (minimized)
        {
            // Start hidden in tray
            mainWindow.ShowInTaskbar = false;
            mainWindow.WindowState = WindowState.Minimized;
            mainWindow.Hide();
        }
        else
        {
            mainWindow.Show();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }

    private static void RunCliMode(List<int> ports)
    {
        // Attach to parent console for output
        bool attached = NativeMethods.AttachConsole(NativeMethods.ATTACH_PARENT_PROCESS);
        if (!attached)
            NativeMethods.AllocConsole();

        try
        {
            // Redirect stdout to the attached console
            using var stdout = Console.OpenStandardOutput();
            using var writer = new StreamWriter(stdout) { AutoFlush = true };
            Console.SetOut(writer);

            var entries = PortScanner.Scan();

            foreach (var port in ports)
            {
                var matches = entries.Where(e => e.Port == port).ToList();

                if (matches.Count == 0)
                {
                    Console.WriteLine($"No process found on port {port}");
                    continue;
                }

                foreach (var entry in matches)
                {
                    Console.WriteLine($"Killing {entry.ProcessName} (PID {entry.Pid}) on port {port}...");
                    bool success = ProcessKiller.Kill(entry.Pid);
                    Console.WriteLine(success
                        ? $"  Killed successfully."
                        : $"  Failed to kill process.");
                }
            }
        }
        finally
        {
            if (!attached)
                NativeMethods.FreeConsole();
        }
    }
}

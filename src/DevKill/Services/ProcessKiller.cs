using System.Diagnostics;

namespace DevKill.Services;

public class ProcessKiller : IProcessKiller
{
    public bool Kill(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            process.Kill(entireProcessTree: true);
            return true;
        }
        catch (ArgumentException)
        {
            // Process already exited — treat as success
            return true;
        }
        catch
        {
            return false;
        }
    }
}

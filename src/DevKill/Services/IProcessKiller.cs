namespace DevKill.Services;

public interface IProcessKiller
{
    bool Kill(int pid);
}

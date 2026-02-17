using DevKill.Models;

namespace DevKill.Services;

public interface IPortScanner
{
    List<PortEntry> Scan();
}

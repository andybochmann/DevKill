namespace DevKill.Models;

public sealed record PortEntry
{
    public const string DevServersGroup = "Dev Servers";
    public const string OtherPortsGroup = "Other Ports";

    public required int Port { get; init; }
    public required int Pid { get; init; }
    public required string ProcessName { get; init; }
    public required string ProcessPath { get; init; }
    public required string Protocol { get; init; }
    public required string LocalAddress { get; init; }
    public required string State { get; init; }
    public string WorkingDirectory { get; init; } = "";
    public bool IsDevProcess { get; init; }
    public string GroupName => IsDevProcess ? DevServersGroup : OtherPortsGroup;
}

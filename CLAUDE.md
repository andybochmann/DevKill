# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Is

DevKill is a Windows-only WPF desktop app (.NET 10) that discovers processes listening on ports via native Windows APIs and lets you kill them. It has both a GUI mode and a CLI mode (`devkill 3000`).

## Build & Test Commands

```bash
dotnet build                              # Build entire solution
dotnet build src/DevKill                  # Build app only
dotnet test                               # Run all xUnit tests
dotnet test --filter "FullyQualifiedName~PortScannerTests"  # Run single test class
dotnet run --project src/DevKill          # Launch GUI (requires admin)
```

Release publish (self-contained single file):
```bash
dotnet publish src/DevKill/DevKill.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true --output ./publish
```

## Architecture

### Port Scanning (P/Invoke, no netstat)

`Services/NativeMethods.cs` declares P/Invoke signatures for `iphlpapi.dll` (`GetExtendedTcpTable`, `GetExtendedUdpTable`) and `kernel32.dll` (console attach for CLI mode). Uses `LibraryImport` source generator (not `DllImport`). The project requires `AllowUnsafeBlocks` for the marshal interop.

`Services/PortScanner.cs` calls the native APIs, marshals raw table buffers into `PortEntry` records. Only TCP entries in `LISTEN` state are included. Each entry is classified as a dev server by matching the process name against a hardcoded `DevProcessNames` hashset. The `NetworkToHostPort` and `IsDevProcessName` helpers are `internal` and tested directly.

### MVVM Structure

- **Model**: `Models/PortEntry.cs` — immutable record with `GroupName` computed property (returns "Dev Servers" or "Other Ports")
- **ViewModel**: `ViewModels/MainWindowViewModel.cs` — uses CommunityToolkit.Mvvm source generators (`[ObservableProperty]`, `[RelayCommand]`). Manages an `ObservableCollection<PortEntryViewModel>` with in-place diffing on refresh to avoid UI flicker. A `DispatcherTimer` polls every 3 seconds. `EntriesView` is an `ICollectionView` with grouping by `GroupName` and filter by `FilterText`.
- **ViewModel**: `ViewModels/PortEntryViewModel.cs` — wraps a `PortEntry`, exposes a `KillCommand` that fires a `KillRequested` event (parent VM listens to trigger refresh).
- **View**: `Views/MainWindow.xaml(.cs)` — inherits `FluentWindow` (WPF-UI). Close button minimizes to tray instead of exiting. Keyboard shortcuts handled in `OnKeyDown`. DataGrid column widths are manually adjusted in `SizeChanged` because star sizing doesn't work reliably with grouped DataGrid.

### App Entry & Dual Mode

`App.xaml.cs` handles single-instance enforcement via a named `Mutex`. CLI mode: if numeric args are passed, it attaches to the parent console, scans ports, kills matching processes, and exits without showing any window. GUI mode: respects `--minimized` flag to start hidden in the system tray.

### System Tray

Uses WPF-UI's `NotifyIcon` from `Wpf.Ui.Tray`. The context menu is rebuilt dynamically on open (`TrayContextMenu_Opened`) to show the current top 10 dev server entries as quick-kill items.

### Startup with Windows

`Helpers/StartupManager.cs` writes/removes a registry value under `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` pointing to the exe with `--minimized`.

## Key Conventions

- Target framework is `net10.0-windows` (preview). Tests also target `net10.0-windows` with `UseWPF=true` (needed to test WPF converters).
- Main project exposes internals to tests via `InternalsVisibleTo`.
- All NuGet versions use floating ranges (`4.*`, `8.*`, `2.*`).
- Process killing uses `Process.Kill(entireProcessTree: true)`.
- CI: GitHub Actions release workflow triggers on `v*` tags only. No CI on push/PR.

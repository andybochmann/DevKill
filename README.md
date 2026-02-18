# DevKill

A Windows utility that finds and kills orphaned dev server processes hogging your ports.

Developers accumulate zombie dev servers — Node, .NET, Python, PHP, Vite, and others — that occupy ports and cause conflicts. DevKill auto-discovers these processes, shows them in a modern dark UI, and lets you kill them individually or in bulk.

## Features

- **Auto-discovers listening ports** via native Windows APIs (no netstat parsing)
- **Smart grouping** — known dev server processes (Node, dotnet, Python, PHP, etc.) are surfaced in a "Dev Servers" group
- **Bulk kill** — select multiple processes and kill them all at once
- **System tray** — minimizes to tray with quick-kill context menu
- **CLI mode** — run `devkill 3000` to instantly free a port without opening the GUI
- **Auto-refresh** — polls every 3 seconds for changes
- **Search/filter** — filter by port number, process name, or PID
- **Start with Windows** — optional auto-start via registry

## Screenshots

*Coming soon*

## Requirements

- Windows 10/11
- .NET 10 Runtime
- Administrator privileges (required to enumerate and kill all processes)

## Installation

### Build from source

```
git clone https://github.com/andybochmann/DevKill.git
cd DevKill
dotnet build src/DevKill
```

### Run

```
# GUI mode (launches with UAC prompt)
dotnet run --project src/DevKill

# CLI mode — kill process on port 3000
devkill 3000

# CLI mode — kill processes on multiple ports
devkill 3000 5000 8080

# Start minimized to tray
devkill --minimized
```

## Tech Stack

| Component | Technology |
|-----------|-----------|
| Framework | WPF on .NET 10 |
| UI Theme | [WPF-UI](https://github.com/lepoco/wpfui) (Fluent/WinUI dark theme) |
| MVVM | [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) |
| Port Scanning | P/Invoke to `iphlpapi.dll` (`GetExtendedTcpTable` / `GetExtendedUdpTable`) |

## Project Structure

```
DevKill.sln
src/DevKill/
  App.xaml(.cs)          # Entry point, single-instance, CLI mode
  app.manifest           # UAC admin elevation
  Models/                # PortEntry data record
  Services/              # Port scanning (P/Invoke), process killing
  ViewModels/            # MVVM view models
  Views/                 # MainWindow with grouped DataGrid + tray
  Converters/            # Protocol badge colors
  Helpers/               # Windows startup registry
tests/DevKill.Tests/     # xUnit unit tests
```

## Dev Server Detection

Processes are classified as dev servers by matching their process name against a whitelist:

`node` `dotnet` `php` `iisexpress` `python` `python3` `ruby` `java` `deno` `bun` `uvicorn` `gunicorn` `nginx` `httpd` `apache` `hugo` `caddy` `vite`

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Delete` | Kill selected process(es) |
| `Ctrl+K` / `Ctrl+F` | Focus search box |
| `Ctrl+A` | Select all |
| `F5` | Refresh |
| `Escape` | Minimize to tray |

## Running Tests

```
dotnet test
```

## License

[MIT](LICENSE)

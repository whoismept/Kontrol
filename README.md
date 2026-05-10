# Kontrol

A Windows desktop application for fan speed control and RGB lighting management.

![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4)
![WinUI 3](https://img.shields.io/badge/UI-WinUI%203%20(Windows%20App%20SDK%201.6)-0078D4)
![Platform](https://img.shields.io/badge/platform-Windows%2010%202004%2B-blue)
![Admin](https://img.shields.io/badge/requires-Administrator-orange)

---

## Features

### Fan Control

| Feature | Description |
|---------|-------------|
| **Quick Profiles** | Auto, Silent, Balanced, Performance — apply to all fans instantly from the Dashboard |
| **Custom Profiles** | Save the current fan configuration as a named profile and restore it later |
| **Auto (BIOS)** | Hand control back to the BIOS firmware |
| **Fixed Speed** | Set a constant speed from 0–100% |
| **Temperature Curve** | Custom temperature/speed points with hysteresis and response time |
| **Curve Editor** | Drag-and-drop graphical curve editor |
| **Temperature Sources** | Combine multiple sensors using Max, Min, or Average |
| **Fan Renaming** | Assign custom labels to fans |
| **Fan Hiding** | Hide unused fans from the list |
| **Config Import/Export** | Save and load fan configurations in JSON format |

### RGB Control

| Feature | Description |
|---------|-------------|
| **Direct HID** | No third-party app required; communicates directly over USB |
| **Multi-Zone** | Each addressable zone is controlled independently (e.g. ASUS Aura: 3 zones) |
| **LED Count Override** | Set a custom LED count per zone |
| **Color Profiles** | Save and load per-device color profiles |
| **Dashboard Flyout** | Quick color and profile switching without leaving the Dashboard |
| **Windows LampArray** | Built-in Windows lighting API (Win10 2004+, no extra software needed) |

**Supported protocols:**

| Brand | Protocol | Status |
|-------|----------|--------|
| ASUS | Aura USB (Gen1 / Gen2) | ✅ Working |
| Gigabyte | RGB Fusion 2 USB | ✅ Defined |
| NZXT | HUE 2 / Kraken | ✅ Defined |
| MSI | Mystic Light USB | ✅ Defined |
| ASRock | Polychrome USB | ✅ Defined |
| Corsair | HID (draft) | 🚧 In progress |

> Device definitions live in `Kontrol.Rgb/Definitions/Builtin/` as JSON files.
> Adding a new brand only requires a new JSON file — no recompilation needed.

### Hardware Monitoring

- CPU / GPU temperatures via LibreHardwareMonitor
- Fan speeds in RPM
- Spinning fan animation with live summary on the Dashboard
- Critical temperature alerts (configurable threshold, system tray notification)

### Application

- WinUI 3 Fluent Design interface with Mica backdrop
- **Dashboard (Simple Mode)** — fan profile quick-switch + RGB color change on one screen
- **Advanced Mode** — curve editor, temperature sources, per-zone RGB control
- Minimize to system tray
- Launch on Windows startup
- Language support (English / Turkish)

---

## Requirements

| Requirement | Minimum |
|-------------|---------|
| Windows | 10 2004 (build 19041) or later |
| .NET | 9.0 |
| Administrator rights | Required for fan control |
| Visual Studio / MSBuild | Required for building (WinUI 3 PRI packaging) |

> Fan control requires administrator privileges so LibreHardwareMonitor can write to hardware registers.
> Without elevation, fans are monitored in read-only mode.

---

## Building

```powershell
git clone https://github.com/user/kontrol.git
cd kontrol

# Build (requires Visual Studio or VS Build Tools)
msbuild Kontrol.sln /p:Configuration=Debug

# Run (as Administrator)
dotnet run --project Kontrol.Gui/Kontrol.Gui.csproj

# Self-contained publish
dotnet publish Kontrol.Gui/Kontrol.Gui.csproj -c Release -o ./publish
```

> `dotnet build` alone is not sufficient — the Windows App SDK PRI packaging step requires MSBuild.
> Close any running instance before building; output DLLs will be locked otherwise.

---

## Project Structure

Three projects all targeting `net9.0-windows10.0.19041.0`:

```
Kontrol/
├── Kontrol.Gui/                    # WinUI 3 — UI, navigation, ViewModels
│   ├── Views/
│   │   ├── DashboardView.xaml      # Simple mode: profile cards + RGB flyout
│   │   ├── FanControlView.xaml     # Advanced fan management
│   │   ├── RgbView.xaml            # Advanced RGB management (zone selection)
│   │   ├── SettingsView.xaml
│   │   └── CurveGraphEditor.cs     # Drag-and-drop curve editor (Canvas)
│   ├── ViewModels/
│   │   ├── MainViewModel.cs        # Fan profiles + Simple/Advanced mode toggle
│   │   ├── FanControlViewModel.cs  # Fan assignment, curves, profile management
│   │   ├── FanViewModel.cs         # Live hardware monitoring
│   │   ├── RgbViewModel.cs         # Zone selection, LED override, profiles
│   │   └── SettingsViewModel.cs
│   ├── Models/
│   │   └── AppSettings.cs          # Persisted to %APPDATA%\Kontrol\settings.json
│   ├── Services/
│   │   ├── Loc.cs                  # Localization (DynamicResource)
│   │   └── TrayService.cs          # Win32 P/Invoke system tray
│   └── Resources/Strings/
│       ├── en.xaml
│       └── tr.xaml
│
├── Kontrol.Fan/                    # Class library — fan domain
│   ├── Models/
│   │   ├── FanAssignment.cs        # Mode, fixed %, curve ID, temp source
│   │   ├── FanProfile.cs           # Profile snapshot model
│   │   ├── FanCurve.cs
│   │   └── TempSource.cs
│   └── Services/
│       ├── HardwareService.cs      # LibreHardwareMonitor wrapper
│       ├── FanControlService.cs    # SetSpeed / SetAuto / fan discovery
│       ├── FanControllerService.cs # 2000 ms polling timer + curve application
│       ├── FanProfileService.cs    # Preset and custom profile I/O
│       ├── FanConfigService.cs     # fanconfig.json read/write
│       ├── TempSourceEvaluator.cs  # Max/Min/Average sensor combining
│       ├── FanCurveEvaluator.cs    # Hysteresis + interpolation
│       └── TempAlertService.cs
│
└── Kontrol.Rgb/                    # Class library — RGB domain
    ├── Models/
    │   ├── RgbDevice.cs            # Backend, zone collection, color state
    │   └── RgbZone.cs              # Zone name, channel, LED count, color
    ├── Backends/
    │   ├── ILedBackend.cs          # SetColor / SetZoneColor interface
    │   ├── HidBackend.cs           # JSON-driven zone discovery + HID writes
    │   └── LampArrayBackend.cs     # Windows.Devices.Lights.LampArray
    ├── Protocols/
    │   ├── AuraUsbProtocol.cs      # ASUS Aura command set
    │   ├── GigabyteRgbFusion2Protocol.cs
    │   ├── NzxtHue2Protocol.cs
    │   ├── MsiMysticLightProtocol.cs
    │   └── AsrockPolychromeProtocol.cs
    ├── Definitions/
    │   └── Builtin/                # Device JSON definitions (VID/PID + zones)
    │       ├── asus-aura.json
    │       ├── gigabyte-fusion2.json
    │       ├── nzxt-hue2.json
    │       └── ...
    └── Services/
        ├── RgbService.cs           # Initializes HidBackend + LampArrayBackend
        └── RgbProfileService.cs
```

---

## Data Locations

| File | Path |
|------|------|
| App settings | `%APPDATA%\Kontrol\settings.json` |
| Fan configuration | `%APPDATA%\Kontrol\fan_config.json` |
| Custom fan profiles | `%APPDATA%\Kontrol\fan_profiles\*.json` |
| RGB profiles | `%APPDATA%\Kontrol\rgb_profiles\*.json` |

---

## Technical Notes

**Fan speed mapping:** `SetSoftware()` does not accept a raw 0–100 value on every board. The app performs a proportional conversion based on the hardware's `MinSoftwareValue` / `MaxSoftwareValue` before sending the command.

**Multi-zone RGB:** The HID backend reads the zone list from the JSON definition and sends a separate channel command per zone. Each zone uses `LedStart = 0` — zones are treated as independent LED arrays.

**BIOS lock:** Some motherboards lock software fan control at the firmware level. The command may be sent successfully, but the hardware can silently ignore it — this is a firmware limitation, not a bug.

**LampArray:** The `Windows.Devices.Lights.LampArray` API is built into Windows 10 2004+. No additional package is required.

---

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.WindowsAppSDK | 1.6 | WinUI 3 runtime |
| CommunityToolkit.Mvvm | 8.4.2 | ObservableObject, RelayCommand, source generators |
| LibreHardwareMonitorLib | 0.9.6 | Sensor reading and fan write access |
| HidSharp | 2.6.4 | USB HID device communication |
| Windows.Devices.Lights | built-in | LampArray — no extra package needed |

---

## Protocol References

RGB device protocols were studied from the **OpenRGB** source code and independently reimplemented in C#. OpenRGB is not used or bundled.

- OpenRGB: https://openrgb.org — Adam Honse (CalcProgrammer1), GPLv2

---

## License

MIT

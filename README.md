# G-Helper Battery — Xbox Game Bar Widget

A tiny Xbox Game Bar widget that shows live **battery percentage** and
**charge / discharge rate in watts** — pin it to the Game Bar home bar and keep
an eye on your power draw while gaming without leaving the game.

```
  ┌─────────────────┐
  │                 │
  │      87%        │
  │     battery     │
  │                 │
  │    ▼ 24.3 W     │
  │   discharging   │
  │                 │
  └─────────────────┘
```

## Install

1. Grab the latest `GHelperXboxBar-x64` (or `-ARM64`) artifact from the
   [Actions](../../actions) tab.
2. Trust the signing cert:
   ```powershell
   Import-Certificate -FilePath .\*.cer -CertStoreLocation Cert:\LocalMachine\TrustedPeople
   ```
3. Install the MSIX:
   ```powershell
   Add-AppxPackage -Path .\GHelperXboxBar.Package_*.msix
   ```
4. Press **Win+G**, open the widget menu (≡), find **G-Helper Battery**, pin it.

## How it works

The widget is a tiny UWP page that polls `Windows.Devices.Power.Battery.AggregateBattery`
every two seconds and also subscribes to `ReportUpdated` for push updates.
Values shown:

- **Battery %** — `RemainingCapacityInMilliwattHours / FullChargeCapacityInMilliwattHours`
- **Rate (W)** — `ChargeRateInMilliwatts / 1000`, colored red (discharging) or
  green (charging), idle when |rate| < 50 mW.

No full-trust helper, no `SendInput`, no `runFullTrust` capability — so the
package works under Windows 11's Smart App Control.

Despite the name "G-Helper", this widget doesn't actually depend on G-Helper;
it reads the OS battery report, so it works on any Windows 10/11 laptop.

## Project layout

```
GHelperXboxBar.sln
├── GHelperXboxBar/           UWP (C# / XAML) — the widget UI
└── GHelperXboxBar.Package/   Windows App Packaging project (MSIX)
    └── Public/               REQUIRED folder referenced by the widget
                              AppExtension's PublicFolder attribute — if it's
                              missing from the produced MSIX, Windows silently
                              drops the extension registration and the widget
                              never appears in Game Bar.
```

The manifest uses the modern `microsoft.gameBarUIExtension` contract with the
`<GameBarWidget Type="Standard">` schema, required by current Xbox Game Bar
builds. The older `com.microsoft.xboxgamebar.widget` contract still registers
with `AppExtensionCatalog` but is invisible to the Game Bar UI.

## Build

### GitHub Actions (no local toolchain)

`.github/workflows/build.yml` runs on `windows-2022`, generates placeholder
assets, creates an ephemeral self-signed cert, and builds `x64` + `ARM64` MSIX
artifacts. Push → grab artifact → install.

### Local

- Visual Studio 2022 (17.8+) with *UWP development* + *.NET desktop development*
  workloads, or headless **VS 2022 Build Tools** with
  `Microsoft.VisualStudio.Workload.UniversalBuildTools` + Win10 SDK 19041.
- Enable **Developer Mode** in Windows Settings → Privacy & security → For developers.

```powershell
./build/Generate-PlaceholderAssets.ps1
msbuild GHelperXboxBar.sln /t:Restore,Build `
  /p:Configuration=Release /p:Platform=x64 `
  /p:UapAppxPackageBuildMode=SideloadOnly `
  /p:AppxPackageSigningEnabled=true `
  /p:PackageCertificateKeyFile=GHelperXboxBar.Package\GHelperXboxBar.Package_TemporaryKey.pfx
```

## Credits

- Xbox Game Bar Widget SDK sample by Microsoft — widget extension reference.
- [G-Helper](https://github.com/seerge/g-helper) by @seerge — the ROG power
  tool that inspired this widget.

# G-Helper Xbox Game Bar Widget

An Xbox Game Bar widget that lets you switch [G-Helper](https://github.com/seerge/g-helper)
performance profiles (Silent / Balanced / Turbo / Custom 1 / Custom 2) and toggle the
XG Mobile (XGM) GPU without leaving your game — press **Win+G**, click a button, done.

![mockup](docs/mockup.png)

## How it works

G-Helper already registers global hotkeys for every performance profile in
`InputDispatcher.cs`:

| Hotkey (default)         | G-Helper action                  |
| ------------------------ | -------------------------------- |
| `Ctrl+Shift+Alt + F17`   | `SetPerformanceMode(0)` Balanced |
| `Ctrl+Shift+Alt + F18`   | `SetPerformanceMode(1)` Turbo    |
| `Ctrl+Shift+Alt + F16`   | `SetPerformanceMode(2)` Silent   |
| `Ctrl+Shift+Alt + F19`   | `SetPerformanceMode(3)` Custom 1 |
| `Ctrl+Shift+Alt + F20`   | `SetPerformanceMode(4)` Custom 2 |
| `Ctrl+Shift+Alt + F21`   | Toggle XGM (eGPU)                |

These are pre-registered when G-Helper starts (no user configuration required).
This widget synthesizes those key combos with `SendInput`, so **no changes to
G-Helper are needed** — it just has to be running.

The modifier keys are configurable in G-Helper via the `modifier_keybind_alt`
AppConfig setting; the widget reads that same config file (if present) so custom
modifiers are honoured.

## Architecture

Xbox Game Bar widgets must be UWP apps, which are sandboxed and cannot call
`SendInput` or launch arbitrary processes. So the solution contains three
projects, as is standard for packaged Game Bar widgets that need desktop access:

```
GHelperXboxBar.sln
├── GHelperXboxBar/           UWP (C# / XAML) — the widget UI
├── GHelperHotkey/            .NET desktop EXE — sends the SendInput hotkeys
└── GHelperXboxBar.Package/   Windows App Packaging project (MSIX) that hosts
                              both, declares the Xbox Game Bar widget
                              extension and the windows.fullTrustProcess
                              extension.
```

The UWP widget stores the requested profile index in `ApplicationData.LocalSettings`
and then calls `FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync`.
`GHelperHotkey.exe` starts, reads that setting, synthesizes the correct
`Ctrl+Shift+Alt+F1x` combination via `SendInput`, and exits. G-Helper's keyboard
hook receives it and flips the profile.

## Building via GitHub Actions (no local toolchain required)

The repo ships a `build-msix` workflow (`.github/workflows/build.yml`) that runs
on `windows-2022` (pinned — `windows-latest` was upgraded to Server 2025 and
no longer ships a UAP-capable Windows 10 SDK). The runner already has
VS 2022 Build Tools + UWP + MSIX SDKs
pre-installed). It:

1. Generates placeholder visual assets (`build/Generate-PlaceholderAssets.ps1`).
2. Creates an ephemeral self-signed code-signing cert.
3. Restores and builds the MSIX for both `x64` and `ARM64`.
4. Uploads `.msix` + `.cer` as workflow artifacts.

Push to GitHub, open the **Actions** tab, grab the artifact from the latest run,
unzip, trust the `.cer` (`Import-Certificate … -CertStoreLocation Cert:\LocalMachine\TrustedPeople`),
then `Add-AppxPackage .\GHelperXboxBar.Package_*.msix`.

## Building locally



- Either **Visual Studio 2022 (17.8+)** with the *UWP development* and
  *.NET desktop development* workloads, **or** the free headless
  **Visual Studio 2022 Build Tools** with the equivalent build workloads
  (`Microsoft.VisualStudio.Workload.UniversalBuildTools` +
  `Microsoft.VisualStudio.Workload.ManagedDesktopBuildTools` + Win10 SDK 19041).
- Windows 10 version 2004 (19041) or Windows 11

### Option A — Visual Studio IDE

1. Open `GHelperXboxBar.sln` in Visual Studio.
2. Right-click `GHelperXboxBar.Package` → **Set as Startup Project**.
3. Enable **Developer Mode** in Windows Settings → Privacy & security → For developers.
4. Build configuration: `Release | x64` (or `ARM64`).
5. Right-click the Package project → **Publish → Create App Packages…**
   - Choose **Sideloading**, generate a self-signed cert if you don't have one.
6. Install the produced `.msix` (or `.msixbundle`) by double-clicking it, or run:
   ```powershell
   Add-AppxPackage -Path .\AppPackages\GHelperXboxBar.Package_*_Test\GHelperXboxBar.Package_*.msix
   ```
7. Trust the self-signed cert: double-click the `.cer` next to the msix, install
   into **Local Machine → Trusted People**.

### Option B — Build Tools only (no IDE)

```powershell
# From a "Developer PowerShell for VS 2022" window
./build/Generate-PlaceholderAssets.ps1    # one-time, unless you have real PNGs
msbuild GHelperXboxBar.sln /t:Restore,Build `
  /p:Configuration=Release /p:Platform=x64 `
  /p:UapAppxPackageBuildMode=SideloadOnly `
  /p:AppxPackageSigningEnabled=true `
  /p:PackageCertificateKeyFile=GHelperXboxBar.Package\GHelperXboxBar.Package_TemporaryKey.pfx
```

## Enable in Game Bar

1. Make sure **G-Helper is running**.
2. Press **Win+G** to open Xbox Game Bar.
3. Click the **Widget menu** button (≡) in the top bar → find **G-Helper Profiles** → click to pin.
4. Optionally pin it to the home bar (the star icon).

## Customising the hotkeys

If you've changed `modifier_keybind_alt` or any of the `keybind_profile_N` keys
in G-Helper's `%APPDATA%\GHelper\config.json`, the widget will pick those up
automatically on launch. To force a re-read, click the ⟳ refresh icon in the
widget's top-right.

## Credits

- [G-Helper](https://github.com/seerge/g-helper) by @seerge — the actual brains.
- Xbox Game Bar Widget SDK sample by Microsoft — extension manifest reference.

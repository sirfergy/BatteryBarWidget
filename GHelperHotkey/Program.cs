using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using Windows.Storage;

namespace GHelperHotkey;

/// <summary>
/// Tiny desktop sidecar for the G-Helper Xbox Game Bar widget.
///
/// Reads the requested action from the UWP package's LocalSettings
/// (ApplicationData.Current.LocalSettings), looks up the matching hotkey combo
/// from G-Helper's own config.json (falling back to G-Helper defaults), and
/// synthesizes the key presses via SendInput. G-Helper's global keyboard hook
/// picks them up and switches the performance profile.
/// </summary>
internal static class Program
{
    // G-Helper defaults from app/Input/InputDispatcher.cs
    // keybind_profile_0 = F17 (Balanced)
    // keybind_profile_1 = F18 (Turbo)
    // keybind_profile_2 = F16 (Silent)
    // keybind_profile_3 = F19 (Custom 1)
    // keybind_profile_4 = F20 (Custom 2)
    // keybind_xgm       = F21
    // modifier_keybind_alt = Control | Shift | Alt
    private static readonly Dictionary<string, ushort> DefaultVks = new()
    {
        ["0"]   = VK_F17,
        ["1"]   = VK_F18,
        ["2"]   = VK_F16,
        ["3"]   = VK_F19,
        ["4"]   = VK_F20,
        ["xgm"] = VK_F21,
    };

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            string action = ReadAction(args);
            if (string.IsNullOrEmpty(action))
            {
                return 1;
            }

            if (action == "reload")
            {
                // Nothing to do — config is read fresh on every invocation.
                return 0;
            }

            var (modVks, profileVks) = LoadHotkeyConfig();
            if (!profileVks.TryGetValue(action, out ushort vk))
            {
                return 2;
            }

            SendCombo(modVks, vk);
            return 0;
        }
        catch (Exception ex)
        {
            try
            {
                var log = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "GHelperXboxBar.log");
                File.AppendAllText(log, $"{DateTime.Now:O} {ex}\n");
            }
            catch { /* ignore */ }
            return 99;
        }
    }

    private static string ReadAction(string[] args)
    {
        if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
        {
            return args[0].Trim();
        }

        try
        {
            var settings = ApplicationData.Current.LocalSettings.Values;
            if (settings.TryGetValue("action", out object? value) && value is string s)
            {
                return s;
            }
        }
        catch
        {
            // Running outside the packaged identity (e.g. standalone dev test).
        }

        return string.Empty;
    }

    private static (ushort[] modifiers, Dictionary<string, ushort> profiles) LoadHotkeyConfig()
    {
        var modVks = new List<ushort> { VK_CONTROL, VK_SHIFT, VK_MENU };
        var profileVks = new Dictionary<string, ushort>(DefaultVks);

        try
        {
            var cfgPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GHelper", "config.json");

            if (!File.Exists(cfgPath))
            {
                return (modVks.ToArray(), profileVks);
            }

            using var doc = JsonDocument.Parse(File.ReadAllText(cfgPath));
            var root = doc.RootElement;

            // Override profile key bindings if present.
            OverrideVk(root, "keybind_profile_0", profileVks, "0");
            OverrideVk(root, "keybind_profile_1", profileVks, "1");
            OverrideVk(root, "keybind_profile_2", profileVks, "2");
            OverrideVk(root, "keybind_profile_3", profileVks, "3");
            OverrideVk(root, "keybind_profile_4", profileVks, "4");
            OverrideVk(root, "keybind_xgm",       profileVks, "xgm");

            // Override modifier mask if present. Values match
            // System.Windows.Input.ModifierKeys: Alt=1, Control=2, Shift=4, Windows=8.
            if (root.TryGetProperty("modifier_keybind_alt", out var modEl) &&
                modEl.TryGetInt32(out int mask))
            {
                modVks.Clear();
                if ((mask & 2) != 0) modVks.Add(VK_CONTROL);
                if ((mask & 4) != 0) modVks.Add(VK_SHIFT);
                if ((mask & 1) != 0) modVks.Add(VK_MENU);
                if ((mask & 8) != 0) modVks.Add(VK_LWIN);
            }
        }
        catch
        {
            // Fall through with defaults.
        }

        return (modVks.ToArray(), profileVks);
    }

    private static void OverrideVk(JsonElement root, string name, Dictionary<string, ushort> map, string key)
    {
        if (root.TryGetProperty(name, out var el) && el.TryGetInt32(out int vk) && vk > 0)
        {
            map[key] = (ushort)vk;
        }
    }

    private static void SendCombo(ushort[] modifiers, ushort key)
    {
        int total = (modifiers.Length + 1) * 2;
        var inputs = new INPUT[total];
        int i = 0;

        foreach (var m in modifiers)
        {
            inputs[i++] = MakeKey(m, keyUp: false);
        }
        inputs[i++] = MakeKey(key, keyUp: false);
        inputs[i++] = MakeKey(key, keyUp: true);
        for (int j = modifiers.Length - 1; j >= 0; j--)
        {
            inputs[i++] = MakeKey(modifiers[j], keyUp: true);
        }

        uint sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
        if (sent != inputs.Length)
        {
            throw new InvalidOperationException(
                $"SendInput injected {sent}/{inputs.Length} events (error {Marshal.GetLastWin32Error()}).");
        }
    }

    private static INPUT MakeKey(ushort vk, bool keyUp) => new()
    {
        type = INPUT_KEYBOARD,
        U = new InputUnion
        {
            ki = new KEYBDINPUT
            {
                wVk = vk,
                wScan = 0,
                dwFlags = keyUp ? KEYEVENTF_KEYUP : 0,
                time = 0,
                dwExtraInfo = IntPtr.Zero,
            }
        }
    };

    // ---- Win32 interop ----

    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    private const ushort VK_SHIFT = 0x10;
    private const ushort VK_CONTROL = 0x11;
    private const ushort VK_MENU = 0x12; // Alt
    private const ushort VK_LWIN = 0x5B;
    private const ushort VK_F16 = 0x7F;
    private const ushort VK_F17 = 0x80;
    private const ushort VK_F18 = 0x81;
    private const ushort VK_F19 = 0x82;
    private const ushort VK_F20 = 0x83;
    private const ushort VK_F21 = 0x84;

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx; public int dy;
        public uint mouseData; public uint dwFlags; public uint time; public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT { public uint uMsg; public ushort wParamL; public ushort wParamH; }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
}

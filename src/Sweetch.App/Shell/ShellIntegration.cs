using Microsoft.Win32;
using static Sweetch.Core.Loc;

namespace Sweetch.App.Shell;

/// <summary>Autostart and the "Forma…" entry in Explorer's context menu (per user, no admin needed).</summary>
internal static class ShellIntegration
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    /// <summary>
    /// Where Windows remembers the Task Manager / Settings → Startup apps switch. An odd first byte means
    /// "disabled": the Run entry stays, but Windows no longer starts it.
    /// </summary>
    private const string ApprovedKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
    private const string ValueName = "Forma";
    private static readonly string[] MenuRoots = [@"Software\Classes\*\shell\Forma", @"Software\Classes\Directory\shell\Forma"];

    // Up to 1.2.0 Forma used the original Sweetch's names, so the two apps overwrote each other's entries.
    private const string LegacyName = "Sweetch";
    private static readonly string[] LegacyMenuRoots = [@"Software\Classes\*\shell\Sweetch", @"Software\Classes\Directory\shell\Sweetch"];

    private static string Exe => Environment.ProcessPath ?? throw new InvalidOperationException(T("Unknown application path.", "Неизвестный путь приложения."));

    public static bool IsAutostartEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            if (key?.GetValue(ValueName) is not string value || !value.Contains(Exe, StringComparison.OrdinalIgnoreCase))
                return false;
            using var approved = Registry.CurrentUser.OpenSubKey(ApprovedKey);
            return approved?.GetValue(ValueName) is not byte[] { Length: > 0 } state || (state[0] & 1) == 0;
        }
    }

    /// <summary>Turning it on also lifts a "disabled" set in Startup apps, so the switch means what it shows.</summary>
    public static void SetAutostart(bool enabled)
    {
        WriteRunEntry(enabled);
        using var approved = Registry.CurrentUser.OpenSubKey(ApprovedKey, writable: true);
        approved?.DeleteValue(ValueName, throwOnMissingValue: false);
    }

    private static void WriteRunEntry(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (enabled)
            key.SetValue(ValueName, $"\"{Exe}\" --background");
        else
            key.DeleteValue(ValueName, throwOnMissingValue: false);
    }

    public static bool IsContextMenuEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(MenuRoots[0] + @"\command");
            return key?.GetValue(null) is string value && value.Contains(Exe, StringComparison.OrdinalIgnoreCase);
        }
    }

    public static void SetContextMenu(bool enabled)
    {
        foreach (var root in MenuRoots)
        {
            if (!enabled)
            {
                Registry.CurrentUser.DeleteSubKeyTree(root, throwOnMissingSubKey: false);
                continue;
            }
            using var key = Registry.CurrentUser.CreateSubKey(root);
            key.SetValue(null, "Forma…");
            key.SetValue("Icon", $"\"{Exe}\",0");
            key.SetValue("MultiSelectModel", "Player");
            using var command = key.CreateSubKey("command");
            command.SetValue(null, $"\"{Exe}\" --wheel \"%1\"");
        }
    }

    /// <summary>
    /// Renames the entries Forma itself wrote under Sweetch's names. Entries pointing at another exe belong
    /// to the original Sweetch and stay.
    /// </summary>
    private static void MoveLegacyEntries()
    {
        using (var run = Registry.CurrentUser.OpenSubKey(RunKey, writable: true))
        {
            if (run?.GetValue(LegacyName) is string value && value.Contains(Exe, StringComparison.OrdinalIgnoreCase))
            {
                run.DeleteValue(LegacyName, throwOnMissingValue: false);
                run.SetValue(ValueName, $"\"{Exe}\" --background");
            }
        }
        bool ours;
        using (var command = Registry.CurrentUser.OpenSubKey(LegacyMenuRoots[0] + @"\command"))
            ours = command?.GetValue(null) is string value && value.Contains(Exe, StringComparison.OrdinalIgnoreCase);
        if (!ours)
            return;
        foreach (var root in LegacyMenuRoots)
            Registry.CurrentUser.DeleteSubKeyTree(root, throwOnMissingSubKey: false);
        SetContextMenu(true);
    }

    /// <summary>Re-points registered entries at the current exe after the app was moved or updated.</summary>
    public static void Refresh()
    {
        try
        {
            MoveLegacyEntries();
            if (Registry.CurrentUser.OpenSubKey(MenuRoots[0]) is { } existing)
            {
                existing.Dispose();
                SetContextMenu(true);
            }
            using var run = Registry.CurrentUser.OpenSubKey(RunKey);
            // Only the path; a choice made in Startup apps stays as it is.
            if (run?.GetValue(ValueName) is not null)
                WriteRunEntry(true);
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}

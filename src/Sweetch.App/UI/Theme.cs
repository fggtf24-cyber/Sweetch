using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace Sweetch.App.UI;

/// <summary>
/// Colours and fonts shared by the wheel, the job cards and the shelf. Follows the Windows
/// light/dark app setting; <see cref="Refresh"/> re-reads it before a window is built.
/// </summary>
internal static class Theme
{
    // Minimal, black and white: flat opaque surfaces separated by hairlines. No gradients, blur or shadows.
    private static readonly Palette DarkPalette = new(
        Surface: Color.FromRgb(0x14, 0x14, 0x15),
        Line: Color.FromRgb(0x2E, 0x2E, 0x30),
        Text: Color.FromRgb(0xF2, 0xF2, 0xF3),
        Muted: Color.FromRgb(0x8E, 0x8E, 0x93),
        Track: Color.FromRgb(0x26, 0x26, 0x28),
        Hover: Color.FromRgb(0x1E, 0x1E, 0x20));

    private static readonly Palette LightPalette = new(
        Surface: Colors.White,
        Line: Color.FromRgb(0xE3, 0xE3, 0xE5),
        Text: Color.FromRgb(0x14, 0x14, 0x15),
        Muted: Color.FromRgb(0x6E, 0x6E, 0x73),
        Track: Color.FromRgb(0xEE, 0xEE, 0xF0),
        Hover: Color.FromRgb(0xF5, 0xF5, 0xF6));

    private sealed record Palette(Color Surface, Color Line, Color Text, Color Muted, Color Track, Color Hover);

    private static Palette _palette = DarkPalette;

    // Monochrome: the selection is white on dark, black on light.
    public static Color AccentColor => _palette.Text;

    public static SolidColorBrush Accent => Freeze(new SolidColorBrush(AccentColor));
    public static SolidColorBrush TextOnAccent => Freeze(new SolidColorBrush(IsDark ? Color.FromRgb(0x11, 0x11, 0x13) : Colors.White));
    public static readonly SolidColorBrush Error = Freeze(new SolidColorBrush(Color.FromRgb(0xF0, 0x5A, 0x5A)));
    public static readonly SolidColorBrush Success = Freeze(new SolidColorBrush(Color.FromRgb(0x3C, 0xC3, 0x6E)));

    public static readonly FontFamily Icons = new("Segoe Fluent Icons, Segoe MDL2 Assets");
    // Type from Smooth Cursor: Inter for text and headlines, IBM Plex Mono for versions, sizes and counts.
    // Bundled (Assets/Fonts, SIL OFL); Segoe stays as the fallback for scripts Inter lacks.
    private static readonly Uri FontBase = new("pack://application:,,,/");
    public static readonly FontFamily UiFont = new(FontBase, "./Assets/Fonts/#Inter, Segoe UI Variable Text, Segoe UI");
    public static readonly FontFamily DisplayFont = UiFont;
    public static readonly FontFamily MonoFont = new(FontBase, "./Assets/Fonts/#IBM Plex Mono, Cascadia Mono, Consolas");

    public static bool IsDark { get; private set; } = true;

    public static Brush Slice => Solid(_palette.Surface);
    public static Brush SliceEdge => Solid(_palette.Line);
    public static Brush Hub => Solid(_palette.Surface);
    public static Brush Panel => Solid(_palette.Surface);
    public static Brush PanelBorder => Solid(_palette.Line);
    public static Brush Text => Solid(_palette.Text);
    public static Brush Muted => Solid(_palette.Muted);
    public static Brush Track => Solid(_palette.Track);
    public static Brush Page => Solid(_palette.Surface);
    public static Brush Card => Solid(_palette.Surface);
    public static Brush Hover => Solid(_palette.Hover);
    public static Brush AccentSoft => Solid(_palette.Track);

    /// <summary>Matches the title bar to the theme. Call once the window has a handle.</summary>
    public static void ApplyBackdrop(Window window)
    {
        window.Background = Page;
        var hwnd = new System.Windows.Interop.WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero)
            return;
        var dark = IsDark ? 1 : 0;
        Native.Win32.DwmSetWindowAttribute(hwnd, Native.Win32.DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));
    }

    /// <summary>Forces dark (true) or light (false) instead of following Windows; used by the self-test.</summary>
    public static bool? Override { get; set; }

    /// <summary>Re-reads the Windows "app mode" (light/dark).</summary>
    public static void Refresh() => Use(Override ?? ReadSystemDark());

    private static void Use(bool dark)
    {
        IsDark = dark;
        _palette = dark ? DarkPalette : LightPalette;
    }

    private static bool ReadSystemDark()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is not int light || light == 0;
        }
        catch (System.Security.SecurityException)
        {
            return true;
        }
    }

    private static SolidColorBrush Solid(Color color) => Freeze(new SolidColorBrush(color));

    private static T Freeze<T>(T brush)
        where T : Freezable
    {
        brush.Freeze();
        return brush;
    }
}

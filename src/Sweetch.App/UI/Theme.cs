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
    // Liquid glass, black and white: neutral translucent surfaces, lit from the top, with a bright rim on the upper-left edge.
    private static readonly Palette DarkPalette = new(
        Slice: Color.FromArgb(0xA8, 0x1C, 0x1C, 0x1F),
        SliceEdge: Color.FromArgb(0x48, 0xFF, 0xFF, 0xFF),
        Hub: Color.FromArgb(0xC8, 0x17, 0x17, 0x19),
        Panel: Color.FromArgb(0xCC, 0x17, 0x17, 0x19),
        PanelEdge: Color.FromArgb(0x58, 0xFF, 0xFF, 0xFF),
        RimLow: Color.FromArgb(0x28, 0xFF, 0xFF, 0xFF),
        Text: Color.FromRgb(0xF5, 0xF5, 0xF7),
        Muted: Color.FromRgb(0xA1, 0xA1, 0xA6),
        Track: Color.FromArgb(0x25, 0xFF, 0xFF, 0xFF),
        Page: Color.FromRgb(0x0E, 0x0E, 0x10),
        Card: Color.FromArgb(0x16, 0xFF, 0xFF, 0xFF),
        Hover: Color.FromArgb(0x14, 0xFF, 0xFF, 0xFF),
        Shadow: 0.45);

    private static readonly Palette LightPalette = new(
        Slice: Color.FromArgb(0xBC, 0xFF, 0xFF, 0xFF),
        SliceEdge: Color.FromArgb(0xE0, 0xFF, 0xFF, 0xFF),
        Hub: Color.FromArgb(0xD8, 0xFB, 0xFB, 0xFC),
        Panel: Color.FromArgb(0xD4, 0xF8, 0xF8, 0xFA),
        PanelEdge: Color.FromArgb(0xF0, 0xFF, 0xFF, 0xFF),
        RimLow: Color.FromArgb(0x30, 0x00, 0x00, 0x00),
        Text: Color.FromRgb(0x1C, 0x1C, 0x1E),
        Muted: Color.FromRgb(0x6E, 0x6E, 0x73),
        Track: Color.FromArgb(0x1C, 0x00, 0x00, 0x00),
        Page: Color.FromRgb(0xF2, 0xF2, 0xF4),
        Card: Color.FromArgb(0x9C, 0xFF, 0xFF, 0xFF),
        Hover: Color.FromArgb(0x0F, 0x00, 0x00, 0x00),
        Shadow: 0.14);

    private sealed record Palette(
        Color Slice,
        Color SliceEdge,
        Color Hub,
        Color Panel,
        Color PanelEdge,
        Color RimLow,
        Color Text,
        Color Muted,
        Color Track,
        Color Page,
        Color Card,
        Color Hover,
        double Shadow);

    private static Palette _palette = DarkPalette;

    // Monochrome: the selection is bright milky glass on dark, ink-black glass on light.
    public static Color AccentColor => IsDark ? Color.FromRgb(0xFA, 0xFA, 0xFC) : Color.FromRgb(0x1C, 0x1C, 0x1E);
    public static Color AccentColor2 => IsDark ? Color.FromRgb(0xC4, 0xC4, 0xCA) : Color.FromRgb(0x48, 0x48, 0x4D);

    /// <summary>White on dark, black on light; used for the selected slice, active tab and progress bars.</summary>
    public static LinearGradientBrush AccentGradient => Freeze(new LinearGradientBrush(AccentColor, AccentColor2, new Point(0, 0), new Point(1, 1)));

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

    public static Brush Slice => Glass(_palette.Slice);
    public static Brush SliceEdge => Rim(_palette.SliceEdge);
    public static Brush Hub => Glass(_palette.Hub);
    public static Brush Panel => Glass(_palette.Panel);
    public static Brush PanelBorder => Rim(_palette.PanelEdge);
    public static Brush Text => Solid(_palette.Text);
    public static Brush Muted => Solid(_palette.Muted);
    public static Brush Track => Solid(_palette.Track);
    public static Brush Page => Solid(_palette.Page);
    public static Brush Card => Glass(_palette.Card);
    public static Brush Hover => Solid(_palette.Hover);
    public static Brush AccentSoft => Solid(Color.FromArgb(IsDark ? (byte)0x38 : (byte)0x26, AccentColor.R, AccentColor.G, AccentColor.B));

    /// <summary>Soft drop shadow under floating surfaces.</summary>
    public static System.Windows.Media.Effects.DropShadowEffect Shadow(double blur = 24) =>
        Freeze(new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = blur, ShadowDepth = 4, Direction = 270, Opacity = _palette.Shadow, Color = Colors.Black, RenderingBias = System.Windows.Media.Effects.RenderingBias.Performance });

    /// <summary>
    /// Frosted glass behind a normal window: the system acrylic backdrop on Windows 11 22H2+, blurred by DWM
    /// at no cost to us. Elsewhere the window keeps the solid page colour. Call once the window has a handle.
    /// </summary>
    public static void ApplyBackdrop(Window window)
    {
        var hwnd = new System.Windows.Interop.WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero)
        {
            window.Background = Page;
            return;
        }
        var dark = IsDark ? 1 : 0;
        Native.Win32.DwmSetWindowAttribute(hwnd, Native.Win32.DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));
        var backdrop = Native.Win32.DWMSBT_TRANSIENTWINDOW;
        if (Native.Win32.DwmSetWindowAttribute(hwnd, Native.Win32.DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int)) != 0)
        {
            window.Background = Page;
            return;
        }
        var margins = new Native.Win32.MARGINS { Left = -1, Right = -1, Top = -1, Bottom = -1 };
        Native.Win32.DwmExtendFrameIntoClientArea(hwnd, ref margins);
        if (System.Windows.Interop.HwndSource.FromHwnd(hwnd)?.CompositionTarget is { } target)
            target.BackgroundColor = Colors.Transparent;
        window.Background = Brushes.Transparent;
    }

    /// <summary>A tinted pane, a little brighter and denser at the top where the light hits it.</summary>
    private static Brush Glass(Color tint)
    {
        var top = Color.FromArgb((byte)Math.Min(255, tint.A + 0x18), Lift(tint.R), Lift(tint.G), Lift(tint.B));
        return Freeze(new LinearGradientBrush(top, tint, new Point(0, 0), new Point(0, 1)));

        static byte Lift(byte c) => (byte)(c + (255 - c) * 0.08);
    }

    /// <summary>The edge of the glass: bright where the light enters (top-left), faint on the sides, tinted below.</summary>
    private static Brush Rim(Color light) => Freeze(new LinearGradientBrush(
        [
            new GradientStop(light, 0),
            new GradientStop(Color.FromArgb((byte)(light.A / 4), light.R, light.G, light.B), 0.45),
            new GradientStop(_palette.RimLow, 1),
        ],
        new Point(0.2, 0), new Point(0.8, 1)));

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

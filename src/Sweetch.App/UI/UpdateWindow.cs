using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Sweetch.App.Shell;
using Sweetch.Core;
using static Sweetch.App.UI.Controls;
using static Sweetch.Core.Loc;

namespace Sweetch.App.UI;

/// <summary>
/// "A new version is out": Smooth Cursor's update card on a glass pane. Update downloads with a square
/// progress bar and restarts into the new version; Not now skips this version.
/// </summary>
internal sealed class UpdateWindow : Window
{
    private static UpdateWindow? _open;

    private readonly UpdateInfo _update;
    private readonly Updater _updater;
    private readonly SquareBar _bar = new();
    private readonly TextBlock _status = Mono("", 11);
    private readonly Border _go;
    private readonly Border _skip;

    private UpdateWindow(UpdateInfo update, Updater updater, Action<UpdateInfo> skip)
    {
        _update = update;
        _updater = updater;
        Theme.Refresh();
        Title = T("Sweetch – update", "Sweetch — обновление");
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ResizeMode = ResizeMode.NoResize;
        Topmost = true;
        ShowInTaskbar = false;
        SizeToContent = SizeToContent.WidthAndHeight;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        FontFamily = Theme.UiFont;

        var top = new DockPanel();
        var versions = Mono($"{Updater.Current} → {update.Version}", 12, Theme.Text);
        DockPanel.SetDock(versions, Dock.Right);
        top.Children.Add(versions);
        top.Children.Add(Caps(T("Update", "Обновление"), Theme.Text));

        var head = Headline(T($"Version\n{update.Version} is out.", $"Вышла\nверсия {update.Version}."), 46);
        head.Margin = new Thickness(0, 22, 0, 0);
        var text = Text(T("Sweetch will close and reopen updated in a few seconds. Your settings stay as they are.",
            "Sweetch закроется и через пару секунд откроется уже обновлённым. Настройки останутся на месте."), 13, brush: Theme.Muted);
        text.Margin = new Thickness(0, 14, 0, 20);

        var progress = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 18) };
        progress.Children.Add(_bar);
        _status.Margin = new Thickness(8, 0, 0, 0);
        progress.Children.Add(_status);

        _skip = Button(T("Not now", "Не сейчас"), () =>
        {
            skip(_update);
            Close();
        });
        _go = Button(T("Update", "Обновить"), Install, primary: true);
        _go.Margin = new Thickness(10, 0, 0, 0);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        buttons.Children.Add(_skip);
        buttons.Children.Add(_go);

        var body = new StackPanel { Width = 470 };
        body.Children.Add(top);
        body.Children.Add(head);
        body.Children.Add(text);
        body.Children.Add(progress);
        body.Children.Add(new Border { Height = 1, Background = Theme.Track, Margin = new Thickness(0, 0, 0, 16) });
        body.Children.Add(buttons);

        Content = new Border
        {
            Background = Theme.Panel,
            BorderBrush = Theme.PanelBorder,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(22),
            Padding = new Thickness(36, 28, 36, 26),
            Margin = new Thickness(24),
            Child = body,
        };
        Closed += (_, _) =>
        {
            _open = null;
            App.TrimMemorySoon();
        };
    }

    public static void ShowFor(UpdateInfo update, Updater updater, Action<UpdateInfo> skip)
    {
        _open ??= new UpdateWindow(update, updater, skip);
        _open.Show();
        _open.Activate();
    }

    /// <summary>The card mid-download, for the self-test.</summary>
    internal static Window Preview(UpdateInfo update, double progress)
    {
        var window = new UpdateWindow(update, new Updater(), _ => { });
        window._bar.Value = progress;
        window._status.Text = $"{progress * update.InstallerSize / 1048576.0:0.0} / {update.InstallerSize / 1048576.0:0.0} MB";
        return window;
    }

    private async void Install()
    {
        if (!Updater.IsInstalled)
        {
            // The portable ZIP has no installer to update through: show the release instead.
            Updater.Open(_update.PageUrl.Length > 0 ? _update.PageUrl : Updater.Page);
            Close();
            return;
        }
        SetBusy(true);
        var mb = _update.InstallerSize / 1048576.0;
        try
        {
            await _updater.InstallAsync(_update, new Progress<double>(p =>
            {
                _bar.Value = p;
                _status.Text = mb > 0 ? $"{p * mb:0.0} / {mb:0.0} MB" : $"{p:P0}";
            }));
        }
        catch (Exception ex) when (ex is System.Net.Http.HttpRequestException or System.IO.IOException or OperationCanceledException or System.ComponentModel.Win32Exception)
        {
            _status.Text = ex.Message;
            _status.Foreground = Theme.Error;
            SetLabel(_go, T("Try again", "Ещё раз"));
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        if (busy)
            SetLabel(_go, T("Downloading…", "Скачиваю…"));
        foreach (var button in new[] { _go, _skip })
        {
            button.IsEnabled = !busy;
            button.Opacity = busy ? 0.5 : 1;
        }
    }

    private static void SetLabel(Border button, string text) =>
        ((StackPanel)button.Child).Children.OfType<TextBlock>().Last().Text = text;
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using static Sweetch.App.UI.Controls;
using static Sweetch.Core.Loc;

namespace Sweetch.App.UI;

internal sealed partial class SettingsWindow
{
    private void AboutPage(Panel root)
    {
        var header = new DockPanel { Margin = new Thickness(0, 0, 0, 18) };
        header.Children.Add(new Image {
            Source = new BitmapImage(new Uri("pack://application:,,,/Assets/Sweetch.png")),
            Width = 72, Height = 72, Margin = new Thickness(0, 0, 20, 0)
        });
        var heading = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        heading.Children.Add(Headline("Sweetch", 40));
        heading.Children.Add(Text(T("A new shape for your files.", "Новая форма ваших файлов."), 14, brush: Theme.Muted));
        header.Children.Add(heading); root.Children.Add(header);
        root.Children.Add(Card(T("Project creator", "Автор проекта"), "@mmmorozov",
            Button(T("Telegram channel", "Telegram-канал"), () => Shell.Updater.Open(Brand.Channel), glyph: ""), glyph: ""));
        root.Children.Add(SectionHeader(T("Support the project", "Поддержать проект")));
        root.Children.Add(SupportCard());
        root.Children.Add(SectionHeader(T("Feedback", "Обратная связь")));
        root.Children.Add(Card(T("Ideas and questions", "Идеи и вопросы"),
            T("News and feedback on the project's Telegram channel.", "Новости и обратная связь в Telegram-канале проекта."),
            Button("@mmmorozov", () => Shell.Updater.Open(Brand.Channel), glyph: ""), glyph: ""));
        root.Children.Add(SectionHeader(T("Updates", "Обновления")));
        root.Children.Add(Card(T("Update automatically", "Обновлять автоматически"),
            T("Sweetch checks GitHub twice a day and offers a new version; it installs only when you say so.",
              "Sweetch дважды в день проверяет GitHub и предлагает новую версию; ставит только с вашего согласия."),
            Toggle(_settings.AutoUpdate, v =>
            {
                _settings.AutoUpdate = v;
                Save();
            }), glyph: ""));
        root.Children.Add(UpdatesCard());
        var footer = Text(T("Local processing. Your files stay on your computer.", "Локальная обработка. Ваши файлы остаются на компьютере."), 12, brush: Theme.Muted);
        footer.Margin = new Thickness(2, 24, 0, 0); root.Children.Add(footer);
    }

    /// <summary>The version, what the last check found, and Check now / Update.</summary>
    private Border UpdatesCard()
    {
        var updater = Updater;
        var status = updater switch
        {
            null => "",
            { Installing: true } => T("Installing the update…", "Устанавливаю обновление…"),
            { Available: { } a } => T($"Version {a.Version} is available.", $"Доступна версия {a.Version}."),
            { LastError: not null } => T("Couldn't reach GitHub. Will try again later.", "Не удалось связаться с GitHub. Попробую позже."),
            { LastChecked: { } at } => T($"Up to date · checked at {at:HH:mm}", $"Последняя версия · проверено в {at:HH:mm}"),
            _ => T("Not checked yet.", "Ещё не проверялось."),
        };
        var below = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 0) };
        if (updater?.Available is { } update)
            below.Children.Add(Button(T("Update", "Обновить"), () => UpdateWindow.ShowFor(update, updater, SkipUpdate), primary: true));
        else if (updater is not null)
            below.Children.Add(Button(T("Check now", "Проверить сейчас"), async () =>
            {
                if (await updater.CheckAsync() is { } found)
                    UpdateWindow.ShowFor(found, updater, SkipUpdate);
            }));
        var page = Button(T("Releases on GitHub", "Релизы на GitHub"), () => Shell.Updater.Open(Shell.Updater.Page));
        page.Margin = new Thickness(8, 0, 0, 0);
        below.Children.Add(page);
        return Card("Sweetch " + Shell.Updater.Current, status, Mono(Shell.Updater.Repo, 11), below);
    }

    private void SkipUpdate(Core.UpdateInfo update)
    {
        _settings.SkippedVersion = update.Tag;
        Save();
    }

    private static Border SupportCard()
    {
        var below = new StackPanel();
        var network = Text("USDT · TRC20", 13, FontWeights.SemiBold, Theme.Accent);
        network.Margin = new Thickness(0, 14, 0, 6); below.Children.Add(network);
        var address = new TextBox {
            Text = Brand.Trc20, IsReadOnly = true, FontFamily = new System.Windows.Media.FontFamily("Cascadia Mono, Consolas"),
            FontSize = 12.5, Padding = new Thickness(10, 9, 10, 9), Background = Theme.Page,
            Foreground = Theme.Text, BorderBrush = Theme.PanelBorder
        };
        below.Children.Add(address);
        var copied = Text("", 12, brush: Theme.Success);
        copied.Margin = new Thickness(0, 8, 0, 0);
        var buttons = new WrapPanel { Margin = new Thickness(0, 12, 0, 0) };
        var copy = Button(T("Copy address", "Копировать адрес"), () => {
            try { Clipboard.SetText(Brand.Trc20); copied.Text = T("Address copied", "Адрес скопирован"); }
            catch (System.Runtime.InteropServices.ExternalException) { address.Focus(); address.SelectAll(); }
        }, glyph: "");
        var donate = Button(T("Support via DonatePay", "Поддержать через DonatePay"), () => Shell.Updater.Open(Brand.DonatePay), primary: true, glyph: "");
        copy.Margin = new Thickness(0, 0, 10, 8); donate.Margin = new Thickness(0, 0, 0, 8);
        buttons.Children.Add(copy); buttons.Children.Add(donate); below.Children.Add(buttons); below.Children.Add(copied);
        return Card(T("Help Sweetch grow", "Помогите Sweetch развиваться"),
            T("If Sweetch saves you time, you can support the project through DonatePay or USDT on TRC20.", "Если Sweetch экономит ваше время, поддержите проект через DonatePay или USDT в сети TRC20."), null, below, glyph: "");
    }
}

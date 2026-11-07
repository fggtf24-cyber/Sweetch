using System.Windows;
using System.Windows.Controls;
using static Sweetch.App.UI.Controls;
using static Sweetch.Core.Loc;

namespace Sweetch.App.UI;

internal sealed partial class SettingsWindow
{
    private void UpdatesPage(Panel root)
    {
        PageHeader(root, PageName(SettingsPage.Updates), T("Keeping the app up to date.", "Как приложение получает новые версии."));
        root.Children.Add(SectionHeader(T("Updates", "Обновления")));
        root.Children.Add(Card(T("Update automatically", "Обновлять автоматически"),
            T("Forma checks GitHub twice a day and offers a new version; it installs only when you say so.",
              "Forma дважды в день проверяет GitHub и предлагает новую версию; ставит только с вашего согласия."),
            Toggle(_settings.AutoUpdate, v =>
            {
                _settings.AutoUpdate = v;
                Save();
            }), glyph: ""));
        root.Children.Add(UpdatesCard());
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
        return Card("Forma " + Shell.Updater.Current, status, Mono(Shell.Updater.Repo, 11), below);
    }

    private void SkipUpdate(Core.UpdateInfo update)
    {
        _settings.SkippedVersion = update.Tag;
        Save();
    }
}

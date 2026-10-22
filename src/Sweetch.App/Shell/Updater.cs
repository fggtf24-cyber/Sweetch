using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Windows;
using Sweetch.Core;
using static Sweetch.Core.Loc;

namespace Sweetch.App.Shell;

/// <summary>
/// New versions from the GitHub releases of <see cref="Repo"/>: checks quietly, and on the user's yes downloads
/// the installer, verifies it and runs it silently; the installer starts the new version again.
/// </summary>
internal sealed class Updater
{
    public const string Repo = "fggtf24-cyber/Sweetch";
    public const string Page = $"https://github.com/{Repo}/releases/latest";

    // Before Http: static fields initialise in order, and the client puts the version in its User-Agent.
    public static Version Current { get; } = UpdateInfo.Normalize(typeof(Updater).Assembly.GetName().Version ?? new Version(1, 0, 0));

    private static readonly HttpClient Http = CreateClient();

    /// <summary>Installed with the setup (it can install over itself); the portable ZIP just opens the release page.</summary>
    public static bool IsInstalled => File.Exists(Path.Combine(AppContext.BaseDirectory, "unins000.exe"));

    public UpdateInfo? Available { get; private set; }
    public DateTime? LastChecked { get; private set; }
    public string? LastError { get; private set; }
    public bool Installing { get; private set; }

    /// <summary>Raised on the UI thread whenever the state above changes.</summary>
    public event Action? Changed;

    private static HttpClient CreateClient()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd($"Sweetch/{Current}");
        return http;
    }

    public async Task<UpdateInfo?> CheckAsync()
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{Repo}/releases/latest");
            request.Headers.Accept.ParseAdd("application/vnd.github+json");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var response = await Http.SendAsync(request, cts.Token);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                Available = null; // nothing released yet
            }
            else
            {
                response.EnsureSuccessStatusCode();
                var info = UpdateInfo.Parse(await response.Content.ReadAsStringAsync(cts.Token));
                // Only installers published in our own repository.
                Available = info is { InstallerUrl: { } url } && info.IsNewerThan(Current)
                    && url.StartsWith($"https://github.com/{Repo}/releases/download/", StringComparison.Ordinal) ? info : null;
            }
            LastError = null;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or System.Text.Json.JsonException or KeyNotFoundException or InvalidOperationException)
        {
            // Offline, rate-limited, odd response: try again at the next check.
            LastError = ex.Message;
        }
        LastChecked = DateTime.Now;
        Changed?.Invoke();
        return Available;
    }

    /// <summary>Checks a little after start and then twice a day, while automatic updates are on.</summary>
    public async void StartBackgroundChecks(AppSettings settings, Action<UpdateInfo> found)
    {
        await Task.Delay(TimeSpan.FromSeconds(20));
        while (true)
        {
            if (settings.AutoUpdate && !Installing && await CheckAsync() is { } update && update.Tag != settings.SkippedVersion)
                found(update);
            await Task.Delay(TimeSpan.FromHours(12));
        }
    }

    /// <summary>Downloads and verifies the installer, runs it silently and quits; it restarts Sweetch when done.</summary>
    public async Task InstallAsync(UpdateInfo update, IProgress<double>? progress = null)
    {
        if (update.InstallerUrl is null || !IsInstalled)
        {
            Open(update.PageUrl.Length > 0 ? update.PageUrl : Page);
            return;
        }
        Installing = true;
        Changed?.Invoke();
        try
        {
            var path = Path.Combine(Path.GetTempPath(), $"Sweetch-{update.Version}-Setup.exe");
            await DownloadAsync(update, path, progress);
            // /UPDATED=1 makes the installer start Sweetch again (installer/Sweetch.iss).
            Process.Start(new ProcessStartInfo(path, "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP- /CLOSEAPPLICATIONS /UPDATED=1") { UseShellExecute = true });
            Application.Current.Shutdown();
        }
        finally
        {
            Installing = false;
            Changed?.Invoke();
        }
    }

    private static async Task DownloadAsync(UpdateInfo update, string path, IProgress<double>? progress)
    {
        using var response = await Http.GetAsync(update.InstallerUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        var total = update.InstallerSize > 0 ? update.InstallerSize : response.Content.Headers.ContentLength ?? 0;
        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        long done = 0;
        await using (var source = await response.Content.ReadAsStreamAsync())
        await using (var file = File.Create(path))
        {
            var buffer = new byte[1 << 16];
            int read;
            while ((read = await source.ReadAsync(buffer)) > 0)
            {
                await file.WriteAsync(buffer.AsMemory(0, read));
                sha.AppendData(buffer, 0, read);
                done += read;
                if (total > 0)
                    progress?.Report((double)done / total);
            }
        }
        var hash = Convert.ToHexString(sha.GetHashAndReset()).ToLowerInvariant();
        if ((update.InstallerSize > 0 && done != update.InstallerSize) || (update.InstallerSha256 is { } expected && hash != expected))
        {
            File.Delete(path);
            throw new InvalidDataException(T("The update file is damaged (checksum mismatch). Try again.", "Файл обновления повреждён (не совпала контрольная сумма). Попробуйте ещё раз."));
        }
    }

    public static void Open(string url) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
}

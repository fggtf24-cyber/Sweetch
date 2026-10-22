using ImageMagick;
using Sweetch.Core;
using Sweetch.Core.Backends;

namespace Sweetch.Tests;

public class RawTests
{
    [Theory]
    [InlineData("IMG_0001.CR3")]
    [InlineData("DSC_0001.NEF")]
    [InlineData("DSC00001.ARW")]
    [InlineData("IMG_0001.DNG")]
    [InlineData("DSCF0001.RAF")]
    public void Raw_files_are_image_inputs(string name)
    {
        var key = Formats.KeyOf(name);
        Assert.Contains(key, Formats.Raw);
        Assert.Contains(key, Formats.ImageInputs);
        // RAW can't be written back, so in-place edits are saved as JPG.
        Assert.Equal("jpg", ImageBackend.OutputFormatFor(key));
    }

    /// <summary>
    /// Real camera files are too big for the repository; CI downloads a few
    /// (see .github/workflows/build.yml) and points SWEETCH_RAW_SAMPLES at them.
    /// </summary>
    [Fact]
    public async Task Real_raw_samples_convert_to_jpg_and_png()
    {
        var dir = Environment.GetEnvironmentVariable("SWEETCH_RAW_SAMPLES");
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            return;
        var samples = Directory.GetFiles(dir).Where(f => Formats.Raw.Contains(Formats.KeyOf(f))).ToList();
        Assert.NotEmpty(samples);
        var outDir = Directory.CreateTempSubdirectory("sweetch-raw-").FullName;
        foreach (var raw in samples)
        {
            foreach (var target in new[] { "jpg", "png" })
            {
                var output = Path.Combine(outDir, Path.GetFileNameWithoutExtension(raw) + "." + target);
                await ImageBackend.ConvertAsync(raw, output, target, new Settings(), CancellationToken.None);
                using var image = new MagickImage(output);
                // Developed full-size frame, not the small embedded thumbnail.
                Assert.True(Math.Max(image.Width, image.Height) >= 2000, $"{Path.GetFileName(raw)} → {image.Width}×{image.Height}");
            }
        }
    }
}

public class ConversionQualityTests
{
    private static string Sample(string dir, string name, uint quality)
    {
        // A noisy gradient: lossy encoders can't hide changes in it.
        using var image = new MagickImage("plasma:red-blue", 96, 64);
        image.Quality = quality;
        var path = Path.Combine(dir, name);
        image.Write(path);
        return path;
    }

    [Fact]
    public async Task Lossless_source_stays_lossless()
    {
        var dir = Directory.CreateTempSubdirectory("sweetch-quality-").FullName;
        var png = Sample(dir, "in.png", 0);
        var webp = Path.Combine(dir, "out.webp");
        await ImageBackend.ConvertAsync(png, webp, "webp", new Settings(), CancellationToken.None);
        using var a = new MagickImage(png);
        using var b = new MagickImage(webp);
        Assert.Equal(0, a.Compare(b, ErrorMetric.Absolute));

        var jpg = Path.Combine(dir, "out.jpg");
        await ImageBackend.ConvertAsync(png, jpg, "jpg", new Settings(), CancellationToken.None);
        using var j = new MagickImage(jpg);
        Assert.Equal(100u, j.Quality);
    }

    [Fact]
    public async Task Lossy_source_keeps_its_quality()
    {
        var dir = Directory.CreateTempSubdirectory("sweetch-quality-").FullName;
        var jpg = Sample(dir, "in.jpg", 77);
        var output = Path.Combine(dir, "out.jpg");
        // The setting (50) must not win: only the format changes, and JPEG records its quality, unlike WebP.
        await ImageBackend.ConvertAsync(jpg, output, "jpg", new Settings { JpegQuality = 50 }, CancellationToken.None);
        using var result = new MagickImage(output);
        Assert.Equal(77u, result.Quality);
    }
}

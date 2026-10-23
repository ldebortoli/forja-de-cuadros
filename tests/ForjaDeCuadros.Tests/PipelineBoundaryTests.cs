using System.IO;
using System.Text.Json;
using Xunit;

namespace ForjaDeCuadros.Tests;

public sealed class PipelineBoundaryTests
{
    [Fact]
    public void QaDataPathOverrideIsProcessScopedAndDefaultPathIsPreserved()
    {
        const string variable = "FORJA_DATA_DIRECTORY";
        var previous = Environment.GetEnvironmentVariable(variable);
        try
        {
            Environment.SetEnvironmentVariable(variable, null);
            Assert.Equal(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ForjaDeCuadros"), AppDataPaths.DataDirectory);
            var isolated = Path.Combine(Path.GetTempPath(), "forja-r19-data-" + Guid.NewGuid().ToString("N"));
            Environment.SetEnvironmentVariable(variable, isolated);
            Assert.Equal(isolated, AppDataPaths.DataDirectory);
        }
        finally { Environment.SetEnvironmentVariable(variable, previous); }
    }

    [Theory]
    [InlineData(23, 24, 20, 12)]
    [InlineData(2049, 24, 20, 12)]
    [InlineData(24, 23, 20, 12)]
    [InlineData(24, 2049, 20, 12)]
    [InlineData(24, 24, 0, 12)]
    [InlineData(24, 24, 24, 12)]
    [InlineData(24, 24, 20, -1)]
    [InlineData(24, 24, 20, 24)]
    public void InvalidCanvasGroundAndRootAreRejectedBeforeReadingFiles(int width, int height, int ground, int root)
    {
        var frames = Enumerable.Range(1, 16).Select(number => new FrameItem { Number = number }).ToArray();
        var options = new ProcessingOptions { CanvasWidth = width, CanvasHeight = height, GroundY = ground, RootX = root };
        Assert.Throws<InvalidOperationException>(() => FrameProcessor.Process(frames, options, Path.GetTempPath()));
    }

    [Theory]
    [InlineData(RegistrationMode.RootAndGround)]
    [InlineData(RegistrationMode.GroundPreserveMotion)]
    [InlineData(RegistrationMode.CameraLocked)]
    public void AllRegistrationModesPreserveFrameOrderTimestampsAndNonemptyImages(RegistrationMode mode)
    {
        var directory = Path.Combine(Path.GetTempPath(), "forja-r19-registration-" + Guid.NewGuid().ToString("N"));
        var frames = Enumerable.Range(1, 16).Select(number =>
        {
            var buffer = FrameBuffer.CreateSolid(32, 32, 0, 0, 0, 0);
            buffer.DrawRectangle(3 + number % 3, 3, 6, 10 + number % 3, (byte)(30 + number * 9), 30, 20);
            var path = Path.Combine(directory, $"source-{number}.png");
            buffer.SavePng(path);
            return new FrameItem { Number = number, Timestamp = number / 12.0, ImagePath = path };
        }).Reverse().ToArray();
        var options = new ProcessingOptions { ChromaEnabled = false, CanvasWidth = 64, CanvasHeight = 64, GroundY = 50, RootX = 32, Padding = 4, RegistrationMode = mode };
        var output = FrameProcessor.Process(frames, options, Path.Combine(directory, "processed"));
        Assert.Equal(Enumerable.Range(1, 16), output.Select(frame => frame.Number));
        Assert.All(output, frame =>
        {
            Assert.Equal(frame.Number / 12.0, frame.Timestamp);
            Assert.False(frame.Bounds.IsEmpty);
            Assert.True(File.Exists(frame.FilePath));
            Assert.Equal(frame.Sha256, FrameBuffer.LoadPng(frame.FilePath).Sha256());
        });
    }

    [Fact]
    public void FullyTransparentFramesAreRejectedAfterChroma()
    {
        var directory = Path.Combine(Path.GetTempPath(), "forja-r19-empty-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "empty.png");
        FrameBuffer.CreateSolid(4, 4, 0, 0, 0, 0).SavePng(path);
        var frames = Enumerable.Range(1, 16).Select(number => new FrameItem { Number = number, ImagePath = path }).ToArray();
        var error = Assert.Throws<InvalidOperationException>(() => FrameProcessor.Process(frames, new ProcessingOptions(), directory));
        Assert.Contains("vacio", error.Message);
    }

    [Fact]
    public void FrameSelectionNotifiesOnlyOnChangesAndPreviewsUseSyntheticPng()
    {
        var path = Path.Combine(Path.GetTempPath(), "forja-r19-model-" + Guid.NewGuid().ToString("N") + ".png");
        var buffer = FrameBuffer.CreateSolid(2, 2, 220, 30, 20);
        buffer.SavePng(path);
        var item = new FrameItem { Number = 1, Timestamp = 0.25, ImagePath = path };
        var events = new List<string?>();
        item.PropertyChanged += (_, args) => events.Add(args.PropertyName);
        item.IsSelected = false;
        item.IsSelected = true;
        item.IsSelected = true;
        item.IsSelected = false;
        Assert.Equal(new[] { nameof(FrameItem.IsSelected), nameof(FrameItem.IsSelected) }, events);
        Assert.False(item.IsSelected);
        Assert.Contains("01", item.Caption);
        Assert.Same(item.Thumbnail, item.Thumbnail);
        Assert.True(item.Thumbnail.IsFrozen);
        var processed = new ProcessedFrame { Number = 1, Timestamp = 0.25, FilePath = path, Buffer = buffer, Bounds = buffer.FindBounds(), RootX = 0.5, Sha256 = buffer.Sha256() };
        Assert.Equal("01 / 16", processed.Caption);
        Assert.True(processed.Preview.IsFrozen);
    }

    [Fact]
    public void ProcessingExportAndVideoMetadataRoundTripAsJson()
    {
        CheckJsonRoundTrip(new ProcessingOptions { ChromaTolerance = 12.25, AlphaSoftnessPercent = 3.5, RegistrationMode = RegistrationMode.CameraLocked });
        CheckJsonRoundTrip(new ExportOptions { BaseFolder = "synthetic", AnimationName = "fixture", Columns = 8, FramesPerSecond = 24.5, GodotTexturePath = "res://synthetic.png" });
        CheckJsonRoundTrip(new ExportResult { Folder = "synthetic", AtlasPath = "atlas.png", ReviewPath = "review.png" });
        var video = new VideoInfo { Width = 32, Height = 24, Duration = 1.25, FramesPerSecond = 24.5 };
        CheckJsonRoundTrip(video);
        Assert.Contains("FPS", video.Summary);
    }

    [Theory]
    [InlineData(FindingLevel.Pass, "#287F76")]
    [InlineData(FindingLevel.Warning, "#B87927")]
    [InlineData(FindingLevel.Error, "#C73B2F")]
    [InlineData(FindingLevel.Info, "#6C665C")]
    public void AuditSeverityKeepsItsDisplayColorAndSymbol(FindingLevel level, string color)
    {
        var finding = new AuditFinding { Level = level, Message = "synthetic" };
        Assert.Equal(color, finding.Color);
        Assert.False(string.IsNullOrEmpty(finding.Symbol));
    }

    [Fact]
    public void EmptyAndClippedAuditsReportErrorsWithoutInventingFrames()
    {
        var empty = AuditService.Audit(Array.Empty<ProcessedFrame>(), new ProcessingOptions());
        Assert.True(empty.HasErrors);
        Assert.Equal(0, empty.UniqueFrames);
        var blank = FrameBuffer.CreateSolid(8, 8, 0, 0, 0, 0);
        var clipped = FrameBuffer.CreateSolid(8, 8, 255, 0, 0);
        var frames = new[] { MakeFrame(blank), MakeFrame(clipped) };
        var audit = AuditService.Audit(frames, new ProcessingOptions { RegistrationMode = RegistrationMode.CameraLocked });
        Assert.True(audit.HasErrors);
        Assert.Contains(audit.Findings, finding => finding.Level == FindingLevel.Error && finding.Message.Contains("vacios"));
        Assert.Contains(audit.Findings, finding => finding.Level == FindingLevel.Error && finding.Message.Contains("borde"));
    }

    [Theory]
    [InlineData(11, FindingLevel.Warning)]
    [InlineData(13, FindingLevel.Error)]
    public void AuditDistinguishesModerateAndLargeHeightDrift(int changedHeight, FindingLevel level)
    {
        var frames = new[] { HeightFrame(10), HeightFrame(changedHeight), HeightFrame(10) };
        var audit = AuditService.Audit(frames, new ProcessingOptions { GroundY = 11, RootX = 0 });
        Assert.Contains(audit.Findings, finding => finding.Level == level && finding.Message.Contains("altura"));
        Assert.Contains(audit.Findings, finding => finding.Level == FindingLevel.Warning && finding.Message.Contains("raiz"));
    }

    [Theory]
    [InlineData(3, FindingLevel.Warning)]
    [InlineData(5, FindingLevel.Error)]
    public void AuditDistinguishesModerateAndLargeLoopSeams(int count, FindingLevel level)
    {
        var frames = Enumerable.Range(0, count).Select(index => MakeFrame(FrameBuffer.CreateSolid(4, 4, (byte)(index * 20), 0, 0))).ToArray();
        var audit = AuditService.Audit(frames, new ProcessingOptions { RegistrationMode = RegistrationMode.GroundPreserveMotion });
        Assert.Contains(audit.Findings, finding => finding.Level == level && finding.Message.StartsWith("Relacion"));
    }

    [Fact]
    public void AuditRecognizesSixteenUniqueSyntheticFrames()
    {
        var frames = Enumerable.Range(0, 16).Select(index => HeightFrame(10, (byte)(index + 1))).ToArray();
        var audit = AuditService.Audit(frames, new ProcessingOptions { GroundY = 11, RootX = 4 });
        Assert.Equal(16, audit.UniqueFrames);
        Assert.Contains(audit.Findings, finding => finding.Level == FindingLevel.Pass && finding.Message.Contains("huellas"));
    }

    [Fact]
    public void AuditGuardsAgainstNonPositiveHeightFromOverflowedMetadata()
    {
        var frame = MakeFrame(FrameBuffer.CreateSolid(1, 1, 255, 0, 0));
        frame.Bounds = new PixelBounds(0, 0, 0, int.MaxValue);
        var report = AuditService.Audit(new[] { frame }, new ProcessingOptions { RegistrationMode = RegistrationMode.CameraLocked });
        Assert.Equal(0, report.HeightDriftPercent);
        Assert.True(report.HasErrors);
    }

    private static ProcessedFrame HeightFrame(int height, byte red = 220)
    {
        var buffer = FrameBuffer.CreateSolid(32, 32, 0, 0, 0, 0);
        buffer.DrawRectangle(2, 2, 4, height, red, 30, 20);
        return MakeFrame(buffer);
    }

    private static ProcessedFrame MakeFrame(FrameBuffer buffer) => new() { Buffer = buffer, Bounds = buffer.FindBounds(), RootX = buffer.FindRootX(buffer.FindBounds()), Sha256 = buffer.Sha256() };

    private static void CheckJsonRoundTrip<T>(T value)
    {
        var json = JsonSerializer.Serialize(value);
        Assert.Equal(json, JsonSerializer.Serialize(JsonSerializer.Deserialize<T>(json)));
    }
}

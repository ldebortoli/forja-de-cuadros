using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ForjaDeCuadros;
using Xunit;

namespace ForjaDeCuadros.Tests;

public sealed class PipelineTests
{
    [Fact]
    public void FrameProcessor_RequiresExactlySixteenFrames()
    {
        var options = new ProcessingOptions();
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => FrameProcessor.Process(Array.Empty<FrameItem>(), options, Path.GetTempPath()));
        Assert.Contains("exactamente 16", error.Message);
    }

    [Fact]
    public void FrameProcessor_RegistersGroundAndRootAndWritesSixteenPngs()
    {
        string workspace = CreateWorkspace();
        try
        {
            var items = new List<FrameItem>();
            for (int index = 0; index < 16; index++)
            {
                FrameBuffer source = FrameBuffer.CreateSolid(24, 24, 0, 0, 0, 0);
                source.DrawRectangle(3 + index % 5, 4, 7 + index % 3, 13 + index % 4, (byte)(30 + index * 8), 40, 50);
                string path = Path.Combine(workspace, $"source_{index + 1:00}.png");
                source.SavePng(path);
                items.Add(new FrameItem { Number = index + 1, Timestamp = index / 12.0, ImagePath = path, IsSelected = true });
            }

            var options = new ProcessingOptions
            {
                ChromaEnabled = false,
                CanvasWidth = 64,
                CanvasHeight = 64,
                GroundY = 56,
                RootX = 32,
                Padding = 4,
                RegistrationMode = RegistrationMode.RootAndGround
            };

            List<ProcessedFrame> result = FrameProcessor.Process(items, options, Path.Combine(workspace, "processed"));

            Assert.Equal(16, result.Count);
            Assert.All(result, frame =>
            {
                Assert.True(File.Exists(frame.FilePath));
                Assert.InRange(frame.Bounds.Bottom, 55, 56);
                Assert.InRange(frame.RootX, 30.5, 33.5);
            });
            Assert.Equal(16, result.Select(frame => frame.Sha256).Distinct().Count());
        }
        finally
        {
            Directory.Delete(workspace, true);
        }
    }

    [Fact]
    public void Audit_FlagsDuplicateFrames()
    {
        FrameBuffer buffer = FrameBuffer.CreateSolid(16, 16, 0, 0, 0, 0);
        buffer.DrawRectangle(4, 4, 6, 8, 220, 30, 20);
        PixelBounds bounds = buffer.FindBounds();
        var frames = Enumerable.Range(1, 16).Select(number => new ProcessedFrame
        {
            Number = number,
            Buffer = buffer.Clone(),
            Bounds = bounds,
            RootX = 6.5,
            Sha256 = "same"
        }).ToList();
        var options = new ProcessingOptions
        {
            CanvasWidth = 16,
            CanvasHeight = 16,
            GroundY = bounds.Bottom,
            RootX = 7,
            RegistrationMode = RegistrationMode.RootAndGround
        };

        AuditReport report = AuditService.Audit(frames, options);

        Assert.True(report.HasErrors);
        Assert.Equal(1, report.UniqueFrames);
        Assert.Contains(report.Findings, finding => finding.Level == FindingLevel.Error && finding.Message.Contains("unicos"));
        Assert.Contains(report.Findings, finding => finding.Level == FindingLevel.Pass && finding.Message.Contains("Ningun cuadro vacio"));
    }

    private static string CreateWorkspace()
    {
        string path = Path.Combine(Path.GetTempPath(), "ForjaDeCuadros-Tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}

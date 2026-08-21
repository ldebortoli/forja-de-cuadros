using ForjaDeCuadros;
using System.IO;
using Xunit;

namespace ForjaDeCuadros.Tests;

public sealed class FrameBufferTests
{
    [Fact]
    public void ApplyChroma_RemovesGreenAndPreservesSubject()
    {
        FrameBuffer source = FrameBuffer.CreateSolid(12, 12, 0, 255, 0);
        source.DrawRectangle(4, 3, 4, 6, 220, 30, 20);
        var options = new ProcessingOptions
        {
            ChromaEnabled = true,
            ChromaTolerance = 30,
            EdgeSoftness = 10,
            HaloPixels = 0,
            IslandCleanupPixels = 0
        };

        FrameBuffer result = source.ApplyChroma(options);

        Assert.Equal(0, result.PixelAt(0, 0).A);
        Assert.True(result.PixelAt(5, 5).A > 240);
        Assert.True(result.PixelAt(5, 5).R > 200);
        Assert.Equal(new PixelBounds(4, 3, 7, 8).Width, result.FindBounds().Width);
    }

    [Fact]
    public void ApplyChroma_RemovesOnlySmallDisconnectedIslands()
    {
        FrameBuffer source = FrameBuffer.CreateSolid(10, 10, 0, 0, 0, 0);
        source.DrawRectangle(2, 2, 3, 3, 220, 30, 20);
        source.DrawRectangle(8, 8, 1, 1, 220, 30, 20);
        var options = new ProcessingOptions
        {
            ChromaEnabled = true,
            HaloPixels = 0,
            IslandCleanupPixels = 1
        };

        FrameBuffer result = source.ApplyChroma(options);

        Assert.True(result.PixelAt(3, 3).A > 0);
        Assert.Equal(0, result.PixelAt(8, 8).A);
    }

    [Fact]
    public void RenderToCanvas_InterpolatesPremultipliedColorWithoutGreenHalo()
    {
        FrameBuffer source = FrameBuffer.CreateSolid(2, 1, 0, 0, 0, 0);
        source.DrawRectangle(0, 0, 1, 1, 255, 0, 0);

        FrameBuffer result = source.RenderToCanvas(4, 2, 2, 0, 0);
        var edge = result.PixelAt(1, 0);

        Assert.True(edge.A > 0 && edge.A < 255);
        Assert.True(edge.R > 240);
        Assert.Equal(0, edge.G);
        Assert.Equal(0, edge.B);
    }

    [Fact]
    public void ApplyAlphaCutoff_RemovesWeakHaloAndKeepsSubjectOpaque()
    {
        FrameBuffer source = FrameBuffer.CreateSolid(4, 1, 220, 30, 20, 255);
        source.Pixels[3] = 0;
        source.Pixels[7] = 13;
        source.Pixels[11] = 128;
        var options = new ProcessingOptions
        {
            AlphaCutoffEnabled = true,
            AlphaCutoffPercent = 10,
            AlphaSoftnessPercent = 0
        };

        FrameBuffer result = source.ApplyAlphaCutoff(options);

        Assert.Equal(0, result.PixelAt(0, 0).A);
        Assert.Equal(0, result.PixelAt(1, 0).A);
        Assert.Equal(255, result.PixelAt(2, 0).A);
        Assert.Equal(255, result.PixelAt(3, 0).A);
        Assert.Equal(13, source.PixelAt(1, 0).A);
    }

    [Fact]
    public void ApplyAlphaCutoff_SoftensTransitionAroundThreshold()
    {
        FrameBuffer source = FrameBuffer.CreateSolid(3, 1, 220, 30, 20, 255);
        source.Pixels[3] = 102;
        source.Pixels[7] = 128;
        source.Pixels[11] = 153;
        var options = new ProcessingOptions
        {
            AlphaCutoffEnabled = true,
            AlphaCutoffPercent = 50,
            AlphaSoftnessPercent = 10
        };

        FrameBuffer result = source.ApplyAlphaCutoff(options);

        Assert.Equal(0, result.PixelAt(0, 0).A);
        Assert.InRange(result.PixelAt(1, 0).A, 130, 132);
        Assert.Equal(255, result.PixelAt(2, 0).A);
    }

    [Fact]
    public void ApplyAlphaCutoff_WhenDisabledPreservesOriginalAlpha()
    {
        FrameBuffer source = FrameBuffer.CreateSolid(1, 1, 220, 30, 20, 37);

        FrameBuffer result = source.ApplyAlphaCutoff(new ProcessingOptions { AlphaCutoffEnabled = false });

        Assert.Equal(37, result.PixelAt(0, 0).A);
    }

    [Fact]
    public void BoundsRootBorderAndDifference_ReportExpectedGeometry()
    {
        FrameBuffer buffer = FrameBuffer.CreateSolid(8, 8, 0, 0, 0, 0);
        buffer.DrawRectangle(2, 3, 4, 4, 40, 50, 60);
        PixelBounds bounds = buffer.FindBounds();

        Assert.Equal(2, bounds.Left);
        Assert.Equal(6, bounds.Bottom);
        Assert.InRange(buffer.FindRootX(bounds), 3.4, 3.6);
        Assert.False(buffer.TouchesBorder());
        Assert.Equal(0, FrameBuffer.MeanAbsoluteDifference(buffer, buffer.Clone()));

        buffer.DrawRectangle(0, 0, 1, 1, 255, 255, 255);
        Assert.True(buffer.TouchesBorder());
        Assert.True(FrameBuffer.MeanAbsoluteDifference(buffer, FrameBuffer.CreateSolid(8, 8, 0, 0, 0, 0)) > 0);
    }

    [Fact]
    public void CompositeOnColor_FlattensTransparentAndSemitransparentPixels()
    {
        FrameBuffer source = FrameBuffer.CreateSolid(3, 1, 255, 0, 0, 255);
        source.Pixels[3] = 0;
        source.Pixels[7] = 128;

        FrameBuffer result = source.CompositeOnColor(0, 255, 0);

        Assert.Equal((0, 255, 0, 255), result.PixelAt(0, 0));
        Assert.InRange(result.PixelAt(1, 0).R, 127, 128);
        Assert.InRange(result.PixelAt(1, 0).G, 127, 128);
        Assert.Equal((255, 0, 0, 255), result.PixelAt(2, 0));
        Assert.Equal(0, source.PixelAt(0, 0).A);
    }

    [Fact]
    public void ImagePreparationService_WritesOpaqueChromaPngAndReportsTransparency()
    {
        string folder = Path.Combine(Path.GetTempPath(), "forja-image-prep-" + Guid.NewGuid().ToString("N"));
        string sourcePath = Path.Combine(folder, "personaje.png");
        string outputPath = Path.Combine(folder, "salida.png");
        try
        {
            FrameBuffer source = FrameBuffer.CreateSolid(2, 1, 30, 40, 50, 255);
            source.Pixels[3] = 0;
            source.SavePng(sourcePath);

            PreparedImageResult result = ImagePreparationService.Prepare(sourcePath, outputPath, 0, 102, 255);
            FrameBuffer prepared = FrameBuffer.LoadPng(outputPath);

            Assert.True(result.HadTransparency);
            Assert.Equal(1, result.TransparentPixelCount);
            Assert.Equal(2, result.PixelCount);
            Assert.Equal((0, 102, 255, 255), prepared.PixelAt(0, 0));
            Assert.Equal((30, 40, 50, 255), prepared.PixelAt(1, 0));
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
    }

    [Fact]
    public void ImagePreparationService_CreatesSafeUniqueOutputPaths()
    {
        string folder = Path.Combine(Path.GetTempPath(), "forja-prepared-images");

        string named = ImagePreparationService.CreateOutputPath("personaje.png", folder, "chroma-verde");
        string fallback = ImagePreparationService.CreateOutputPath(".png", folder, "");

        Assert.Equal(folder, Path.GetDirectoryName(named));
        Assert.StartsWith("personaje-chroma-verde-", Path.GetFileName(named));
        Assert.StartsWith("imagen-chroma-", Path.GetFileName(fallback));
        Assert.EndsWith(".png", named);
        Assert.Throws<ArgumentException>(() => ImagePreparationService.CreateOutputPath("", folder, "verde"));
        Assert.Throws<ArgumentException>(() => ImagePreparationService.CreateOutputPath("personaje.png", "", "verde"));
    }

    [Fact]
    public void ImagePreparationService_RejectsMissingOrUnreadableInputs()
    {
        string folder = Path.Combine(Path.GetTempPath(), "forja-image-invalid-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string invalidPath = Path.Combine(folder, "invalida.png");
        File.WriteAllText(invalidPath, "esto no es una imagen");
        try
        {
            Assert.Throws<FileNotFoundException>(() => ImagePreparationService.Prepare(Path.Combine(folder, "falta.png"), Path.Combine(folder, "salida.png"), 0, 255, 0));
            Assert.Throws<ArgumentException>(() => ImagePreparationService.Prepare(invalidPath, "", 0, 255, 0));
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => ImagePreparationService.Prepare(invalidPath, Path.Combine(folder, "salida.png"), 0, 255, 0));
            Assert.Contains("Windows no pudo abrir", exception.Message);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }
}

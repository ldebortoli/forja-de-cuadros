using ForjaDeCuadros;
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
}

using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Xunit;

namespace ForjaDeCuadros.Tests;

public sealed class RasterBoundaryTests
{
    [Theory]
    [InlineData(0, 255, 0, 220, 30, 20)]
    [InlineData(255, 0, 0, 20, 30, 220)]
    [InlineData(0, 0, 255, 220, 30, 20)]
    public void ChromaNeverAddsKeyColorToOpaqueForeground(byte keyRed, byte keyGreen, byte keyBlue, byte red, byte green, byte blue)
    {
        var source = FrameBuffer.CreateSolid(1, 1, red, green, blue);
        var result = source.ApplyChroma(new ProcessingOptions { KeyR = keyRed, KeyG = keyGreen, KeyB = keyBlue, HaloPixels = 0, IslandCleanupPixels = 0 });
        Assert.Equal(source.PixelAt(0, 0), result.PixelAt(0, 0));
    }

    [Fact]
    public void InvalidBufferSizeAndMissingPngPathAreRejected()
    {
        Assert.Throws<ArgumentException>(() => new FrameBuffer(2, 1, new byte[4]));
        Assert.Throws<ArgumentNullException>(() => FrameBuffer.CreateSolid(1, 1, 0, 0, 0).SavePng(null!));
    }

    [Fact]
    public void BgrPngIsConvertedToOpaqueRgbaAndFrozenForDisplay()
    {
        var path = Path.Combine(Path.GetTempPath(), "forja-r19-bgr-" + Guid.NewGuid().ToString("N") + ".png");
        var bitmap = BitmapSource.Create(2, 1, 96, 96, PixelFormats.Bgr24, null, new byte[] { 10, 20, 30, 40, 50, 60 }, 6);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using (var stream = File.Create(path)) encoder.Save(stream);
        var loaded = FrameBuffer.LoadPng(path);
        Assert.Equal(((byte)30, (byte)20, (byte)10, (byte)255), loaded.PixelAt(0, 0));
        Assert.True(loaded.ToBitmapSource().IsFrozen);
        Assert.Equal(2, loaded.ToBitmapSource().PixelWidth);
    }

    [Theory]
    [InlineData(0, 255, 0)]
    [InlineData(255, 0, 0)]
    [InlineData(0, 0, 255)]
    [InlineData(255, 0, 255)]
    public void ChromaSupportsEachDominantChannelAndHueWrap(byte red, byte green, byte blue)
    {
        var source = FrameBuffer.CreateSolid(7, 1, red, green, blue);
        source.DrawRectangle(1, 0, 1, 1, 255, 0, 0);
        source.DrawRectangle(2, 0, 1, 1, 0, 255, 0);
        source.DrawRectangle(3, 0, 1, 1, 0, 0, 255);
        source.DrawRectangle(4, 0, 1, 1, 128, 128, 128);
        source.DrawRectangle(5, 0, 1, 1, 0, 0, 0);
        source.DrawRectangle(6, 0, 1, 1, 255, 0, 255);
        var result = source.ApplyChroma(new ProcessingOptions { KeyR = red, KeyG = green, KeyB = blue, HaloPixels = 0, IslandCleanupPixels = 0 });
        Assert.Equal(0, result.PixelAt(0, 0).A);
        Assert.Equal(255, source.PixelAt(0, 0).A);
        Assert.Equal(source.Width, result.Width);
    }

    [Fact]
    public void HaloErosionClearsCanvasEdgesAndPreservesOpaqueInterior()
    {
        var source = FrameBuffer.CreateSolid(7, 7, 255, 0, 0);
        source.DrawRectangle(0, 0, 1, 1, 0, 0, 0, 0);
        var result = source.ApplyChroma(new ProcessingOptions { HaloPixels = 1, IslandCleanupPixels = 0 });
        Assert.Equal(0, result.PixelAt(3, 0).A);
        Assert.Equal(0, result.PixelAt(0, 0).A);
        Assert.Equal(255, result.PixelAt(3, 3).A);
        Assert.Equal(255, source.PixelAt(3, 0).A);
    }

    [Fact]
    public void IslandCleanupKeepsLargeComponentsTouchingEveryCanvasEdge()
    {
        var source = FrameBuffer.CreateSolid(5, 5, 255, 0, 0);
        var result = source.ApplyChroma(new ProcessingOptions { HaloPixels = 0, IslandCleanupPixels = 1 });
        Assert.Equal(source.Sha256(), result.Sha256());
    }

    [Fact]
    public void EmptyAndFaintAlphaBoundsHaveStableRootFallbacks()
    {
        var empty = FrameBuffer.CreateSolid(4, 3, 0, 0, 0, 0);
        Assert.True(empty.FindBounds().IsEmpty);
        Assert.Equal(0, empty.FindBounds().Width);
        Assert.Equal(0, empty.FindBounds().Height);
        Assert.Equal(2, empty.FindRootX(empty.FindBounds()));
        Assert.True(new PixelBounds(0, 1, 0, 0).IsEmpty);
        empty.DrawRectangle(1, 1, 2, 1, 255, 0, 0, 12);
        Assert.Equal(1.5, empty.FindRootX(empty.FindBounds()));
        Assert.Equal(double.PositiveInfinity, FrameBuffer.MeanAbsoluteDifference(empty, FrameBuffer.CreateSolid(3, 3, 0, 0, 0)));
        Assert.Equal(double.PositiveInfinity, FrameBuffer.MeanAbsoluteDifference(empty, FrameBuffer.CreateSolid(4, 2, 0, 0, 0)));
    }

    [Fact]
    public void FractionalCanvasOffsetsClipOutsideAndPreserveInteriorColor()
    {
        var source = FrameBuffer.CreateSolid(2, 2, 220, 30, 20);
        var result = source.RenderToCanvas(6, 6, 1, 2.5, 2.5);
        Assert.Equal(0, result.PixelAt(0, 0).A);
        Assert.Equal(255, result.PixelAt(3, 3).A);
        Assert.Equal(220, result.PixelAt(3, 3).R);
        Assert.InRange(result.PixelAt(2, 2).A, 63, 64);
    }

    [Fact]
    public void BlitClipsBothAxesWithoutChangingSource()
    {
        var source = FrameBuffer.CreateSolid(2, 2, 220, 30, 20);
        var destination = FrameBuffer.CreateSolid(3, 3, 0, 0, 0, 0);
        destination.Blit(source, -1, -1);
        Assert.Equal(source.PixelAt(1, 1), destination.PixelAt(0, 0));
        destination.Blit(source, 2, 2);
        Assert.Equal(source.PixelAt(0, 0), destination.PixelAt(2, 2));
        var hash = destination.Sha256();
        foreach (var (x, y) in new[] { (-3, 0), (3, 0), (0, -3), (0, 3) }) destination.Blit(source, x, y);
        Assert.Equal(hash, destination.Sha256());
    }

    [Theory]
    [InlineData(2, 0)]
    [InlineData(2, 4)]
    [InlineData(0, 2)]
    [InlineData(4, 2)]
    public void BorderDetectionChecksAllFourSides(int x, int y)
    {
        var frame = FrameBuffer.CreateSolid(5, 5, 0, 0, 0, 0);
        frame.DrawRectangle(x, y, 1, 1, 255, 0, 0);
        Assert.True(frame.TouchesBorder());
    }
}

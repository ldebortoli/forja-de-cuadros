using ForjaDeCuadros;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ForjaDeCuadros.Tests;

public sealed class LoopDetectionServiceTests
{
    [Fact]
    public void DetectSignatures_RequiresAtLeastSixteenFrames()
    {
        double[][] signatures = Enumerable.Range(0, 15).Select(index => new[] { index / 15.0 }).ToArray();

        LoopDetectionResult result = LoopDetectionService.DetectSignatures(signatures);

        Assert.False(result.IsAvailable);
        Assert.Empty(result.SelectedIndices);
    }

    [Fact]
    public void DetectSignatures_WithExactlySixteenSelectsAllAndDoesNotInventConfidence()
    {
        double[][] signatures = Enumerable.Range(0, 16).Select(index => new[] { index / 16.0 }).ToArray();

        LoopDetectionResult result = LoopDetectionService.DetectSignatures(signatures);

        Assert.True(result.IsAvailable);
        Assert.Equal(0, result.StartIndex);
        Assert.Equal(16, result.EndExclusiveIndex);
        Assert.Equal(Enumerable.Range(0, 16), result.SelectedIndices);
        Assert.Equal(0, result.Confidence);
    }

    [Fact]
    public void DetectSignatures_FindsRepeatedCycleAfterLeadIn()
    {
        var signatures = new List<double[]>();
        signatures.Add(new[] { 0.91, 0.02, 0.12 });
        signatures.Add(new[] { 0.84, 0.11, 0.04 });
        signatures.Add(new[] { 0.78, 0.19, 0.21 });
        for (int repetition = 0; repetition < 2; repetition++)
        {
            for (int phase = 0; phase < 20; phase++) signatures.Add(CycleSignature(phase, 20));
        }
        signatures.Add(CycleSignature(0, 20));

        LoopDetectionResult result = LoopDetectionService.DetectSignatures(signatures);

        Assert.True(result.IsAvailable);
        Assert.Equal(3, result.StartIndex);
        Assert.Equal(23, result.EndExclusiveIndex);
        Assert.Equal(16, result.SelectedIndices.Count);
        Assert.Equal(16, result.SelectedIndices.Distinct().Count());
        Assert.DoesNotContain(result.EndExclusiveIndex, result.SelectedIndices);
        Assert.True(result.IsStrongMatch);
    }

    [Fact]
    public void SelectEvenly_NeverIncludesRepeatedClosureFrame()
    {
        IReadOnlyList<int> selection = LoopDetectionService.SelectEvenly(5, 25, 16);

        Assert.Equal(16, selection.Count);
        Assert.Equal(5, selection[0]);
        Assert.True(selection[^1] < 25);
        Assert.Equal(selection.OrderBy(index => index), selection);
        Assert.Equal(16, selection.Distinct().Count());
    }

    [Fact]
    public void Detect_UsesImageContentAndFindsARepeatedVisualPhase()
    {
        var frames = new List<FrameBuffer>();
        for (int phase = 0; phase < 18; phase++) frames.Add(CreatePhaseFrame(phase, 18));
        frames.Add(CreatePhaseFrame(0, 18));

        LoopDetectionResult result = LoopDetectionService.Detect(frames);

        Assert.True(result.IsAvailable);
        Assert.Equal(0, result.StartIndex);
        Assert.Equal(18, result.EndExclusiveIndex);
        Assert.Equal(16, result.SelectedIndices.Count);
        Assert.True(result.Confidence > 0.5);
    }

    private static double[] CycleSignature(int phase, int length)
    {
        double angle = phase * Math.PI * 2.0 / length;
        return new[] { (Math.Sin(angle) + 1) / 2, (Math.Cos(angle) + 1) / 2, (Math.Sin(angle * 2) + 1) / 2 };
    }

    private static FrameBuffer CreatePhaseFrame(int phase, int length)
    {
        FrameBuffer frame = FrameBuffer.CreateSolid(80, 60, 0, 255, 0);
        double angle = phase * Math.PI * 2.0 / length;
        int x = 32 + (int)Math.Round(Math.Cos(angle) * 22);
        int y = 22 + (int)Math.Round(Math.Sin(angle) * 12);
        frame.DrawRectangle(x, y, 13, 18, 245, 240, 225);
        frame.DrawRectangle(x + 4, y + 4, 5, 8, 20, 18, 17);
        return frame;
    }
}

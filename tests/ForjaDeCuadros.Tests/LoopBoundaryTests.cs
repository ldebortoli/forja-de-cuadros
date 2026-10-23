using Xunit;

namespace ForjaDeCuadros.Tests;

public sealed class LoopBoundaryTests
{
    [Fact]
    public void RawFrameDetectionRejectsInvalidCountsAndUsesAllSixteenAtMinimum()
    {
        Assert.Throws<ArgumentNullException>(() => LoopDetectionService.Detect(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => LoopDetectionService.Detect(Array.Empty<FrameBuffer>(), 1));
        Assert.False(LoopDetectionService.Detect(Array.Empty<FrameBuffer>()).IsAvailable);
        var frames = Enumerable.Range(0, 16).Select(_ => FrameBuffer.CreateSolid(2, 2, 255, 0, 0)).ToArray();
        var result = LoopDetectionService.Detect(frames);
        Assert.True(result.IsAvailable);
        Assert.False(result.IsStrongMatch);
        Assert.Equal(0, result.Confidence);
        Assert.Equal(1, result.Score);
        Assert.Equal(Enumerable.Range(0, 16), result.SelectedIndices);
    }

    [Fact]
    public void SignatureDetectionRejectsNullEmptyAndMismatchedData()
    {
        Assert.Throws<ArgumentNullException>(() => LoopDetectionService.DetectSignatures(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => LoopDetectionService.DetectSignatures(Array.Empty<double[]>(), 1));
        Assert.False(LoopDetectionService.DetectSignatures(Array.Empty<double[]>()).IsAvailable);
        Assert.Throws<ArgumentException>(() => LoopDetectionService.DetectSignatures(new[] { null!, new[] { 1d }, new[] { 2d } }, 2));
        Assert.Throws<ArgumentException>(() => LoopDetectionService.DetectSignatures(new[] { Array.Empty<double>(), new[] { 1d }, new[] { 2d } }, 2));
        Assert.Throws<ArgumentException>(() => LoopDetectionService.DetectSignatures(new[] { new[] { 1d, 2d }, new[] { 1d }, new[] { 2d } }, 2));
    }

    [Fact]
    public void EquivalentStaticCandidatesKeepEarliestIntervalAndExcludeTerminal()
    {
        var signatures = Enumerable.Range(0, 20).Select(_ => new[] { 0d }).ToArray();
        var result = LoopDetectionService.DetectSignatures(signatures);
        Assert.Equal(0, result.StartIndex);
        Assert.Equal(16, result.EndExclusiveIndex);
        Assert.Equal(Enumerable.Range(0, 16), result.SelectedIndices);
        Assert.True(result.IsStrongMatch);
    }

    [Fact]
    public void MovingCycleBeatsUnchangingIntro()
    {
        var intro = Enumerable.Range(0, 18).Select(_ => new[] { 40d });
        var cycle = Enumerable.Range(0, 33).Select(index => new[] { Math.Sin(index * Math.PI * 2 / 16) });
        var result = LoopDetectionService.DetectSignatures(intro.Concat(cycle).ToArray());
        Assert.True(result.IsAvailable);
        Assert.InRange(result.StartIndex, 18, 34);
        Assert.Equal(16, result.SelectedIndices.Distinct().Count());
        Assert.InRange(result.Confidence, 0, 1);
    }

    [Fact]
    public void EvenSelectionRejectsInvalidRangesWithoutDuplicatingEndpoints()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => LoopDetectionService.SelectEvenly(-1, 16, 16));
        Assert.Throws<ArgumentOutOfRangeException>(() => LoopDetectionService.SelectEvenly(0, 0, 16));
        Assert.Throws<ArgumentOutOfRangeException>(() => LoopDetectionService.SelectEvenly(0, 16, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => LoopDetectionService.SelectEvenly(0, 15, 16));
        var selection = LoopDetectionService.SelectEvenly(2, 19, 16);
        Assert.Equal(16, selection.Distinct().Count());
        Assert.Equal(2, selection[0]);
        Assert.InRange(selection[^1], 17, 18);
    }
}

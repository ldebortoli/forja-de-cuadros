using System;
using System.IO;
using System.Text.Json;
using ForjaDeCuadros;
using Xunit;

namespace ForjaDeCuadros.Tests;

public sealed class KaggleTests
{
    [Fact]
    public void JobTemplate_WritesPrivateT4JobWithoutEmbeddingPromptOrLocalPathInCode()
    {
        string root = CreateWorkspace();
        try
        {
            string image = Path.Combine(root, "character.png");
            File.WriteAllBytes(image, new byte[] { 1, 2, 3, 4 });
            var request = new KaggleJobRequest
            {
                Username = "tester_name",
                ImagePath = image,
                Prompt = "A character performs a clean walk cycle on green.",
                OutputFolder = Path.Combine(root, "outputs"),
                Width = 512,
                Height = 512,
                NumberOfFrames = 97,
                FramesPerSecond = 30,
                Seed = 42,
                DeleteRemoteAfterDownload = true
            };

            KaggleJobDefinition definition = KaggleJobTemplate.Create(request, Path.Combine(root, "jobs"), new DateTimeOffset(2026, 8, 20, 12, 0, 0, TimeSpan.Zero), "ABC123");
            KaggleJobTemplate.WriteFiles(request, definition);

            Assert.Equal("tester_name/forja-input-20260820-120000-abc123", definition.DatasetHandle);
            Assert.Equal("tester_name/forja-ltx-20260820-120000-abc123", definition.KernelHandle);
            Assert.True(definition.DeleteRemoteAfterDownload);
            using JsonDocument kernel = JsonDocument.Parse(File.ReadAllText(Path.Combine(definition.KernelFolder, "kernel-metadata.json")));
            Assert.True(kernel.RootElement.GetProperty("is_private").GetBoolean());
            Assert.True(kernel.RootElement.GetProperty("enable_gpu").GetBoolean());
            Assert.True(kernel.RootElement.GetProperty("enable_internet").GetBoolean());
            Assert.Equal("NvidiaTeslaT4", kernel.RootElement.GetProperty("machine_shape").GetString());
            Assert.Equal(definition.DatasetHandle, kernel.RootElement.GetProperty("dataset_sources")[0].GetString());

            using JsonDocument dataset = JsonDocument.Parse(File.ReadAllText(Path.Combine(definition.DatasetFolder, "dataset-metadata.json")));
            Assert.Equal("other", dataset.RootElement.GetProperty("licenses")[0].GetProperty("name").GetString());
            Assert.Contains("not licensed for reuse", dataset.RootElement.GetProperty("description").GetString());
            string script = File.ReadAllText(Path.Combine(definition.KernelFolder, "forja_ltx.py"));
            Assert.Contains(KaggleJobTemplate.LtxCommit, script);
            Assert.Contains("forja-output.mp4", script);
            Assert.Contains("--offload_to_cpu", script);
            Assert.DoesNotContain(request.Prompt, script);
            Assert.DoesNotContain(root, script);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Theory]
    [InlineData("KernelWorkerStatus.QUEUED", KaggleRunState.Pending)]
    [InlineData("KernelWorkerStatus.RUNNING", KaggleRunState.Running)]
    [InlineData("KernelWorkerStatus.COMPLETE", KaggleRunState.Complete)]
    [InlineData("KernelWorkerStatus.ERROR", KaggleRunState.Failed)]
    [InlineData("cancelled by user", KaggleRunState.Failed)]
    public void StatusParser_MapsKaggleStates(string value, KaggleRunState expected)
    {
        Assert.Equal(expected, KaggleStatusParser.Parse(value));
    }

    [Theory]
    [InlineData("Python 3.13.2", 3, 13)]
    [InlineData("Python 3.11.9\r\n", 3, 11)]
    [InlineData("launcher output 3.10.4", 3, 10)]
    public void PythonVersionParser_ReadsSupportedFormat(string value, int major, int minor)
    {
        Version? version = KaggleCliService.ParsePythonVersion(value);
        Assert.NotNull(version);
        Assert.Equal(major, version!.Major);
        Assert.Equal(minor, version.Minor);
    }

    [Fact]
    public void CliParsers_ReadVersionAndOauthUsername()
    {
        Version? version = KaggleCliService.ParseCliVersion("Kaggle CLI 2.2.2\r\n");
        string? username = KaggleCliService.ParseConfiguredUsername("Configuration values\n- username: detected_user\n- auth_method: OAUTH");

        Assert.Equal(new Version(2, 2, 2), version);
        Assert.Equal("detected_user", username);
        Assert.Null(KaggleCliService.ParseConfiguredUsername("- auth_method: OAUTH"));
    }

    [Theory]
    [InlineData("Dataset creation error: Please select a valid license.")]
    [InlineData("403 Client Error: Forbidden")]
    [InlineData("Kernel push error: GPU quota exceeded")]
    public void CliFailureParser_DetectsErrorsReportedWithSuccessfulExitCode(string output)
    {
        Assert.True(KaggleCliService.ContainsReportedCliFailure(output));
        Assert.False(KaggleCliService.ContainsReportedCliFailure("Dataset created successfully"));
    }

    [Fact]
    public void JobTemplate_RejectsInvalidIdentityAndFrameCount()
    {
        string root = CreateWorkspace();
        try
        {
            string image = Path.Combine(root, "input.png");
            File.WriteAllBytes(image, new byte[] { 1 });
            var request = new KaggleJobRequest
            {
                Username = "bad/user",
                ImagePath = image,
                Prompt = "A valid prompt with enough characters.",
                OutputFolder = root,
                NumberOfFrames = 96
            };

            Assert.Throws<InvalidOperationException>(() => KaggleJobTemplate.Validate(request));
            request.Username = "valid-user";
            Assert.Throws<InvalidOperationException>(() => KaggleJobTemplate.Validate(request));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static string CreateWorkspace()
    {
        string path = Path.Combine(Path.GetTempPath(), "Forja-Kaggle-Tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}

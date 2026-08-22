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
            Assert.Contains("pipeline.enable_model_cpu_offload()", script);
            Assert.Contains("PYTORCH_ALLOC_CONF", script);
            Assert.Contains("Pinned LTX inference source changed", script);
            Assert.Contains("prompt_enhancement_words_threshold: 0", script);
            Assert.Contains("device=init_latents.device", script);
            Assert.Contains("latents.to(self.latent_upsampler.device)", script);
            Assert.Contains("self.vae = self.vae.to(latents.device)", script);
            Assert.Contains("vae.enable_z_tiling(32)", script);
            Assert.Contains("target_shape=target_shape_split, timestep=timestep", script);
            Assert.Contains("reduction_factor = int(self.temporal_downscale_factor)", script);
            Assert.Contains("start = end - 1", script);
            Assert.Contains("decoded_tile = decoded_tile[:, :, 1:]", script);
            Assert.Contains("LTX decoded only {images.shape[2]}", script);
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

    [Fact]
    public void QuotaParser_ReadsGpuHoursPercentAndRefreshDate()
    {
        const string csv = "resource,used,remaining,total,refreshAt\r\nGPU,0.22h,29.78h,30.00h,2026-08-29T00:00:00\r\nTPU,0.00h,20.00h,20.00h,2026-08-29T00:00:00\r\n";

        KaggleGpuQuota? quota = KaggleQuotaParser.ParseGpuCsv(csv);

        Assert.NotNull(quota);
        Assert.Equal(0.22, quota!.UsedHours, 2);
        Assert.Equal(29.78, quota.RemainingHours, 2);
        Assert.Equal(30, quota.TotalHours, 2);
        Assert.Equal(99.27, quota.RemainingPercent, 2);
        Assert.Equal(new DateTime(2026, 8, 29), quota.RefreshAt.Date);
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
    public void KernelLogDiagnostics_ExplainsCudaOutOfMemoryAsModelLoadingFailure()
    {
        const string logs = """
            [
              { "data": "Loading pipeline" },
              { "data": "torch.OutOfMemoryError: CUDA out of memory. GPU 0 has a total capacity of 14.56 GiB" }
            ]
            """;

        string summary = KaggleFailureDiagnostics.SummarizeKernelLogs(logs);

        Assert.Contains("GPU T4", summary);
        Assert.Contains("No es un problema de tu cuenta", summary);
        Assert.Contains("mejorador de prompt opcional", summary);
        Assert.Contains("descarga secuencial CPU/GPU", summary);
    }

    [Fact]
    public void KernelLogDiagnostics_PreservesUsefulTailForUnknownFailures()
    {
        string summary = KaggleFailureDiagnostics.SummarizeKernelLogs("line one\nRuntimeError: unexpected tensor shape");

        Assert.Contains("RuntimeError: unexpected tensor shape", summary);
    }

    [Fact]
    public void KernelLogDiagnostics_ExplainsCpuGpuConditioningMismatch()
    {
        string summary = KaggleFailureDiagnostics.SummarizeKernelLogs("RuntimeError: Expected all tensors to be on the same device, but found cuda:0 and cpu");

        Assert.Contains("tensores de condicionamiento", summary);
        Assert.Contains("mismo dispositivo", summary);
    }

    [Fact]
    public void KernelLogDiagnostics_ExplainsMultiscaleUpsamplerMismatch()
    {
        string summary = KaggleFailureDiagnostics.SummarizeKernelLogs("assert latents.device == latest_upsampler.device\nAssertionError");

        Assert.Contains("reescalador multiescala", summary);
        Assert.Contains("primera pasada", summary);
    }

    [Fact]
    public void KernelLogDiagnostics_ExplainsVaeDecodeMismatch()
    {
        string summary = KaggleFailureDiagnostics.SummarizeKernelLogs("in un_normalize_latents\nlatents * vae.std_of_means\nRuntimeError: Expected all tensors to be on the same device");

        Assert.Contains("decodificacion final", summary);
        Assert.Contains("reconstruir los cuadros", summary);
    }

    [Fact]
    public void KernelLogDiagnostics_ExplainsVaeDecodeOutOfMemory()
    {
        string summary = KaggleFailureDiagnostics.SummarizeKernelLogs("in _run_decoder\nimage = vae.decode\ntorch.OutOfMemoryError: CUDA out of memory");

        Assert.Contains("todos los cuadros juntos", summary);
        Assert.Contains("decodificacion temporal por bloques", summary);
    }

    [Fact]
    public void KernelLogDiagnostics_ExplainsBrokenTemporalTilingAttribute()
    {
        string summary = KaggleFailureDiagnostics.SummarizeKernelLogs("AttributeError: 'Encoder' object has no attribute 'patch_size_t'");

        Assert.Contains("atributo", summary);
        Assert.Contains("propio VAE", summary);
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

using System;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ForjaDeCuadros
{
    public sealed class KaggleJobRequest
    {
        public string Username { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        public string Prompt { get; set; } = string.Empty;
        public string OutputFolder { get; set; } = string.Empty;
        public int Width { get; set; } = 512;
        public int Height { get; set; } = 512;
        public int NumberOfFrames { get; set; } = 97;
        public int FramesPerSecond { get; set; } = 30;
        public int Seed { get; set; } = 171198;
        public bool DeleteRemoteAfterDownload { get; set; } = true;
    }

    public sealed class KaggleJobDefinition
    {
        public string JobId { get; set; } = string.Empty;
        public string WorkspaceFolder { get; set; } = string.Empty;
        public string DatasetFolder { get; set; } = string.Empty;
        public string KernelFolder { get; set; } = string.Empty;
        public string DownloadFolder { get; set; } = string.Empty;
        public string DatasetHandle { get; set; } = string.Empty;
        public string KernelHandle { get; set; } = string.Empty;
        public string InputFileName { get; set; } = string.Empty;
        public string OutputFolder { get; set; } = string.Empty;
        public bool DeleteRemoteAfterDownload { get; set; }
    }

    public sealed class KaggleJobResult
    {
        public string VideoPath { get; set; } = string.Empty;
        public string KernelUrl { get; set; } = string.Empty;
        public string JobId { get; set; } = string.Empty;
    }

    public enum KaggleRunState
    {
        Pending,
        Running,
        Complete,
        Failed
    }

    public static class KaggleStatusParser
    {
        public static KaggleRunState Parse(string value)
        {
            string normalized = (value ?? string.Empty).Trim().ToUpperInvariant();
            if (normalized.Contains("ERROR", StringComparison.Ordinal) || normalized.Contains("FAILED", StringComparison.Ordinal) || normalized.Contains("CANCELLED", StringComparison.Ordinal)) return KaggleRunState.Failed;
            if (normalized.Contains("COMPLETE", StringComparison.Ordinal)) return KaggleRunState.Complete;
            if (normalized.Contains("RUNNING", StringComparison.Ordinal)) return KaggleRunState.Running;
            return KaggleRunState.Pending;
        }
    }

    public static class KaggleJobTemplate
    {
        public const string LtxRepository = "https://github.com/Lightricks/LTX-Video.git";
        public const string LtxCommit = "4b2d053057623ddd4d0a1d3e9cd28890e9ef487f";
        public const string LtxPipelineConfig = "configs/ltxv-2b-0.9.8-distilled.yaml";
        private static readonly Regex UsernamePattern = new Regex("^[A-Za-z0-9_-]{2,50}$", RegexOptions.CultureInvariant);

        public static KaggleJobDefinition Create(KaggleJobRequest request, string jobsRoot, DateTimeOffset timestamp, string suffix)
        {
            Validate(request);
            if (string.IsNullOrWhiteSpace(jobsRoot)) throw new ArgumentException("Falta la carpeta local de trabajos.", nameof(jobsRoot));
            string safeSuffix = Regex.Replace(suffix ?? string.Empty, "[^a-zA-Z0-9]", string.Empty).ToLowerInvariant();
            if (safeSuffix.Length < 4) throw new ArgumentException("El identificador unico debe tener al menos cuatro caracteres.", nameof(suffix));
            if (safeSuffix.Length > 8) safeSuffix = safeSuffix.Substring(0, 8);
            string stamp = timestamp.UtcDateTime.ToString("yyyyMMdd-HHmmss");
            string jobId = stamp + "-" + safeSuffix;
            string datasetSlug = "forja-input-" + jobId.ToLowerInvariant();
            string kernelSlug = "forja-ltx-" + jobId.ToLowerInvariant();
            string workspace = Path.Combine(Path.GetFullPath(jobsRoot), jobId);
            string extension = Path.GetExtension(request.ImagePath).ToLowerInvariant();
            if (extension == ".jpeg") extension = ".jpg";
            string inputFile = "input" + extension;
            return new KaggleJobDefinition
            {
                JobId = jobId,
                WorkspaceFolder = workspace,
                DatasetFolder = Path.Combine(workspace, "dataset"),
                KernelFolder = Path.Combine(workspace, "kernel"),
                DownloadFolder = Path.Combine(workspace, "download"),
                DatasetHandle = request.Username.Trim().ToLowerInvariant() + "/" + datasetSlug,
                KernelHandle = request.Username.Trim().ToLowerInvariant() + "/" + kernelSlug,
                InputFileName = inputFile,
                OutputFolder = Path.GetFullPath(request.OutputFolder),
                DeleteRemoteAfterDownload = request.DeleteRemoteAfterDownload
            };
        }

        public static void WriteFiles(KaggleJobRequest request, KaggleJobDefinition definition)
        {
            Directory.CreateDirectory(definition.DatasetFolder);
            Directory.CreateDirectory(definition.KernelFolder);
            Directory.CreateDirectory(definition.DownloadFolder);
            Directory.CreateDirectory(definition.OutputFolder);
            File.Copy(request.ImagePath, Path.Combine(definition.DatasetFolder, definition.InputFileName), true);

            var remoteRequest = new
            {
                input_file = definition.InputFileName,
                prompt = request.Prompt.Trim(),
                width = request.Width,
                height = request.Height,
                num_frames = request.NumberOfFrames,
                frame_rate = request.FramesPerSecond,
                seed = request.Seed,
                model = "Lightricks/LTX-Video 2B 0.9.8 distilled",
                source_commit = LtxCommit
            };
            WriteJson(Path.Combine(definition.DatasetFolder, "request.json"), remoteRequest);

            var datasetMetadata = new
            {
                title = "Forja private input " + definition.JobId,
                id = definition.DatasetHandle,
                subtitle = "Private temporary input for Forja de Cuadros",
                description = "Private transient image and generation request. Copyright remains with the original author.",
                licenses = new[] { new { name = "copyright-authors" } }
            };
            WriteJson(Path.Combine(definition.DatasetFolder, "dataset-metadata.json"), datasetMetadata);

            var kernelMetadata = new
            {
                id = definition.KernelHandle,
                title = "Forja LTX " + definition.JobId,
                code_file = "forja_ltx.py",
                language = "python",
                kernel_type = "script",
                is_private = true,
                enable_gpu = true,
                enable_internet = true,
                machine_shape = "NvidiaTeslaT4",
                dataset_sources = new[] { definition.DatasetHandle },
                competition_sources = Array.Empty<string>(),
                kernel_sources = Array.Empty<string>(),
                model_sources = Array.Empty<string>()
            };
            WriteJson(Path.Combine(definition.KernelFolder, "kernel-metadata.json"), kernelMetadata);
            File.WriteAllText(Path.Combine(definition.KernelFolder, "forja_ltx.py"), BuildKernelScript());
            WriteJson(Path.Combine(definition.WorkspaceFolder, "job.json"), new
            {
                definition.JobId,
                definition.DatasetHandle,
                definition.KernelHandle,
                definition.OutputFolder,
                definition.DeleteRemoteAfterDownload,
                created_at = DateTimeOffset.Now
            });
        }

        public static string BuildKernelScript()
        {
            return """
from pathlib import Path
import json
import shutil
import subprocess
import sys

LTX_REPOSITORY = "https://github.com/Lightricks/LTX-Video.git"
LTX_COMMIT = "4b2d053057623ddd4d0a1d3e9cd28890e9ef487f"
PIPELINE_CONFIG = "configs/ltxv-2b-0.9.8-distilled.yaml"

input_root = Path("/kaggle/input")
working_root = Path("/kaggle/working")
request_path = next(input_root.rglob("request.json"))
request = json.loads(request_path.read_text(encoding="utf-8"))
image_path = next(path for path in input_root.rglob(request["input_file"]) if path.is_file())
repo_path = working_root / "LTX-Video"
generated_path = working_root / "generated"

subprocess.run(["git", "clone", "--filter=blob:none", LTX_REPOSITORY, str(repo_path)], check=True)
subprocess.run(["git", "-C", str(repo_path), "checkout", LTX_COMMIT], check=True)
subprocess.run([sys.executable, "-m", "pip", "install", "-q", f"{repo_path}[inference]"], check=True)

command = [
    sys.executable,
    str(repo_path / "inference.py"),
    "--prompt", request["prompt"],
    "--output_path", str(generated_path),
    "--pipeline_config", str(repo_path / PIPELINE_CONFIG),
    "--seed", str(request["seed"]),
    "--height", str(request["height"]),
    "--width", str(request["width"]),
    "--num_frames", str(request["num_frames"]),
    "--frame_rate", str(request["frame_rate"]),
    "--offload_to_cpu",
    "--conditioning_media_paths", str(image_path),
    "--conditioning_strengths", "1.0",
    "--conditioning_start_frames", "0",
]
subprocess.run(command, check=True)
videos = sorted(generated_path.glob("*.mp4"), key=lambda path: path.stat().st_mtime)
if not videos:
    raise RuntimeError("LTX-Video did not produce an MP4 file")
result_path = working_root / "forja-output.mp4"
shutil.copy2(videos[-1], result_path)
(working_root / "forja-result.json").write_text(json.dumps({
    "success": True,
    "video": result_path.name,
    "model": request["model"],
    "source_commit": LTX_COMMIT,
    "width": request["width"],
    "height": request["height"],
    "num_frames": request["num_frames"],
    "frame_rate": request["frame_rate"],
    "seed": request["seed"],
}, indent=2), encoding="utf-8")
print("FORJA_KAGGLE_RESULT=forja-output.mp4")
""" + Environment.NewLine;
        }

        public static void Validate(KaggleJobRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (!UsernamePattern.IsMatch(request.Username.Trim())) throw new InvalidOperationException("El usuario de Kaggle debe tener entre 2 y 50 letras, numeros, guiones o guiones bajos.");
            if (!File.Exists(request.ImagePath)) throw new FileNotFoundException("No existe la imagen de entrada.", request.ImagePath);
            string extension = Path.GetExtension(request.ImagePath).ToLowerInvariant();
            if (extension != ".png" && extension != ".jpg" && extension != ".jpeg" && extension != ".webp") throw new InvalidOperationException("La imagen debe ser PNG, JPG o WebP.");
            if (request.Prompt.Trim().Length < 12) throw new InvalidOperationException("Escribi un prompt de al menos 12 caracteres.");
            if (request.Prompt.Length > 4000) throw new InvalidOperationException("El prompt no puede superar 4000 caracteres.");
            if (request.Width < 256 || request.Width > 1024 || request.Width % 32 != 0 || request.Height < 256 || request.Height > 1024 || request.Height % 32 != 0) throw new InvalidOperationException("Ancho y alto deben estar entre 256 y 1024 y ser multiplos de 32.");
            if (request.NumberOfFrames < 17 || request.NumberOfFrames > 241 || (request.NumberOfFrames - 1) % 8 != 0) throw new InvalidOperationException("La cantidad de cuadros debe ser 8N + 1, entre 17 y 241.");
            if (request.FramesPerSecond < 8 || request.FramesPerSecond > 60) throw new InvalidOperationException("Los FPS deben estar entre 8 y 60.");
            if (string.IsNullOrWhiteSpace(request.OutputFolder)) throw new InvalidOperationException("Elegi una carpeta local para el MP4.");
        }

        private static void WriteJson(string path, object value)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            File.WriteAllText(path, JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}

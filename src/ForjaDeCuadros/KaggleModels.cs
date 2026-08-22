using System;
using System.Globalization;
using System.IO;
using System.Linq;
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

    public sealed class KaggleGpuQuota
    {
        public double UsedHours { get; set; }
        public double RemainingHours { get; set; }
        public double TotalHours { get; set; }
        public DateTime RefreshAt { get; set; }
        public double RemainingPercent => TotalHours <= 0 ? 0 : Math.Clamp(RemainingHours / TotalHours * 100, 0, 100);
    }

    public static class KaggleQuotaParser
    {
        public static KaggleGpuQuota? ParseGpuCsv(string value)
        {
            foreach (string line in (value ?? string.Empty).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] columns = line.Split(',').Select(column => column.Trim().Trim('"')).ToArray();
                if (columns.Length < 5 || !columns[0].Equals("GPU", StringComparison.OrdinalIgnoreCase)) continue;
                if (!TryParseHours(columns[1], out double used)
                    || !TryParseHours(columns[2], out double remaining)
                    || !TryParseHours(columns[3], out double total)
                    || !DateTime.TryParse(columns[4], CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out DateTime refreshAt)) return null;
                return new KaggleGpuQuota
                {
                    UsedHours = used,
                    RemainingHours = remaining,
                    TotalHours = total,
                    RefreshAt = refreshAt
                };
            }
            return null;
        }

        private static bool TryParseHours(string value, out double hours)
        {
            string normalized = value.Trim();
            if (normalized.EndsWith("h", StringComparison.OrdinalIgnoreCase)) normalized = normalized.Substring(0, normalized.Length - 1);
            return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out hours);
        }
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

    public static class KaggleFailureDiagnostics
    {
        public static string SummarizeKernelLogs(string value)
        {
            string log = ExtractLogText(value);
            bool outOfMemory = log.Contains("CUDA out of memory", StringComparison.OrdinalIgnoreCase)
                || log.Contains("torch.OutOfMemoryError", StringComparison.OrdinalIgnoreCase);
            if (outOfMemory && (log.Contains("_run_decoder", StringComparison.OrdinalIgnoreCase)
                || log.Contains("vae.decode", StringComparison.OrdinalIgnoreCase)))
            {
                return "El VAE de LTX agoto la memoria al reconstruir todos los cuadros juntos. Los trabajos nuevos activan decodificacion temporal por bloques para una GPU T4.";
            }
            if (outOfMemory)
            {
                return "La GPU T4 se quedo sin memoria al cargar los modelos. No es un problema de tu cuenta ni del prompt. Los trabajos nuevos desactivan el mejorador de prompt opcional y usan descarga secuencial CPU/GPU para evitarlo.";
            }
            if (log.Contains("GPU quota", StringComparison.OrdinalIgnoreCase)
                || log.Contains("quota exceeded", StringComparison.OrdinalIgnoreCase))
            {
                return "Kaggle rechazo la GPU porque la cuenta no tiene cuota disponible en este momento. Revisa VER CUOTA GPU y volve a intentar mas tarde.";
            }
            if (log.Contains("No space left on device", StringComparison.OrdinalIgnoreCase))
            {
                return "El entorno temporal de Kaggle se quedo sin espacio en disco durante la generacion.";
            }
            if (log.Contains("un_normalize_latents", StringComparison.OrdinalIgnoreCase)
                || log.Contains("vae.std_of_means", StringComparison.OrdinalIgnoreCase))
            {
                return "El VAE de LTX seguia descargado en CPU al comenzar la decodificacion final. Los trabajos nuevos lo mueven junto a los latentes antes de reconstruir los cuadros.";
            }
            if (log.Contains("Expected all tensors to be on the same device", StringComparison.OrdinalIgnoreCase))
            {
                return "LTX mezclo tensores de condicionamiento entre CPU y GPU durante el offload. Los trabajos nuevos conservan esos tensores en el mismo dispositivo que los latentes de video.";
            }
            if (log.Contains("latents.device == latest_upsampler.device", StringComparison.OrdinalIgnoreCase))
            {
                return "El reescalador multiescala de LTX recibio latentes en GPU mientras estaba descargado en CPU. Los trabajos nuevos mueven esa primera pasada al dispositivo del reescalador.";
            }
            if (log.Contains("has no attribute 'patch_size_t'", StringComparison.OrdinalIgnoreCase))
            {
                return "El tiling temporal de LTX intento leer un atributo que este VAE no define. Los trabajos nuevos usan el factor temporal que el propio VAE publica.";
            }

            string[] usefulLines = log.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => line.Length > 0 && !line.StartsWith("Traceback (most recent call last)", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            return usefulLines.Length == 0
                ? "Kaggle informo un error sin detalle descargable. Abri el trabajo remoto para ver el log completo."
                : string.Join(Environment.NewLine, usefulLines.Skip(Math.Max(0, usefulLines.Length - 6)));
        }

        private static string ExtractLogText(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            try
            {
                using JsonDocument document = JsonDocument.Parse(value);
                if (document.RootElement.ValueKind != JsonValueKind.Array) return value;
                return string.Join(Environment.NewLine, document.RootElement.EnumerateArray()
                    .Select(entry => entry.TryGetProperty("data", out JsonElement data) ? data.GetString() : null)
                    .Where(data => !string.IsNullOrWhiteSpace(data)));
            }
            catch (JsonException)
            {
                return value;
            }
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
                description = "Private transient image and generation request. Copyright remains with the original author; this temporary upload is not licensed for reuse.",
                licenses = new[] { new { name = "other" } }
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
import os
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

# The pinned upstream script moves the transformer, VAE and T5 text encoder to
# the GPU at the same time. That exceeds a T4's 16 GB before inference starts.
# Diffusers already knows this pipeline's module order, so install its standard
# model CPU-offload hooks and keep only the active module in VRAM.
inference_module = repo_path / "ltx_video" / "inference.py"
inference_source = inference_module.read_text(encoding="utf-8")
eager_moves = '''    transformer = transformer.to(device)
    vae = vae.to(device)
    text_encoder = text_encoder.to(device)'''
sequential_moves = '''    if device != "cuda":
        transformer = transformer.to(device)
        vae = vae.to(device)
        text_encoder = text_encoder.to(device)'''
eager_pipeline = '''    pipeline = pipeline.to(device)
    return pipeline'''
offloaded_pipeline = '''    if device == "cuda":
        pipeline.enable_model_cpu_offload()
    else:
        pipeline = pipeline.to(device)
    return pipeline'''
if eager_moves not in inference_source or eager_pipeline not in inference_source:
    raise RuntimeError("Pinned LTX inference source changed; refusing an unsafe VRAM patch")
inference_source = inference_source.replace(eager_moves, sequential_moves, 1)
inference_source = inference_source.replace(eager_pipeline, offloaded_pipeline, 1)
vae_precision = "    vae = vae.to(torch.bfloat16)"
vae_tiled_decode = '''    vae = vae.to(torch.bfloat16)
    vae.enable_z_tiling(32)'''
if inference_source.count(vae_precision) != 1:
    raise RuntimeError("Pinned LTX VAE precision source changed")
inference_source = inference_source.replace(vae_precision, vae_tiled_decode, 1)
crop_marker = "    # Crop the padded images to the desired resolution and number of frames"
validated_crop = '''    if images.shape[2] < config.num_frames:
        raise RuntimeError(f"LTX decoded only {images.shape[2]} of {config.num_frames} requested frames")

    # Crop the padded images to the desired resolution and number of frames'''
if inference_source.count(crop_marker) != 1:
    raise RuntimeError("Pinned LTX output crop source changed")
inference_source = inference_source.replace(crop_marker, validated_crop, 1)
inference_module.write_text(inference_source, encoding="utf-8")

vae_module = repo_path / "ltx_video" / "models" / "autoencoders" / "vae.py"
vae_source = vae_module.read_text(encoding="utf-8")
tiled_decode_without_timestep = "                    else self._decode(z_tile, target_shape=target_shape_split)"
tiled_decode_with_timestep = "                    else self._decode(z_tile, target_shape=target_shape_split, timestep=timestep)"
if vae_source.count(tiled_decode_without_timestep) != 1:
    raise RuntimeError("Pinned LTX tiled VAE source changed")
vae_module.write_text(
    vae_source.replace(tiled_decode_without_timestep, tiled_decode_with_timestep, 1),
    encoding="utf-8",
)

vae_source = vae_module.read_text(encoding="utf-8")
broken_temporal_factor = '''            reduction_factor = int(
                self.encoder.patch_size_t
                * 2
                ** (
                    len(self.encoder.down_blocks)
                    - 1
                    - math.sqrt(self.encoder.patch_size)
                )
            )'''
stable_temporal_factor = "            reduction_factor = int(self.temporal_downscale_factor)"
if vae_source.count(broken_temporal_factor) != 1:
    raise RuntimeError("Pinned LTX temporal tiling source changed")
vae_module.write_text(
    vae_source.replace(broken_temporal_factor, stable_temporal_factor, 1),
    encoding="utf-8",
)

vae_source = vae_module.read_text(encoding="utf-8")
legacy_tiled_decode = '''        if self.use_z_tiling and z.shape[2] > self.z_sample_size > 1:
            reduction_factor = int(self.temporal_downscale_factor)
            split_size = self.z_sample_size // reduction_factor
            num_splits = z.shape[2] // split_size

            # copy target shape, and divide frame dimension (=2) by the context size
            target_shape_split = list(target_shape)
            target_shape_split[2] = target_shape[2] // num_splits

            decoded_tiles = [
                (
                    self._hw_tiled_decode(z_tile, target_shape_split)
                    if self.use_hw_tiling
                    else self._decode(z_tile, target_shape=target_shape_split, timestep=timestep)
                )
                for z_tile in torch.tensor_split(z, num_splits, dim=2)
            ]
            decoded = torch.cat(decoded_tiles, dim=2)
        else:'''
overlapped_tiled_decode = '''        if self.use_z_tiling and z.shape[2] > 1:
            temporal_scale = int(self.temporal_downscale_factor)
            latent_chunk_size = max(2, self.z_sample_size // max(1, temporal_scale) + 1)
            decoded_tiles = []
            start = 0
            while start < z.shape[2]:
                end = min(start + latent_chunk_size, z.shape[2])
                tile_target_shape = list(target_shape)
                tile_target_shape[2] = (end - start - 1) * temporal_scale + 1
                z_tile = z[:, :, start:end]
                decoded_tile = (
                    self._hw_tiled_decode(z_tile, tile_target_shape)
                    if self.use_hw_tiling
                    else self._decode(z_tile, target_shape=tile_target_shape, timestep=timestep)
                )
                if start > 0:
                    decoded_tile = decoded_tile[:, :, 1:]
                decoded_tiles.append(decoded_tile)
                if end == z.shape[2]:
                    break
                start = end - 1
            decoded = torch.cat(decoded_tiles, dim=2)
        else:'''
if vae_source.count(legacy_tiled_decode) != 1:
    raise RuntimeError("Pinned LTX temporal decode branch changed")
vae_module.write_text(
    vae_source.replace(legacy_tiled_decode, overlapped_tiled_decode, 1),
    encoding="utf-8",
)

# The optional Florence + Llama prompt enhancer occupies most of a T4 and is
# unnecessary because Forja already asks the user for a detailed motion prompt.
pipeline_config_path = repo_path / PIPELINE_CONFIG
pipeline_config_source = pipeline_config_path.read_text(encoding="utf-8")
prompt_enhancement = "prompt_enhancement_words_threshold: 120"
if prompt_enhancement not in pipeline_config_source:
    raise RuntimeError("Pinned LTX prompt-enhancement config changed")
pipeline_config_path.write_text(
    pipeline_config_source.replace(prompt_enhancement, "prompt_enhancement_words_threshold: 0", 1),
    encoding="utf-8",
)

# Upstream preserves the VAE output device when it only changes dtype. Under
# CPU offload that can leave the conditioning latent on CPU while noise lives
# on CUDA. Move it explicitly to the initial latent device before torch.lerp.
pipeline_module = repo_path / "ltx_video" / "pipelines" / "pipeline_ltx_video.py"
pipeline_source = pipeline_module.read_text(encoding="utf-8")
device_less_conditioning = ").to(dtype=init_latents.dtype)"
device_aware_conditioning = ").to(dtype=init_latents.dtype, device=init_latents.device)"
if pipeline_source.count(device_less_conditioning) != 1:
    raise RuntimeError("Pinned LTX conditioning source changed")
pipeline_module.write_text(
    pipeline_source.replace(device_less_conditioning, device_aware_conditioning, 1),
    encoding="utf-8",
)

pipeline_source = pipeline_module.read_text(encoding="utf-8")
upsampler_call = "        upsampled_latents = self._upsample_latents(self.latent_upsampler, latents)"
device_aware_upsampler = '''        latents = latents.to(self.latent_upsampler.device)
        upsampled_latents = self._upsample_latents(self.latent_upsampler, latents)'''
if pipeline_source.count(upsampler_call) != 1:
    raise RuntimeError("Pinned LTX multiscale source changed")
pipeline_module.write_text(
    pipeline_source.replace(upsampler_call, device_aware_upsampler, 1),
    encoding="utf-8",
)

pipeline_source = pipeline_module.read_text(encoding="utf-8")
vae_decode_call = "            image = vae_decode("
device_aware_vae_decode = '''            self.vae = self.vae.to(latents.device)
            image = vae_decode('''
if pipeline_source.count(vae_decode_call) != 1:
    raise RuntimeError("Pinned LTX VAE decode source changed")
pipeline_module.write_text(
    pipeline_source.replace(vae_decode_call, device_aware_vae_decode, 1),
    encoding="utf-8",
)

command = [
    sys.executable,
    str(repo_path / "inference.py"),
    "--prompt", request["prompt"],
    "--output_path", str(generated_path),
    "--pipeline_config", str(pipeline_config_path),
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
inference_environment = os.environ.copy()
inference_environment["PYTORCH_ALLOC_CONF"] = "expandable_segments:True"
subprocess.run(command, check=True, env=inference_environment)
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ForjaDeCuadros
{
    public sealed class FfmpegService : IDisposable
    {
        private readonly object _processLock = new object();
        private Process? _activeProcess;

        public FfmpegService()
        {
            FfmpegPath = ResolveExecutable("ffmpeg.exe") ?? throw new FileNotFoundException("No se encontro FFmpeg. Instalalo con winget install Gyan.FFmpeg.");
            FfprobePath = ResolveExecutable("ffprobe.exe") ?? Path.Combine(Path.GetDirectoryName(FfmpegPath) ?? string.Empty, "ffprobe.exe");
            if (!File.Exists(FfprobePath)) throw new FileNotFoundException("No se encontro ffprobe junto a FFmpeg.");
        }

        public string FfmpegPath { get; }
        public string FfprobePath { get; }

        public async Task<VideoInfo> ProbeAsync(string inputPath, CancellationToken cancellationToken = default)
        {
            var result = await RunAsync(FfprobePath, new[] { "-v", "error", "-select_streams", "v:0", "-show_entries", "stream=width,height,avg_frame_rate,r_frame_rate,duration:format=duration", "-of", "json", inputPath }, cancellationToken).ConfigureAwait(false);
            using JsonDocument document = JsonDocument.Parse(result.StandardOutput);
            JsonElement stream = document.RootElement.GetProperty("streams")[0];
            double duration = ReadDouble(stream, "duration");
            if (duration <= 0 && document.RootElement.TryGetProperty("format", out JsonElement format)) duration = ReadDouble(format, "duration");
            string fpsText = stream.TryGetProperty("avg_frame_rate", out JsonElement average) ? average.GetString() ?? "0/1" : "0/1";
            double fps = ParseFraction(fpsText);
            if (fps <= 0 && stream.TryGetProperty("r_frame_rate", out JsonElement rawRate)) fps = ParseFraction(rawRate.GetString() ?? "0/1");
            return new VideoInfo
            {
                Width = stream.GetProperty("width").GetInt32(),
                Height = stream.GetProperty("height").GetInt32(),
                Duration = duration,
                FramesPerSecond = fps
            };
        }

        public async Task<IReadOnlyList<string>> ExtractFramesAsync(string inputPath, string outputFolder, double startSeconds, double endSeconds, int everyNFrames, int maxFrames, CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(outputFolder);
            string pattern = Path.Combine(outputFolder, "candidate_%04d.png");
            var arguments = new List<string> { "-hide_banner", "-loglevel", "error", "-y" };
            if (startSeconds > 0) { arguments.Add("-ss"); arguments.Add(startSeconds.ToString("0.###", CultureInfo.InvariantCulture)); }
            arguments.Add("-i"); arguments.Add(inputPath);
            if (endSeconds > startSeconds)
            {
                arguments.Add("-t");
                arguments.Add((endSeconds - startSeconds).ToString("0.###", CultureInfo.InvariantCulture));
            }
            arguments.Add("-vf"); arguments.Add("select=not(mod(n\\," + Math.Max(1, everyNFrames) + "))");
            arguments.Add("-fps_mode"); arguments.Add("vfr");
            arguments.Add("-frames:v"); arguments.Add(Math.Clamp(maxFrames, 16, 400).ToString(CultureInfo.InvariantCulture));
            arguments.Add(pattern);
            await RunAsync(FfmpegPath, arguments, cancellationToken).ConfigureAwait(false);
            return Directory.GetFiles(outputFolder, "candidate_*.png").OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray();
        }

        public async Task EncodePngSequenceAsync(string framePattern, double fps, string outputPath, CancellationToken cancellationToken = default)
        {
            await RunAsync(FfmpegPath, new[]
            {
                "-hide_banner", "-loglevel", "error", "-y", "-framerate", fps.ToString("0.###", CultureInfo.InvariantCulture), "-start_number", "1",
                "-i", framePattern, "-c:v", "libx264", "-pix_fmt", "yuv420p", outputPath
            }, cancellationToken).ConfigureAwait(false);
        }

        public async Task CreateGifAsync(string framePattern, double fps, string outputPath, CancellationToken cancellationToken = default)
        {
            string filter = "[0:v]split[a][b];[a]palettegen=reserve_transparent=1:transparency_color=ffffff[p];[b][p]paletteuse=dither=sierra2_4a:alpha_threshold=64";
            await RunAsync(FfmpegPath, new[]
            {
                "-hide_banner", "-loglevel", "error", "-y", "-framerate", fps.ToString("0.###", CultureInfo.InvariantCulture), "-start_number", "1",
                "-i", framePattern, "-filter_complex", filter, "-loop", "0", outputPath
            }, cancellationToken).ConfigureAwait(false);
        }

        public void CancelActive()
        {
            lock (_processLock)
            {
                try
                {
                    if (_activeProcess != null && !_activeProcess.HasExited) _activeProcess.Kill(true);
                }
                catch { }
            }
        }

        public void Dispose() => CancelActive();

        private async Task<ProcessResult> RunAsync(string executable, IEnumerable<string> arguments, CancellationToken cancellationToken)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            foreach (string argument in arguments) startInfo.ArgumentList.Add(argument);
            using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            lock (_processLock) _activeProcess = process;
            try
            {
                if (!process.Start()) throw new InvalidOperationException("No se pudo iniciar " + Path.GetFileName(executable) + ".");
                Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
                Task<string> stderrTask = process.StandardError.ReadToEndAsync();
                using CancellationTokenRegistration registration = cancellationToken.Register(() =>
                {
                    try { if (!process.HasExited) process.Kill(true); } catch { }
                });
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                string stdout = await stdoutTask.ConfigureAwait(false);
                string stderr = await stderrTask.ConfigureAwait(false);
                if (process.ExitCode != 0) throw new InvalidOperationException(Path.GetFileName(executable) + " termino con codigo " + process.ExitCode + ":\n" + stderr.Trim());
                return new ProcessResult(stdout, stderr);
            }
            finally
            {
                lock (_processLock) if (ReferenceEquals(_activeProcess, process)) _activeProcess = null;
            }
        }

        private static string? ResolveExecutable(string name)
        {
            string? explicitPath = Environment.GetEnvironmentVariable(name.StartsWith("ffprobe", StringComparison.OrdinalIgnoreCase) ? "FFPROBE_PATH" : "FFMPEG_PATH");
            if (!string.IsNullOrWhiteSpace(explicitPath) && File.Exists(explicitPath)) return Path.GetFullPath(explicitPath);
            string? pathVariable = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrWhiteSpace(pathVariable))
            {
                foreach (string folder in pathVariable.Split(Path.PathSeparator))
                {
                    try
                    {
                        string candidate = Path.Combine(folder.Trim(), name);
                        if (File.Exists(candidate)) return Path.GetFullPath(candidate);
                    }
                    catch { }
                }
            }
            string packages = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WinGet", "Packages");
            if (Directory.Exists(packages))
            {
                try
                {
                    string? candidate = Directory.EnumerateFiles(packages, name, SearchOption.AllDirectories).FirstOrDefault(path => path.IndexOf("Gyan.FFmpeg", StringComparison.OrdinalIgnoreCase) >= 0);
                    if (candidate != null) return candidate;
                }
                catch { }
            }
            return null;
        }

        private static double ParseFraction(string value)
        {
            string[] parts = value.Split('/');
            if (parts.Length == 2 && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double numerator) && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double denominator) && denominator != 0) return numerator / denominator;
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result) ? result : 0;
        }

        private static double ReadDouble(JsonElement element, string property)
        {
            if (!element.TryGetProperty(property, out JsonElement value)) return 0;
            if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out double number)) return number;
            return double.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out number) ? number : 0;
        }

        private readonly struct ProcessResult
        {
            public ProcessResult(string standardOutput, string standardError) { StandardOutput = standardOutput; StandardError = standardError; }
            public string StandardOutput { get; }
            public string StandardError { get; }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ForjaDeCuadros
{
    public static class SelfTestRunner
    {
        public static async Task<int> RunAsync(string reportPath)
        {
            string workspace = Path.Combine(Path.GetTempPath(), "ForjaDeCuadros-SelfTest-" + Guid.NewGuid().ToString("N"));
            var checks = new List<object>();
            bool success = false;
            string? error = null;
            try
            {
                Directory.CreateDirectory(workspace);
                string sourceFrames = Path.Combine(workspace, "source");
                Directory.CreateDirectory(sourceFrames);
                for (int index = 0; index < 48; index++)
                {
                    var frame = FrameBuffer.CreateSolid(320, 240, 0, 255, 0);
                    int x = 42 + index * 3;
                    int bob = (int)Math.Round(Math.Sin(index / 48.0 * Math.PI * 4) * 4);
                    frame.DrawRectangle(x, 42 + bob, 68, 168, 24, 23, 22);
                    frame.DrawRectangle(x + 17, 62 + bob, 34, 48, 199, 59, 47);
                    frame.DrawRectangle(x + 8, 204, 22, 8, 24, 23, 22);
                    frame.DrawRectangle(x + 39, 204, 22, 8, 24, 23, 22);
                    frame.SavePng(Path.Combine(sourceFrames, "source_" + (index + 1).ToString("D3") + ".png"));
                }

                using var ffmpeg = new FfmpegService();
                string videoPath = Path.Combine(workspace, "input.mp4");
                await ffmpeg.EncodePngSequenceAsync(Path.Combine(sourceFrames, "source_%03d.png"), 24, videoPath);
                VideoInfo info = await ffmpeg.ProbeAsync(videoPath);
                checks.Add(new { name = "ffmpeg_probe", passed = info.Width == 320 && info.Height == 240 && info.FramesPerSecond > 20 });

                string candidatesFolder = Path.Combine(workspace, "candidates");
                var candidatePaths = await ffmpeg.ExtractFramesAsync(videoPath, candidatesFolder, 0, info.Duration, 3, 16);
                checks.Add(new { name = "extract_16", passed = candidatePaths.Count == 16, count = candidatePaths.Count });
                var candidates = candidatePaths.Select((path, index) => new FrameItem { Number = index + 1, Timestamp = index * 3.0 / 24.0, ImagePath = path, IsSelected = true }).ToList();
                var processing = new ProcessingOptions { CanvasWidth = 256, CanvasHeight = 256, GroundY = 234, RootX = 128, Padding = 8, ChromaEnabled = true, ChromaTolerance = 28, EdgeSoftness = 10, HaloPixels = 1, IslandCleanupPixels = 24, SpillSuppression = 0.65, RegistrationMode = RegistrationMode.RootAndGround };
                string processedFolder = Path.Combine(workspace, "processed");
                List<ProcessedFrame> processed = FrameProcessor.Process(candidates, processing, processedFolder);
                AuditReport audit = AuditService.Audit(processed, processing);
                checks.Add(new { name = "audit_unique", passed = audit.UniqueFrames == 16, unique = audit.UniqueFrames });
                checks.Add(new { name = "audit_no_blanks", passed = processed.All(frame => !frame.Bounds.IsEmpty) });

                string exports = Path.Combine(workspace, "exports");
                ExportResult export = await ExportService.ExportAsync(processed, processing, audit, new ExportOptions { BaseFolder = exports, AnimationName = "autoprueba", Columns = 4, FramesPerSecond = 12, GodotTexturePath = "res://assets/sprites/generated/autoprueba_atlas.png" }, ffmpeg);
                string exportFolder = export.Folder;
                bool artifacts = File.Exists(export.AtlasPath) && File.Exists(export.ReviewPath) && File.Exists(Path.Combine(exportFolder, "autoprueba_preview.gif")) && File.Exists(Path.Combine(exportFolder, "autoprueba_spriteframes.tres")) && Directory.GetFiles(Path.Combine(exportFolder, "frames"), "*.png").Length == 16;
                checks.Add(new { name = "export_artifacts", passed = artifacts });
                success = checks.All(check => JsonSerializer.Serialize(check).Contains("\"passed\":true", StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception exception)
            {
                error = exception.ToString();
            }

            Directory.CreateDirectory(Path.GetDirectoryName(reportPath) ?? ".");
            bool keepWorkspace = string.Equals(Environment.GetEnvironmentVariable("FORJA_KEEP_SELFTEST"), "1", StringComparison.Ordinal);
            File.WriteAllText(reportPath, JsonSerializer.Serialize(new { success, generated_at = DateTimeOffset.Now, checks, error, workspace = keepWorkspace ? workspace : null }, new JsonSerializerOptions { WriteIndented = true }));
            try { if (success && !keepWorkspace && Directory.Exists(workspace)) Directory.Delete(workspace, true); } catch { }
            return success ? 0 : 1;
        }
    }
}

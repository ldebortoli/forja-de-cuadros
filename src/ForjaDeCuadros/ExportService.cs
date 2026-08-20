using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ForjaDeCuadros
{
    public static class ExportService
    {
        public static async Task<ExportResult> ExportAsync(IReadOnlyList<ProcessedFrame> frames, ProcessingOptions processing, AuditReport audit, ExportOptions options, FfmpegService ffmpeg, CancellationToken cancellationToken = default)
        {
            if (frames.Count != 16) throw new InvalidOperationException("La exportacion requiere exactamente 16 cuadros.");
            string safeName = SanitizeName(options.AnimationName);
            if (string.IsNullOrWhiteSpace(safeName)) safeName = "nueva_animacion";
            Directory.CreateDirectory(options.BaseFolder);
            string outputFolder = Path.Combine(options.BaseFolder, safeName);
            if (Directory.Exists(outputFolder)) outputFolder += "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            string frameFolder = Path.Combine(outputFolder, "frames");
            Directory.CreateDirectory(frameFolder);

            for (int index = 0; index < frames.Count; index++)
            {
                string destination = Path.Combine(frameFolder, "frame_" + (index + 1).ToString("00") + ".png");
                File.Copy(frames[index].FilePath, destination, true);
            }

            int columns = options.Columns == 16 ? 16 : 4;
            int rows = (int)Math.Ceiling(frames.Count / (double)columns);
            var atlas = new FrameBuffer(processing.CanvasWidth * columns, processing.CanvasHeight * rows, new byte[processing.CanvasWidth * columns * processing.CanvasHeight * rows * 4]);
            for (int index = 0; index < frames.Count; index++) atlas.Blit(frames[index].Buffer, (index % columns) * processing.CanvasWidth, (index / columns) * processing.CanvasHeight);
            string atlasPath = Path.Combine(outputFolder, safeName + "_atlas.png");
            atlas.SavePng(atlasPath);

            string gifPath = Path.Combine(outputFolder, safeName + "_preview.gif");
            await ffmpeg.CreateGifAsync(Path.Combine(frameFolder, "frame_%02d.png"), options.FramesPerSecond, gifPath, cancellationToken).ConfigureAwait(false);

            string texturePath = string.IsNullOrWhiteSpace(options.GodotTexturePath) ? "res://assets/sprites/generated/" + safeName + "_atlas.png" : options.GodotTexturePath.Trim();
            string tresPath = Path.Combine(outputFolder, safeName + "_spriteframes.tres");
            File.WriteAllText(tresPath, BuildTres(safeName, texturePath, frames.Count, columns, processing.CanvasWidth, processing.CanvasHeight, options.FramesPerSecond), new UTF8Encoding(false));

            var metadata = new
            {
                schema = "forja-de-cuadros/v1",
                animation = safeName,
                generated_at = DateTimeOffset.Now,
                fps = options.FramesPerSecond,
                loop = true,
                canvas = new { width = processing.CanvasWidth, height = processing.CanvasHeight, ground_y = processing.GroundY, root_x = processing.RootX },
                registration = processing.RegistrationMode.ToString(),
                atlas = new { file = Path.GetFileName(atlasPath), columns, rows, texture_path = texturePath },
                audit = new { passed = !audit.HasErrors, unique_frames = audit.UniqueFrames, height_drift_percent = audit.HeightDriftPercent, loop_seam_ratio = audit.LoopSeamRatio, findings = audit.Findings.Select(f => new { level = f.Level.ToString(), message = f.Message }) },
                frames = frames.Select((frame, index) => new { number = index + 1, file = "frames/frame_" + (index + 1).ToString("00") + ".png", source_time = frame.Timestamp, sha256_rgba = frame.Sha256, region = new { x = (index % columns) * processing.CanvasWidth, y = (index / columns) * processing.CanvasHeight, width = processing.CanvasWidth, height = processing.CanvasHeight } })
            };
            string metadataPath = Path.Combine(outputFolder, safeName + "_metadata.json");
            File.WriteAllText(metadataPath, JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));

            string reviewPath = Path.Combine(outputFolder, "index.html");
            File.WriteAllText(reviewPath, BuildReviewHtml(safeName, Path.GetFileName(gifPath), audit), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(outputFolder, "LEEME.txt"), BuildReadme(safeName, texturePath, audit), new UTF8Encoding(false));

            return new ExportResult { Folder = outputFolder, AtlasPath = atlasPath, ReviewPath = reviewPath };
        }

        private static string BuildTres(string animationName, string texturePath, int frameCount, int columns, int width, int height, double fps)
        {
            var builder = new StringBuilder();
            builder.AppendLine("[gd_resource type=\"SpriteFrames\" load_steps=" + (frameCount + 2) + " format=3]");
            builder.AppendLine();
            builder.AppendLine("[ext_resource type=\"Texture2D\" path=\"" + texturePath.Replace("\\", "/").Replace("\"", "") + "\" id=\"1_atlas\"]");
            builder.AppendLine();
            for (int index = 0; index < frameCount; index++)
            {
                builder.AppendLine("[sub_resource type=\"AtlasTexture\" id=\"AtlasTexture_" + (index + 1).ToString("00") + "\"]");
                builder.AppendLine("atlas = ExtResource(\"1_atlas\")");
                builder.AppendLine("region = Rect2(" + ((index % columns) * width) + ", " + ((index / columns) * height) + ", " + width + ", " + height + ")");
                builder.AppendLine();
            }
            builder.AppendLine("[resource]");
            builder.AppendLine("animations = [{");
            builder.AppendLine("\"frames\": [");
            for (int index = 0; index < frameCount; index++)
            {
                builder.Append("{\"duration\": 1.0, \"texture\": SubResource(\"AtlasTexture_" + (index + 1).ToString("00") + "\")}");
                builder.AppendLine(index == frameCount - 1 ? string.Empty : ",");
            }
            builder.AppendLine("],");
            builder.AppendLine("\"loop\": true,");
            builder.AppendLine("\"name\": &\"" + animationName.Replace("\"", string.Empty) + "\",");
            builder.AppendLine("\"speed\": " + fps.ToString("0.###", CultureInfo.InvariantCulture));
            builder.AppendLine("}]");
            return builder.ToString();
        }

        private static string BuildReviewHtml(string name, string gifFile, AuditReport audit)
        {
            string findings = string.Join(Environment.NewLine, audit.Findings.Select(f => "<li class=\"" + f.Level.ToString().ToLowerInvariant() + "\"><b>" + WebUtility.HtmlEncode(f.Symbol) + "</b> " + WebUtility.HtmlEncode(f.Message) + "</li>"));
            string frameCards = string.Join(Environment.NewLine, Enumerable.Range(1, 16).Select(index => "<figure><img src=\"frames/frame_" + index.ToString("00") + ".png\" alt=\"Cuadro " + index + "\"><figcaption>" + index.ToString("00") + "</figcaption></figure>"));
            return "<!doctype html><html lang=\"es\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>" + WebUtility.HtmlEncode(name) + " · Forja de Cuadros</title><style>" +
                   "body{margin:0;background:#181716;color:#f2e9d5;font:16px system-ui,sans-serif}main{max-width:1400px;margin:auto;padding:34px}h1{margin:0;font-size:42px}h1 span{color:#c73b2f}.sub{color:#bfb39d;margin:8px 0 26px}.top{display:grid;grid-template-columns:minmax(280px,520px) 1fr;gap:28px}.card{background:#242220;border:1px solid #443e36;border-radius:16px;padding:20px}.preview{width:100%;image-rendering:auto;background:repeating-conic-gradient(#eee 0 25%,#bbb 0 50%) 50%/24px 24px;border-radius:10px}.frames{display:grid;grid-template-columns:repeat(8,1fr);gap:12px;margin-top:24px}figure{margin:0;background:#242220;border:1px solid #443e36;border-radius:10px;padding:8px;text-align:center}figure img{width:100%;aspect-ratio:1;object-fit:contain;background:repeating-conic-gradient(#fff 0 25%,#d8d1c4 0 50%) 50%/16px 16px}figcaption{padding-top:6px;color:#bfb39d}.pass b{color:#55b5a8}.warning b{color:#d59b43}.error b{color:#e45b4f}.info b{color:#8db1bb}li{margin:9px 0}@media(max-width:800px){.top{grid-template-columns:1fr}.frames{grid-template-columns:repeat(2,1fr)}}" +
                   "</style></head><body><main><h1>FORJA <span>/</span> " + WebUtility.HtmlEncode(name) + "</h1><p class=\"sub\">GIF y 16 PNG runtime exactos, sin recentrado de revision.</p><section class=\"top\"><div class=\"card\"><img class=\"preview\" src=\"" + WebUtility.HtmlEncode(gifFile) + "\" alt=\"Preview animada\"></div><div class=\"card\"><h2>Auditoria tecnica</h2><ul>" + findings + "</ul><p>La aprobacion artistica sigue siendo humana: revisar anatomia, equipo rigido y continuidad 16→01.</p></div></section><section class=\"frames\">" + frameCards + "</section></main></body></html>";
        }

        private static string BuildReadme(string name, string texturePath, AuditReport audit)
        {
            return "FORJA DE CUADROS · " + name + Environment.NewLine +
                   "================================" + Environment.NewLine +
                   "- Atlas PNG sin perdida." + Environment.NewLine +
                   "- 16 PNG exactos en la carpeta frames." + Environment.NewLine +
                   "- GIF e index.html para revision." + Environment.NewLine +
                   "- Metadata JSON y SpriteFrames .tres." + Environment.NewLine +
                   "- Ruta esperada por el .tres: " + texturePath + Environment.NewLine + Environment.NewLine +
                   "Auditoria automatica: " + (audit.HasErrors ? "CON ERRORES" : "SIN ERRORES ESTRUCTURALES") + Environment.NewLine +
                   "La revision visual humana es obligatoria antes de integrar al juego." + Environment.NewLine;
        }

        private static string SanitizeName(string value)
        {
            var builder = new StringBuilder();
            foreach (char character in value.Trim().ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(character)) builder.Append(character);
                else if ((character == ' ' || character == '-' || character == '_') && builder.Length > 0 && builder[builder.Length - 1] != '_') builder.Append('_');
            }
            return builder.ToString().Trim('_');
        }
    }
}

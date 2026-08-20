using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ForjaDeCuadros
{
    public sealed class KaggleCliService : IDisposable
    {
        public const string KaggleCliVersion = "2.2.0";
        private readonly object _processLock = new object();
        private Process? _activeProcess;

        public KaggleCliService()
        {
            RootFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ForjaDeCuadros", "Kaggle");
            EnvironmentFolder = Path.Combine(RootFolder, "cli");
            JobsFolder = Path.Combine(RootFolder, "jobs");
        }

        public string RootFolder { get; }
        public string EnvironmentFolder { get; }
        public string JobsFolder { get; }
        public string KaggleExecutable => OperatingSystem.IsWindows() ? Path.Combine(EnvironmentFolder, "Scripts", "kaggle.exe") : Path.Combine(EnvironmentFolder, "bin", "kaggle");

        public async Task<bool> IsPreparedAsync(CancellationToken cancellationToken = default)
        {
            if (!File.Exists(KaggleExecutable)) return false;
            ProcessResult result = await RunAsync(KaggleExecutable, new[] { "--version" }, null, cancellationToken, false).ConfigureAwait(false);
            return result.ExitCode == 0;
        }

        public async Task PrepareAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default)
        {
            if (await IsPreparedAsync(cancellationToken).ConfigureAwait(false))
            {
                progress?.Report("Kaggle CLI ya esta preparado en el entorno privado de Forja.");
                return;
            }

            Directory.CreateDirectory(RootFolder);
            progress?.Report("Buscando Python 3.11 o superior…");
            PythonCommand python = await FindPythonAsync(cancellationToken).ConfigureAwait(false) ?? throw new InvalidOperationException("No encontre Python 3.11 o superior. Instalalo desde https://www.python.org/downloads/ y volve a intentar.");
            progress?.Report("Creando el entorno aislado para Kaggle…");
            var createArguments = new List<string>(python.PrefixArguments) { "-m", "venv", EnvironmentFolder };
            await RunCheckedAsync(python.Executable, createArguments, progress, cancellationToken).ConfigureAwait(false);

            string environmentPython = OperatingSystem.IsWindows() ? Path.Combine(EnvironmentFolder, "Scripts", "python.exe") : Path.Combine(EnvironmentFolder, "bin", "python");
            progress?.Report("Instalando Kaggle CLI oficial " + KaggleCliVersion + "…");
            await RunCheckedAsync(environmentPython, new[] { "-m", "pip", "install", "--disable-pip-version-check", "--upgrade", "kaggle==" + KaggleCliVersion }, progress, cancellationToken).ConfigureAwait(false);
            if (!await IsPreparedAsync(cancellationToken).ConfigureAwait(false)) throw new InvalidOperationException("Kaggle CLI se instalo pero no responde correctamente.");
            progress?.Report("Kaggle CLI listo. Forja no guardo ninguna credencial.");
        }

        public async Task AuthenticateAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default)
        {
            await PrepareAsync(progress, cancellationToken).ConfigureAwait(false);
            progress?.Report("Se abrira Kaggle en tu navegador. Acepta el acceso y volve a Forja.");
            await RunCheckedAsync(KaggleExecutable, new[] { "auth", "login", "--force" }, progress, cancellationToken).ConfigureAwait(false);
            progress?.Report("OAuth termino. Verificando la cuenta…");
            await VerifyAuthenticationAsync(progress, cancellationToken).ConfigureAwait(false);
        }

        public async Task VerifyAuthenticationAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default)
        {
            if (!await IsPreparedAsync(cancellationToken).ConfigureAwait(false)) throw new InvalidOperationException("Primero prepara Kaggle CLI.");
            ProcessResult result = await RunAsync(KaggleExecutable, new[] { "datasets", "list", "--mine", "--page", "1" }, progress, cancellationToken, false).ConfigureAwait(false);
            if (result.ExitCode != 0) throw new InvalidOperationException("Kaggle no esta conectado. Usa CONECTAR CUENTA y completa OAuth en el navegador.\n\n" + LastUsefulLine(result));
            progress?.Report("Cuenta Kaggle conectada. Recorda verificar el telefono para habilitar GPU.");
        }

        public async Task<KaggleJobResult> RunImageToVideoAsync(KaggleJobRequest request, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
        {
            await VerifyAuthenticationAsync(progress, cancellationToken).ConfigureAwait(false);
            string suffix = Guid.NewGuid().ToString("N").Substring(0, 6);
            KaggleJobDefinition definition = KaggleJobTemplate.Create(request, JobsFolder, DateTimeOffset.UtcNow, suffix);
            KaggleJobTemplate.WriteFiles(request, definition);
            progress?.Report("Trabajo local: " + definition.JobId);
            progress?.Report("Subiendo la imagen como dataset privado temporal…");
            await RunCheckedAsync(KaggleExecutable, new[] { "datasets", "create", "-p", definition.DatasetFolder, "-q", "-t", "-r", "skip" }, progress, cancellationToken).ConfigureAwait(false);
            await WaitForDatasetAsync(definition.DatasetHandle, progress, cancellationToken).ConfigureAwait(false);

            progress?.Report("Enviando el script privado y solicitando GPU T4…");
            await RunCheckedAsync(KaggleExecutable, new[] { "kernels", "push", "-p", definition.KernelFolder, "--accelerator", "NvidiaTeslaT4" }, progress, cancellationToken).ConfigureAwait(false);
            progress?.Report("Trabajo remoto: https://www.kaggle.com/code/" + definition.KernelHandle);
            await WaitForKernelAsync(definition.KernelHandle, progress, cancellationToken).ConfigureAwait(false);

            progress?.Report("Descargando el MP4 terminado…");
            Directory.CreateDirectory(definition.DownloadFolder);
            await RunCheckedAsync(KaggleExecutable, new[] { "kernels", "output", definition.KernelHandle, "-p", definition.DownloadFolder, "-o", "--file-pattern", ".*\\.(mp4|json)$" }, progress, cancellationToken).ConfigureAwait(false);
            string? downloaded = Directory.EnumerateFiles(definition.DownloadFolder, "forja-output.mp4", SearchOption.AllDirectories).FirstOrDefault();
            if (downloaded == null) downloaded = Directory.EnumerateFiles(definition.DownloadFolder, "*.mp4", SearchOption.AllDirectories).OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault();
            if (downloaded == null) throw new InvalidOperationException("Kaggle termino pero no devolvio ningun MP4. Abri el trabajo remoto para revisar el log.");
            Directory.CreateDirectory(definition.OutputFolder);
            string destination = UniqueOutputPath(definition.OutputFolder, "forja-kaggle-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"), ".mp4");
            File.Copy(downloaded, destination, false);

            if (definition.DeleteRemoteAfterDownload)
            {
                progress?.Report("Eliminando el dataset y script privados de este trabajo…");
                await DeleteRemoteAsync(definition, progress, cancellationToken).ConfigureAwait(false);
            }
            DeleteLocalWorkspaceAfterSuccess(definition, destination, progress);
            progress?.Report("MP4 listo: " + destination);
            return new KaggleJobResult
            {
                JobId = definition.JobId,
                VideoPath = destination,
                KernelUrl = "https://www.kaggle.com/code/" + definition.KernelHandle
            };
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

        private async Task WaitForDatasetAsync(string handle, IProgress<string>? progress, CancellationToken cancellationToken)
        {
            DateTimeOffset deadline = DateTimeOffset.UtcNow.AddMinutes(8);
            while (DateTimeOffset.UtcNow < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ProcessResult status = await RunAsync(KaggleExecutable, new[] { "datasets", "status", handle, "--format", "json" }, null, cancellationToken, false).ConfigureAwait(false);
                string combined = status.StandardOutput + "\n" + status.StandardError;
                if (IsRateLimited(combined))
                {
                    progress?.Report("Kaggle aplico un limite temporal; esperando un minuto antes de consultar otra vez…");
                    await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken).ConfigureAwait(false);
                    continue;
                }
                if (status.ExitCode == 0 && (combined.Contains("READY", StringComparison.OrdinalIgnoreCase) || combined.Contains("COMPLETE", StringComparison.OrdinalIgnoreCase) || combined.Contains("ACTIVE", StringComparison.OrdinalIgnoreCase)))
                {
                    progress?.Report("Dataset privado disponible.");
                    return;
                }
                if (combined.Contains("ERROR", StringComparison.OrdinalIgnoreCase) || combined.Contains("FAILED", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Kaggle no pudo preparar el dataset privado.\n\n" + LastUsefulLine(status));
                progress?.Report("Kaggle esta preparando el input privado…");
                await Task.Delay(TimeSpan.FromSeconds(8), cancellationToken).ConfigureAwait(false);
            }
            throw new TimeoutException("Kaggle no termino de preparar el input dentro de ocho minutos.");
        }

        private async Task WaitForKernelAsync(string handle, IProgress<string>? progress, CancellationToken cancellationToken)
        {
            DateTimeOffset deadline = DateTimeOffset.UtcNow.AddHours(3);
            while (DateTimeOffset.UtcNow < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ProcessResult result = await RunAsync(KaggleExecutable, new[] { "kernels", "status", handle }, null, cancellationToken, false).ConfigureAwait(false);
                string combined = result.StandardOutput + "\n" + result.StandardError;
                if (IsRateLimited(combined))
                {
                    progress?.Report("Kaggle aplico un limite temporal; esperando un minuto antes de consultar otra vez…");
                    await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken).ConfigureAwait(false);
                    continue;
                }
                KaggleRunState state = KaggleStatusParser.Parse(combined);
                if (state == KaggleRunState.Complete)
                {
                    progress?.Report("Kaggle completo la generacion.");
                    return;
                }
                if (state == KaggleRunState.Failed) throw new InvalidOperationException("El trabajo Kaggle fallo. Abri su pagina para revisar el log.\n\n" + LastUsefulLine(result));
                progress?.Report(state == KaggleRunState.Running ? "GPU trabajando; Forja seguira esperando…" : "Trabajo en cola; Forja seguira esperando…");
                await Task.Delay(TimeSpan.FromSeconds(20), cancellationToken).ConfigureAwait(false);
            }
            throw new TimeoutException("El trabajo Kaggle supero tres horas. Puede continuar remoto; revisalo desde su pagina.");
        }

        private async Task DeleteRemoteAsync(KaggleJobDefinition definition, IProgress<string>? progress, CancellationToken cancellationToken)
        {
            ProcessResult kernel = await RunAsync(KaggleExecutable, new[] { "kernels", "delete", definition.KernelHandle, "--yes" }, progress, cancellationToken, false).ConfigureAwait(false);
            ProcessResult dataset = await RunAsync(KaggleExecutable, new[] { "datasets", "delete", definition.DatasetHandle, "--yes" }, progress, cancellationToken, false).ConfigureAwait(false);
            if (kernel.ExitCode != 0 || dataset.ExitCode != 0) progress?.Report("El MP4 esta a salvo, pero Kaggle no pudo borrar todo el material remoto. Revisalo desde Your Work.");
        }

        private async Task<PythonCommand?> FindPythonAsync(CancellationToken cancellationToken)
        {
            var candidates = new[]
            {
                new PythonCommand("py", new[] { "-3.13" }),
                new PythonCommand("py", new[] { "-3.12" }),
                new PythonCommand("py", new[] { "-3.11" }),
                new PythonCommand("python3", Array.Empty<string>()),
                new PythonCommand("python", Array.Empty<string>())
            };
            foreach (PythonCommand candidate in candidates)
            {
                try
                {
                    var arguments = new List<string>(candidate.PrefixArguments) { "--version" };
                    ProcessResult result = await RunAsync(candidate.Executable, arguments, null, cancellationToken, false).ConfigureAwait(false);
                    if (result.ExitCode == 0 && ParsePythonVersion(result.StandardOutput + " " + result.StandardError) is Version version && version >= new Version(3, 11)) return candidate;
                }
                catch (Exception exception) when (exception is System.ComponentModel.Win32Exception || exception is FileNotFoundException) { }
            }
            return null;
        }

        public static Version? ParsePythonVersion(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            string? versionText = value.Split(' ', '\r', '\n', '\t').FirstOrDefault(token => token.Length > 0 && char.IsDigit(token[0]) && token.Count(character => character == '.') >= 1);
            if (versionText == null) return null;
            string cleaned = new string(versionText.TakeWhile(character => char.IsDigit(character) || character == '.').ToArray());
            return Version.TryParse(cleaned, out Version? version) ? version : null;
        }

        private async Task RunCheckedAsync(string executable, IEnumerable<string> arguments, IProgress<string>? progress, CancellationToken cancellationToken)
        {
            ProcessResult result = await RunAsync(executable, arguments, progress, cancellationToken, false).ConfigureAwait(false);
            if (result.ExitCode != 0) throw new InvalidOperationException(Path.GetFileName(executable) + " termino con codigo " + result.ExitCode + ".\n\n" + LastUsefulLine(result));
        }

        private async Task<ProcessResult> RunAsync(string executable, IEnumerable<string> arguments, IProgress<string>? progress, CancellationToken cancellationToken, bool unused)
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
            var standardOutput = new StringBuilder();
            var standardError = new StringBuilder();
            process.OutputDataReceived += (_, e) => { if (e.Data != null) { standardOutput.AppendLine(e.Data); progress?.Report(e.Data); } };
            process.ErrorDataReceived += (_, e) => { if (e.Data != null) { standardError.AppendLine(e.Data); progress?.Report(e.Data); } };
            lock (_processLock) _activeProcess = process;
            try
            {
                if (!process.Start()) throw new InvalidOperationException("No se pudo iniciar " + executable + ".");
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                using CancellationTokenRegistration registration = cancellationToken.Register(() => { try { if (!process.HasExited) process.Kill(true); } catch { } });
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                process.WaitForExit();
                return new ProcessResult(process.ExitCode, standardOutput.ToString(), standardError.ToString());
            }
            finally
            {
                lock (_processLock) if (ReferenceEquals(_activeProcess, process)) _activeProcess = null;
            }
        }

        private static string LastUsefulLine(ProcessResult result)
        {
            string combined = (result.StandardError + "\n" + result.StandardOutput).Trim();
            if (combined.Length == 0) return "Sin detalle adicional.";
            string[] lines = combined.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(Environment.NewLine, lines.Skip(Math.Max(0, lines.Length - 8)));
        }

        private static bool IsRateLimited(string value)
        {
            return value.Contains("429", StringComparison.OrdinalIgnoreCase) || value.Contains("TOO MANY REQUESTS", StringComparison.OrdinalIgnoreCase) || value.Contains("RATE LIMIT", StringComparison.OrdinalIgnoreCase);
        }

        private static string UniqueOutputPath(string folder, string baseName, string extension)
        {
            string candidate = Path.Combine(folder, baseName + extension);
            for (int index = 2; File.Exists(candidate); index++) candidate = Path.Combine(folder, baseName + "-" + index + extension);
            return candidate;
        }

        private void DeleteLocalWorkspaceAfterSuccess(KaggleJobDefinition definition, string destination, IProgress<string>? progress)
        {
            try
            {
                string jobsRoot = Path.GetFullPath(JobsFolder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                string workspace = Path.GetFullPath(definition.WorkspaceFolder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                string output = Path.GetFullPath(destination);
                if (!workspace.StartsWith(jobsRoot, StringComparison.OrdinalIgnoreCase) || output.StartsWith(workspace, StringComparison.OrdinalIgnoreCase))
                {
                    progress?.Report("No se limpio el temporal local porque sus rutas no pasaron la comprobacion de seguridad.");
                    return;
                }
                if (Directory.Exists(definition.WorkspaceFolder)) Directory.Delete(definition.WorkspaceFolder, true);
                progress?.Report("Temporales locales del trabajo eliminados.");
            }
            catch (Exception exception)
            {
                progress?.Report("El MP4 esta a salvo, pero no se pudo limpiar el temporal local: " + exception.Message);
            }
        }

        private sealed class PythonCommand
        {
            public PythonCommand(string executable, IReadOnlyList<string> prefixArguments) { Executable = executable; PrefixArguments = prefixArguments; }
            public string Executable { get; }
            public IReadOnlyList<string> PrefixArguments { get; }
        }

        private readonly struct ProcessResult
        {
            public ProcessResult(int exitCode, string standardOutput, string standardError) { ExitCode = exitCode; StandardOutput = standardOutput; StandardError = standardError; }
            public int ExitCode { get; }
            public string StandardOutput { get; }
            public string StandardError { get; }
        }
    }
}

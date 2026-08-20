using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ForjaDeCuadros
{
    public partial class App : Application
    {
        [DllImport("shell32.dll", SetLastError = true)]
        private static extern int SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string appId);

        [DllImport("shell32.dll")]
        private static extern int GetCurrentProcessExplicitAppUserModelID(out IntPtr appId);

        protected override void OnStartup(StartupEventArgs e)
        {
            SetCurrentProcessExplicitAppUserModelID("io.github.ldebortoli.ForjaDeCuadros");
            AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;
            DispatcherUnhandledException += HandleDispatcherException;

            string? identityTestPath = ValueAfter(e.Args, "--identity-test");
            if (!string.IsNullOrWhiteSpace(identityTestPath))
            {
                IntPtr appIdPointer = IntPtr.Zero;
                int result = GetCurrentProcessExplicitAppUserModelID(out appIdPointer);
                string activeId = result == 0 && appIdPointer != IntPtr.Zero ? Marshal.PtrToStringUni(appIdPointer) ?? string.Empty : string.Empty;
                if (appIdPointer != IntPtr.Zero) Marshal.FreeCoTaskMem(appIdPointer);
                Directory.CreateDirectory(Path.GetDirectoryName(identityTestPath) ?? ".");
                File.WriteAllText(identityTestPath, "APP_ID=" + activeId + Environment.NewLine + "RESULT=" + result);
                Shutdown(activeId == "io.github.ldebortoli.ForjaDeCuadros" ? 0 : 1);
                return;
            }

            string? selfTestPath = ValueAfter(e.Args, "--self-test");
            if (!string.IsNullOrWhiteSpace(selfTestPath))
            {
                int exitCode = Task.Run(async () => await SelfTestRunner.RunAsync(selfTestPath).ConfigureAwait(false)).GetAwaiter().GetResult();
                Shutdown(exitCode);
                return;
            }

            string? capturePath = ValueAfter(e.Args, "--capture");
            string? kaggleCapturePath = ValueAfter(e.Args, "--capture-kaggle");
            string? splashCapturePath = ValueAfter(e.Args, "--capture-splash");
            int? captureWidth = int.TryParse(ValueAfter(e.Args, "--capture-width"), out int parsedWidth) && parsedWidth > 0 ? parsedWidth : (int?)null;
            int? captureHeight = int.TryParse(ValueAfter(e.Args, "--capture-height"), out int parsedHeight) && parsedHeight > 0 ? parsedHeight : (int?)null;
            var splash = new SplashWindow();
            splash.Show();

            if (!string.IsNullOrWhiteSpace(splashCapturePath))
            {
                var captureTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(260) };
                captureTimer.Tick += (_, __) =>
                {
                    captureTimer.Stop();
                    Directory.CreateDirectory(Path.GetDirectoryName(splashCapturePath) ?? ".");
                    var bitmap = new RenderTargetBitmap((int)Math.Ceiling(splash.ActualWidth), (int)Math.Ceiling(splash.ActualHeight), 96, 96, PixelFormats.Pbgra32);
                    bitmap.Render(splash);
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    using (var stream = File.Create(splashCapturePath)) encoder.Save(stream);
                    splash.Close();
                    Shutdown(0);
                };
                captureTimer.Start();
                return;
            }

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(capturePath == null && kaggleCapturePath == null ? 850 : 120) };
            timer.Tick += (_, __) =>
            {
                timer.Stop();
                Window main = !string.IsNullOrWhiteSpace(kaggleCapturePath)
                    ? new KaggleWindow(kaggleCapturePath, captureWidth, captureHeight)
                    : new MainWindow(capturePath, captureWidth, captureHeight);
                MainWindow = main;
                main.Show();
                splash.Close();
            };
            timer.Start();
        }

        private static string? ValueAfter(string[] args, string key)
        {
            int index = Array.FindIndex(args, value => string.Equals(value, key, StringComparison.OrdinalIgnoreCase));
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }

        private static void HandleUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            WriteCrashLog(e.ExceptionObject as Exception);
        }

        private static void HandleDispatcherException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            WriteCrashLog(e.Exception);
            MessageBox.Show("Forja de Cuadros encontro un error inesperado. Se guardo un diagnostico local.", "Forja de Cuadros", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        private static void WriteCrashLog(Exception? exception)
        {
            try
            {
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ForjaDeCuadros", "Logs");
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, "crash-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log"), exception?.ToString() ?? "Error desconocido");
            }
            catch { }
        }
    }
}

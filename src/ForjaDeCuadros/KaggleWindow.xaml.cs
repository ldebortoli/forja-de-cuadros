using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WinForms = System.Windows.Forms;

namespace ForjaDeCuadros
{
    public partial class KaggleWindow : Window
    {
        private readonly KaggleCliService _kaggle = new KaggleCliService();
        private readonly string? _capturePath;
        private CancellationTokenSource? _operationCancellation;
        private string? _jobUrl;
        private bool _isFitting;
        private bool _fitQueued;

        public KaggleWindow(string? capturePath = null, int? captureWidth = null, int? captureHeight = null, string? initialImagePath = null)
        {
            InitializeComponent();
            _capturePath = capturePath;
            if (captureWidth.HasValue) Width = Math.Max(MinWidth, captureWidth.Value);
            if (captureHeight.HasValue) Height = Math.Max(MinHeight, captureHeight.Value);
            if (!string.IsNullOrWhiteSpace(initialImagePath))
            {
                ImagePathText.Text = initialImagePath;
                ImagePathText.ToolTip = initialImagePath;
            }
            OutputFolderText.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "Forja de Cuadros", "Kaggle");
            SourceInitialized += (_, __) => Dispatcher.BeginInvoke(new Action(() => FitToCurrentMonitor(true)), DispatcherPriority.Loaded);
            LocationChanged += (_, __) => QueueFit();
            StateChanged += KaggleWindow_StateChanged;
            Loaded += KaggleWindow_Loaded;
            Closing += KaggleWindow_Closing;
        }

        public string? GeneratedVideoPath { get; private set; }

        private async void KaggleWindow_Loaded(object sender, RoutedEventArgs e)
        {
            WindowsIdentity.Apply(this);
            FitToCurrentMonitor(true);
            if (!string.IsNullOrWhiteSpace(_capturePath))
            {
                AccountReadyCheck.IsChecked = true;
                await Task.Delay(900);
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(_capturePath) ?? ".");
                    var bitmap = new RenderTargetBitmap((int)Math.Ceiling(ActualWidth), (int)Math.Ceiling(ActualHeight), 96, 96, PixelFormats.Pbgra32);
                    bitmap.Render(this);
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    using var stream = File.Create(_capturePath);
                    encoder.Save(stream);
                }
                finally { Close(); }
                return;
            }

            try
            {
                Version? installedVersion = await _kaggle.GetInstalledVersionAsync();
                if (installedVersion == null)
                {
                    ConnectionStatusText.Text = "SIN CONFIGURAR";
                    AppendProgress("Primero crea/verifica tu cuenta y despues pulsa PREPARAR KAGGLE.");
                }
                else
                {
                    string? username = await _kaggle.GetConfiguredUsernameAsync();
                    ApplyDetectedUsername(username);
                    bool current = installedVersion == new Version(KaggleCliService.KaggleCliVersion);
                    ConnectionStatusText.Text = current
                        ? username == null ? "CLI LISTA · FALTA CONECTAR" : "CUENTA @" + username + " DETECTADA"
                        : "CLI " + installedVersion + " · ACTUALIZAR";
                    AppendProgress(current
                        ? username == null ? "Kaggle CLI ya esta preparado. Conecta tu cuenta." : "Detecté la cuenta @" + username + ". Pulsá VERIFICAR o generá directamente."
                        : "Kaggle CLI " + installedVersion + " esta desactualizada. PREPARAR KAGGLE la actualiza a " + KaggleCliService.KaggleCliVersion + ".");
                }
            }
            catch { ConnectionStatusText.Text = "SIN CONFIGURAR"; }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;
            if (e.ClickCount == 2)
            {
                ToggleMaximizedState();
                return;
            }
            if (WindowState != WindowState.Normal) return;
            try { DragMove(); } catch (InvalidOperationException) { }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void Maximize_Click(object sender, RoutedEventArgs e) => ToggleMaximizedState();
        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void ToggleMaximizedState()
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void KaggleWindow_StateChanged(object? sender, EventArgs e)
        {
            bool maximized = WindowState == WindowState.Maximized;
            MaximizeWindowButton.Content = maximized ? "❐" : "□";
            MaximizeWindowButton.ToolTip = maximized ? "Restaurar" : "Maximizar";
            Dispatcher.BeginInvoke(new Action(() => FitToCurrentMonitor(true)), DispatcherPriority.Background);
        }

        private void OpenSignup_Click(object sender, RoutedEventArgs e) => OpenUrl("https://www.kaggle.com/account/login?phase=startRegisterTab");
        private void OpenAccountSettings_Click(object sender, RoutedEventArgs e) => OpenUrl("https://www.kaggle.com/settings/account");
        private void OpenApiSettings_Click(object sender, RoutedEventArgs e) => OpenUrl("https://www.kaggle.com/settings/api");
        private void OpenNotebooks_Click(object sender, RoutedEventArgs e) => OpenUrl("https://www.kaggle.com/notebooks");

        private async void Prepare_Click(object sender, RoutedEventArgs e)
        {
            await RunOperationAsync(async token =>
            {
                await _kaggle.PrepareAsync(CreateProgress(), token);
                string? username = await _kaggle.GetConfiguredUsernameAsync(token);
                ApplyDetectedUsername(username);
                ConnectionStatusText.Text = username == null ? "CLI LISTA · FALTA CONECTAR" : "CUENTA @" + username + " DETECTADA";
            }, "Preparando Kaggle CLI…");
        }

        private async void Connect_Click(object sender, RoutedEventArgs e)
        {
            if (AccountReadyCheck.IsChecked != true)
            {
                AccountGuideBorder.BringIntoView();
                MessageBox.Show(
                    this,
                    "Antes de conectar necesitás una cuenta de Kaggle creada y verificada.\n\nSeguí la guía del paso 01, confirmá el correo y completá la verificación requerida. Después marcá la casilla y volvé a pulsar CONECTAR CUENTA.",
                    "Primero creá tu cuenta de Kaggle",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            await RunOperationAsync(async token =>
            {
                string? username = await _kaggle.AuthenticateAsync(CreateProgress(), token);
                ApplyDetectedUsername(username);
                ConnectionStatusText.Text = username == null ? "CONECTADA · VERIFICA GPU" : "CONECTADA COMO @" + username;
            }, "Abriendo OAuth de Kaggle…");
        }

        private async void Verify_Click(object sender, RoutedEventArgs e)
        {
            await RunOperationAsync(async token =>
            {
                string? username = await _kaggle.VerifyAuthenticationAsync(CreateProgress(), token);
                ApplyDetectedUsername(username);
                ConnectionStatusText.Text = username == null ? "CONECTADA · VERIFICA GPU" : "CONECTADA COMO @" + username;
            }, "Verificando la cuenta…");
        }

        private void BrowseImage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Title = "Elegir imagen inicial", Filter = "Imagen|*.png;*.jpg;*.jpeg;*.webp|Todos los archivos|*.*" };
            if (dialog.ShowDialog(this) == true)
            {
                ImagePathText.Text = dialog.FileName;
                ImagePathText.ToolTip = dialog.FileName;
            }
        }

        private void BrowseOutput_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new WinForms.FolderBrowserDialog { Description = "Carpeta local para los videos de Kaggle", SelectedPath = OutputFolderText.Text, ShowNewFolderButton = true };
            if (dialog.ShowDialog() == WinForms.DialogResult.OK) OutputFolderText.Text = dialog.SelectedPath;
        }

        private async void Generate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ApplyDetectedUsername(await _kaggle.GetConfiguredUsernameAsync());
                KaggleJobRequest request = ReadRequest();
                await RunOperationAsync(async token =>
                {
                    KaggleJobResult result = await _kaggle.RunImageToVideoAsync(request, CreateProgress(), token);
                    GeneratedVideoPath = result.VideoPath;
                    _jobUrl = result.KernelUrl;
                    OpenJobButton.IsEnabled = true;
                    UseVideoButton.IsEnabled = true;
                    ConnectionStatusText.Text = "MP4 LISTO";
                }, "Sincronizando con Kaggle…");
            }
            catch (Exception exception)
            {
                ShowError(exception);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _operationCancellation?.Cancel();
            _kaggle.CancelActive();
            AppendProgress("Cancelaste la espera local. Si el trabajo ya estaba en GPU puede seguir ejecutandose en Kaggle.");
        }

        private void OpenJob_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_jobUrl)) OpenUrl(_jobUrl);
        }

        private void UseVideo_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(GeneratedVideoPath) || !File.Exists(GeneratedVideoPath)) return;
            DialogResult = true;
            Close();
        }

        private KaggleJobRequest ReadRequest()
        {
            string resolution = (ResolutionCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "512x512";
            string[] dimensions = resolution.Split('x');
            int width = int.Parse(dimensions[0], CultureInfo.InvariantCulture);
            int height = int.Parse(dimensions[1], CultureInfo.InvariantCulture);
            int frames = int.Parse((FramesCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "97", CultureInfo.InvariantCulture);
            return new KaggleJobRequest
            {
                Username = UsernameText.Text,
                ImagePath = ImagePathText.Text,
                Prompt = PromptText.Text,
                OutputFolder = OutputFolderText.Text,
                Width = width,
                Height = height,
                NumberOfFrames = frames,
                FramesPerSecond = int.TryParse(FpsText.Text, NumberStyles.Integer, CultureInfo.CurrentCulture, out int fps) ? fps : 30,
                Seed = int.TryParse(SeedText.Text, NumberStyles.Integer, CultureInfo.CurrentCulture, out int seed) ? seed : 171198,
                DeleteRemoteAfterDownload = DeleteRemoteCheck.IsChecked == true
            };
        }

        private void ApplyDetectedUsername(string? username)
        {
            if (string.IsNullOrWhiteSpace(username)) return;
            UsernameText.Text = username;
            UsernameHelpText.Text = "Cuenta detectada: @" + username + " · perfil: kaggle.com/" + username;
            UsernameText.ToolTip = "https://www.kaggle.com/" + username;
        }

        private async Task RunOperationAsync(Func<CancellationToken, Task> operation, string startMessage)
        {
            if (_operationCancellation != null) throw new InvalidOperationException("Ya hay una operacion en curso.");
            AppendProgress(startMessage);
            SetBusy(true);
            _operationCancellation = new CancellationTokenSource();
            try
            {
                await operation(_operationCancellation.Token);
            }
            catch (OperationCanceledException)
            {
                AppendProgress("Operacion local cancelada.");
            }
            catch (Exception exception)
            {
                AppendProgress("ERROR: " + exception.Message);
                ShowError(exception);
            }
            finally
            {
                _operationCancellation.Dispose();
                _operationCancellation = null;
                SetBusy(false);
            }
        }

        private IProgress<string> CreateProgress()
        {
            return new Progress<string>(message =>
            {
                AppendProgress(message);
                const string prefix = "Trabajo remoto: ";
                if (message.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    _jobUrl = message.Substring(prefix.Length).Trim();
                    OpenJobButton.IsEnabled = Uri.TryCreate(_jobUrl, UriKind.Absolute, out _);
                }
            });
        }

        private void AppendProgress(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            if (ProgressText.Text.StartsWith("Forja esta lista.", StringComparison.Ordinal)) ProgressText.Clear();
            ProgressText.AppendText((ProgressText.Text.Length == 0 ? string.Empty : Environment.NewLine) + DateTime.Now.ToString("HH:mm:ss") + "  " + message.Trim());
            ProgressText.ScrollToEnd();
        }

        private void SetBusy(bool busy)
        {
            PrepareButton.IsEnabled = !busy;
            ConnectButton.IsEnabled = !busy;
            VerifyButton.IsEnabled = !busy;
            GenerateButton.IsEnabled = !busy;
            CancelButton.IsEnabled = busy;
        }

        private void QueueFit()
        {
            if (_isFitting || _fitQueued || !IsLoaded) return;
            _fitQueued = true;
            Dispatcher.BeginInvoke(new Action(() => { _fitQueued = false; FitToCurrentMonitor(false); }), DispatcherPriority.Background);
        }

        private void FitToCurrentMonitor(bool force)
        {
            if (_isFitting) return;
            IntPtr handle = new WindowInteropHelper(this).Handle;
            if (handle == IntPtr.Zero) return;
            WinForms.Screen screen = WinForms.Screen.FromHandle(handle);
            PresentationSource? source = PresentationSource.FromVisual(this);
            if (source?.CompositionTarget == null) return;
            Matrix fromDevice = source.CompositionTarget.TransformFromDevice;
            Point topLeft = fromDevice.Transform(new Point(screen.WorkingArea.Left, screen.WorkingArea.Top));
            Point bottomRight = fromDevice.Transform(new Point(screen.WorkingArea.Right, screen.WorkingArea.Bottom));
            const double margin = 8;
            double usableWidth = Math.Max(420, bottomRight.X - topLeft.X - margin * 2);
            double usableHeight = Math.Max(380, bottomRight.Y - topLeft.Y - margin * 2);
            _isFitting = true;
            try
            {
                MinWidth = Math.Min(720, usableWidth);
                MinHeight = Math.Min(540, usableHeight);
                MaxWidth = usableWidth;
                MaxHeight = usableHeight;
                if (WindowState != WindowState.Normal) return;
                Width = Math.Min(Width, usableWidth);
                Height = Math.Min(Height, usableHeight);
                double currentLeft = double.IsNaN(Left) ? topLeft.X + margin : Left;
                double currentTop = double.IsNaN(Top) ? topLeft.Y + margin : Top;
                if (force || currentLeft < topLeft.X + margin || currentLeft + Width > bottomRight.X - margin) Left = Math.Max(topLeft.X + margin, Math.Min(currentLeft, bottomRight.X - margin - Width));
                if (force || currentTop < topLeft.Y + margin || currentTop + Height > bottomRight.Y - margin) Top = Math.Max(topLeft.Y + margin, Math.Min(currentTop, bottomRight.Y - margin - Height));
            }
            finally { _isFitting = false; }
        }

        private static void OpenUrl(string url)
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }

        private void ShowError(Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Kaggle I2V", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void KaggleWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _operationCancellation?.Cancel();
            _kaggle.CancelActive();
            _kaggle.Dispose();
        }
    }
}

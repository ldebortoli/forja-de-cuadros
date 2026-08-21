using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
    public partial class MainWindow : Window
    {
        private readonly FfmpegService _ffmpeg;
        private readonly string _sessionFolder;
        private readonly DispatcherTimer _previewTimer;
        private DispatcherTimer? _edgePreviewTimer;
        private readonly string? _capturePath;
        private readonly bool _captureAlphaControls;
        private CancellationTokenSource? _operationCancellation;
        private VideoInfo? _videoInfo;
        private string? _selectedVideo;
        private string? _selectedImage;
        private string? _preparedImage;
        private FrameItem? _previewCandidate;
        private AuditReport? _auditReport;
        private ProcessingOptions? _lastProcessingOptions;
        private int _previewIndex;
        private bool _isPlaying;
        private bool _selectionGuard;
        private bool _isFittingWindow;
        private bool _fitQueued;
        private string? _activeMonitorDeviceName;
        private HwndSource? _windowSource;

        private const int WmGetMinMaxInfo = 0x0024;
        private const uint MonitorDefaultToNearest = 0x00000002;

        public MainWindow(string? capturePath = null, int? captureWidth = null, int? captureHeight = null, bool captureAlphaControls = false)
        {
            InitializeComponent();
            DataContext = this;
            _capturePath = capturePath;
            _captureAlphaControls = captureAlphaControls;
            if (captureWidth.HasValue) Width = Math.Max(MinWidth, captureWidth.Value);
            if (captureHeight.HasValue) Height = Math.Max(MinHeight, captureHeight.Value);
            _ffmpeg = new FfmpegService();
            _sessionFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ForjaDeCuadros", "Sessions", DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(_sessionFolder);
            ExportFolderText.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Forja de Cuadros", "Exportaciones");
            _previewTimer = new DispatcherTimer();
            _previewTimer.Tick += PreviewTimer_Tick;
            UpdatePreviewInterval();
            _edgePreviewTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(90) };
            _edgePreviewTimer.Tick += (_, __) =>
            {
                _edgePreviewTimer.Stop();
                RefreshEdgePreview();
            };
            UpdateEdgeControlLabels();
            AuditFindings.Add(new AuditFinding { Level = FindingLevel.Info, Message = "Todavia no hay una seleccion procesada." });
            Loaded += (_, __) => Dispatcher.BeginInvoke(new Action(() => WindowsIdentity.Apply(this)), DispatcherPriority.ApplicationIdle);
            SourceInitialized += MainWindow_SourceInitialized;
            LocationChanged += MainWindow_LocationChanged;
            StateChanged += MainWindow_StateChanged;
            Closing += MainWindow_Closing;
            Loaded += MainWindow_Loaded;
        }

        public ObservableCollection<FrameItem> Candidates { get; } = new ObservableCollection<FrameItem>();
        public ObservableCollection<ProcessedFrame> ProcessedFrames { get; } = new ObservableCollection<ProcessedFrame>();
        public ObservableCollection<AuditFinding> AuditFindings { get; } = new ObservableCollection<AuditFinding>();

        private void MainWindow_SourceInitialized(object? sender, EventArgs e)
        {
            IntPtr handle = new WindowInteropHelper(this).Handle;
            _windowSource = HwndSource.FromHwnd(handle);
            _windowSource?.AddHook(WindowMessageHook);
            Dispatcher.BeginInvoke(new Action(() => FitToCurrentMonitor(true)), DispatcherPriority.Loaded);
        }

        private void MainWindow_LocationChanged(object? sender, EventArgs e)
        {
            if (_isFittingWindow || _fitQueued || !IsLoaded) return;
            _fitQueued = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _fitQueued = false;
                FitToCurrentMonitor(false);
            }), DispatcherPriority.Background);
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            if (MaximizeWindowButton != null)
            {
                bool maximized = WindowState == WindowState.Maximized;
                MaximizeWindowButton.Content = maximized ? "❐" : "□";
                MaximizeWindowButton.ToolTip = maximized ? "Restaurar" : "Maximizar";
            }
            Dispatcher.BeginInvoke(new Action(() => FitToCurrentMonitor(true)), DispatcherPriority.Background);
        }

        private void FitToCurrentMonitor(bool force)
        {
            if (_isFittingWindow) return;
            IntPtr handle = new WindowInteropHelper(this).Handle;
            if (handle == IntPtr.Zero) return;
            WinForms.Screen screen = WinForms.Screen.FromHandle(handle);
            if (!force && string.Equals(_activeMonitorDeviceName, screen.DeviceName, StringComparison.OrdinalIgnoreCase)) return;

            PresentationSource? source = PresentationSource.FromVisual(this);
            if (source?.CompositionTarget == null) return;
            Matrix fromDevice = source.CompositionTarget.TransformFromDevice;
            Point workTopLeft = fromDevice.Transform(new Point(screen.WorkingArea.Left, screen.WorkingArea.Top));
            Point workBottomRight = fromDevice.Transform(new Point(screen.WorkingArea.Right, screen.WorkingArea.Bottom));
            const double margin = 6;
            double usableWidth = Math.Max(320, workBottomRight.X - workTopLeft.X - margin * 2);
            double usableHeight = Math.Max(300, workBottomRight.Y - workTopLeft.Y - margin * 2);

            _isFittingWindow = true;
            try
            {
                _activeMonitorDeviceName = screen.DeviceName;
                MinWidth = Math.Min(1060, usableWidth);
                MinHeight = Math.Min(560, usableHeight);
                MaxWidth = usableWidth;
                MaxHeight = usableHeight;
                if (WindowState != WindowState.Normal) return;

                Width = Math.Min(Width, usableWidth);
                Height = Math.Min(Height, usableHeight);
                double minimumLeft = workTopLeft.X + margin;
                double minimumTop = workTopLeft.Y + margin;
                double maximumLeft = Math.Max(minimumLeft, workBottomRight.X - margin - Width);
                double maximumTop = Math.Max(minimumTop, workBottomRight.Y - margin - Height);
                double currentLeft = double.IsNaN(Left) ? minimumLeft : Left;
                double currentTop = double.IsNaN(Top) ? minimumTop : Top;
                Left = Math.Max(minimumLeft, Math.Min(currentLeft, maximumLeft));
                Top = Math.Max(minimumTop, Math.Min(currentTop, maximumTop));
            }
            finally
            {
                _isFittingWindow = false;
            }
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

        private void MinimizeWindow_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void MaximizeWindow_Click(object sender, RoutedEventArgs e) => ToggleMaximizedState();

        private void ToggleMaximizedState()
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void CloseWindow_Click(object sender, RoutedEventArgs e) => Close();

        private IntPtr WindowMessageHook(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (message != WmGetMinMaxInfo || lParam == IntPtr.Zero) return IntPtr.Zero;
            IntPtr monitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
            if (monitor == IntPtr.Zero) return IntPtr.Zero;
            var monitorInfo = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
            if (!GetMonitorInfo(monitor, ref monitorInfo)) return IntPtr.Zero;

            var minMax = Marshal.PtrToStructure<MinMaxInfo>(lParam);
            minMax.MaxPosition.X = Math.Abs(monitorInfo.WorkArea.Left - monitorInfo.MonitorArea.Left);
            minMax.MaxPosition.Y = Math.Abs(monitorInfo.WorkArea.Top - monitorInfo.MonitorArea.Top);
            minMax.MaxSize.X = Math.Abs(monitorInfo.WorkArea.Right - monitorInfo.WorkArea.Left);
            minMax.MaxSize.Y = Math.Abs(monitorInfo.WorkArea.Bottom - monitorInfo.WorkArea.Top);
            Marshal.StructureToPtr(minMax, lParam, true);
            handled = true;
            return IntPtr.Zero;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_capturePath)) return;
            await Task.Delay(500);
            if (_captureAlphaControls)
            {
                AlphaCutoffEnabledCheck.BringIntoView();
                await Task.Delay(120);
                WorkflowScrollViewer.ScrollToVerticalOffset(WorkflowScrollViewer.VerticalOffset + 300);
                await Task.Delay(280);
            }
            else
            {
                await Task.Delay(400);
            }
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
            finally
            {
                Close();
            }
        }

        private async void BrowseVideo_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Elegir video para Forja de Cuadros",
                Filter = "Video e imagen animada|*.mp4;*.mov;*.webm;*.mkv;*.avi;*.gif|Todos los archivos|*.*"
            };
            if (dialog.ShowDialog(this) != true) return;
            await LoadVideoAsync(dialog.FileName);
        }

        private void BrowseInitialImage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Elegir imagen inicial",
                Filter = "Imagen|*.png;*.jpg;*.jpeg;*.webp;*.bmp;*.tif;*.tiff|Todos los archivos|*.*"
            };
            if (dialog.ShowDialog(this) != true) return;

            _selectedImage = dialog.FileName;
            _preparedImage = null;
            InitialImagePathText.Text = dialog.FileName;
            InitialImagePathText.ToolTip = dialog.FileName;
            KaggleImagePathText.Text = "Preparando chroma verde…";
            KaggleImagePathText.ToolTip = null;
            ImagePrepStatusText.Text = "Imagen elegida. Preparando chroma verde automáticamente…";
            ImagePrepStatusText.ToolTip = null;
            PrepareInitialImage("#00FF00");
        }

        private void PrepareInitialImage_Click(object sender, RoutedEventArgs e)
        {
            string hex = (sender as Button)?.Tag?.ToString() ?? "#00FF00";
            PrepareInitialImage(hex);
        }

        private bool PrepareInitialImage(string hex)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_selectedImage)) throw new InvalidOperationException("Primero elegí una imagen en el paso 00.");
                (byte keyR, byte keyG, byte keyB) = ParseHex(hex);
                string presetName = hex.Equals("#0066FF", StringComparison.OrdinalIgnoreCase) ? "chroma-azul" : "chroma-verde";
                string outputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ForjaDeCuadros", "PreparedImages");
                string outputPath = ImagePreparationService.CreateOutputPath(_selectedImage, outputFolder, presetName);
                PreparedImageResult result = ImagePreparationService.Prepare(_selectedImage, outputPath, keyR, keyG, keyB);
                _preparedImage = result.OutputPath;
                KaggleImagePathText.Text = result.OutputPath;
                KaggleImagePathText.ToolTip = result.OutputPath;

                if (result.HadTransparency)
                {
                    double transparentPercent = 100.0 * result.TransparentPixelCount / Math.Max(1, result.PixelCount);
                    ImagePrepStatusText.Text = $"Chroma listo · {result.Width} × {result.Height} · {transparentPercent:0.#} % con transparencia. Kaggle usará este PNG.";
                }
                else
                {
                    ImagePrepStatusText.Text = $"PNG listo · {result.Width} × {result.Height}. La imagen era opaca, por eso el chroma no queda visible.";
                }
                ImagePrepStatusText.ToolTip = result.OutputPath;
                return true;
            }
            catch (Exception exception)
            {
                _preparedImage = null;
                KaggleImagePathText.Text = "No se pudo preparar la imagen";
                KaggleImagePathText.ToolTip = null;
                ShowError("No pude preparar la imagen", exception);
                return false;
            }
        }

        private async Task LoadVideoAsync(string path)
        {
            try
            {
                SetBusy(true, "Leyendo el video…");
                _selectedVideo = path;
                _videoInfo = await _ffmpeg.ProbeAsync(path);
                VideoPathText.Text = path;
                VideoPathText.ToolTip = path;
                VideoInfoText.Text = _videoInfo.Summary;
                StartText.Text = "0";
                EndText.Text = Math.Min(4, _videoInfo.Duration).ToString("0.###", CultureInfo.CurrentCulture);
                ExtractButton.IsEnabled = true;
                CandidateSummaryText.Text = "Video listo. Elegí el tramo y extraé candidatos.";
            }
            catch (Exception exception)
            {
                ShowError("No pude leer el video", exception);
                _selectedVideo = null;
                _videoInfo = null;
            }
            finally { SetBusy(false); }
        }

        private async void Kaggle_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_selectedImage) && string.IsNullOrWhiteSpace(_preparedImage) && !PrepareInitialImage("#00FF00")) return;
            var dialog = new KaggleWindow(initialImagePath: _preparedImage ?? _selectedImage) { Owner = this };
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.GeneratedVideoPath))
            {
                await LoadVideoAsync(dialog.GeneratedVideoPath);
            }
        }

        private async void Extract_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedVideo == null || _videoInfo == null) return;
            try
            {
                double start = ReadDouble(StartText.Text, 0);
                double end = ReadDouble(EndText.Text, _videoInfo.Duration);
                int everyN = ReadInt(EveryNText.Text, 2);
                if (start < 0 || end <= start || end > _videoInfo.Duration + 0.1) throw new InvalidOperationException("El tramo debe quedar dentro del video y tener duracion positiva.");
                if (everyN < 1 || everyN > 120) throw new InvalidOperationException("Cada N cuadros debe estar entre 1 y 120.");
                string candidateFolder = Path.Combine(_sessionFolder, "candidates-" + DateTime.Now.ToString("HHmmssfff"));
                SetBusy(true, "Extrayendo fotogramas…");
                _operationCancellation = new CancellationTokenSource();
                var paths = await _ffmpeg.ExtractFramesAsync(_selectedVideo, candidateFolder, start, end, everyN, 160, _operationCancellation.Token);
                Candidates.Clear();
                ProcessedFrames.Clear();
                _auditReport = null;
                _lastProcessingOptions = null;
                foreach (string candidatePath in paths)
                {
                    int index = Candidates.Count;
                    var item = new FrameItem { Number = index + 1, Timestamp = start + index * everyN / Math.Max(0.001, _videoInfo.FramesPerSecond), ImagePath = candidatePath };
                    item.PropertyChanged += Candidate_PropertyChanged;
                    Candidates.Add(item);
                }
                if (Candidates.Count > 0) ShowCandidate(Candidates[0]);
                AutoSelectFrames();
                CandidateSummaryText.Text = Candidates.Count + " candidatos · " + SelectedCount + "/16 elegidos";
                ProcessedSummaryText.Text = "Selección pendiente de procesado.";
                ExportButton.IsEnabled = false;
            }
            catch (OperationCanceledException) { CandidateSummaryText.Text = "Extraccion cancelada."; }
            catch (Exception exception) { ShowError("No pude extraer los cuadros", exception); }
            finally
            {
                _operationCancellation?.Dispose();
                _operationCancellation = null;
                SetBusy(false);
            }
        }

        private int SelectedCount => Candidates.Count(item => item.IsSelected);

        private void AutoSelect_Click(object sender, RoutedEventArgs e) => AutoSelectFrames();

        private void AutoSelectFrames()
        {
            _selectionGuard = true;
            foreach (FrameItem item in Candidates) item.IsSelected = false;
            if (Candidates.Count >= 16)
            {
                for (int slot = 0; slot < 16; slot++)
                {
                    int index = (int)Math.Round(slot * (Candidates.Count - 1) / 15.0);
                    Candidates[index].IsSelected = true;
                }
            }
            _selectionGuard = false;
            UpdateSelectionState();
        }

        private void ClearSelection_Click(object sender, RoutedEventArgs e)
        {
            _selectionGuard = true;
            foreach (FrameItem item in Candidates) item.IsSelected = false;
            _selectionGuard = false;
            UpdateSelectionState();
        }

        private void Candidate_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(FrameItem.IsSelected) || _selectionGuard) return;
            if (SelectedCount > 16 && sender is FrameItem item)
            {
                _selectionGuard = true;
                item.IsSelected = false;
                _selectionGuard = false;
                System.Media.SystemSounds.Beep.Play();
            }
            UpdateSelectionState();
        }

        private void SelectionCheck_Click(object sender, RoutedEventArgs e) => UpdateSelectionState();

        private void UpdateSelectionState()
        {
            int count = SelectedCount;
            CandidateSummaryText.Text = Candidates.Count + " candidatos · " + count + "/16 elegidos";
            ProcessButton.IsEnabled = count == 16;
        }

        private void Candidate_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is FrameItem item) ShowCandidate(item);
        }

        private void ShowCandidate(FrameItem item)
        {
            _previewCandidate = item;
            CandidatePreview.Source = ImageLoading.LoadBitmap(item.ImagePath);
            SampleHintText.Text = item.Caption + " · hacé clic sobre el fondo para tomar ese color.";
            QueueEdgePreview();
        }

        private void CandidatePreview_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_previewCandidate == null || CandidatePreview.Source is not BitmapSource source) return;
            Point point = e.GetPosition(CandidatePreview);
            double scale = Math.Min(CandidatePreview.ActualWidth / source.PixelWidth, CandidatePreview.ActualHeight / source.PixelHeight);
            double offsetX = (CandidatePreview.ActualWidth - source.PixelWidth * scale) / 2.0;
            double offsetY = (CandidatePreview.ActualHeight - source.PixelHeight * scale) / 2.0;
            int x = (int)Math.Floor((point.X - offsetX) / scale);
            int y = (int)Math.Floor((point.Y - offsetY) / scale);
            if (x < 0 || y < 0 || x >= source.PixelWidth || y >= source.PixelHeight) return;
            FrameBuffer buffer = FrameBuffer.LoadPng(_previewCandidate.ImagePath);
            var color = buffer.PixelAt(x, y);
            ChromaHexText.Text = "#" + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
            SampleHintText.Text = "Color tomado: " + ChromaHexText.Text + ". Procesá de nuevo para aplicarlo.";
            QueueEdgePreview();
        }

        private void ChromaPreset_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is string color)
            {
                ChromaHexText.Text = color;
                QueueEdgePreview();
            }
        }

        private void EdgeControl_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateEdgeControlLabels();
            QueueEdgePreview();
        }

        private void EdgeControl_Changed(object sender, RoutedEventArgs e)
        {
            UpdateEdgeControlLabels();
            QueueEdgePreview();
        }

        private void UpdateEdgeControlLabels()
        {
            if (AlphaCutoffValueText == null || AlphaSoftnessValueText == null || AlphaCutoffSlider == null || AlphaSoftnessSlider == null) return;
            AlphaCutoffValueText.Text = Math.Round(AlphaCutoffSlider.Value) + " %";
            AlphaSoftnessValueText.Text = Math.Round(AlphaSoftnessSlider.Value) + " %";
        }

        private void QueueEdgePreview()
        {
            if (_edgePreviewTimer == null || !IsInitialized) return;
            _edgePreviewTimer.Stop();
            _edgePreviewTimer.Start();
        }

        private void RefreshEdgePreview()
        {
            if (_previewCandidate == null)
            {
                AlphaPreviewImage.Source = null;
                AlphaPreviewHintText.Text = "Elegí un candidato para revisar sus bordes mientras movés los controles.";
                return;
            }

            try
            {
                ProcessingOptions options = ReadProcessingOptions();
                FrameBuffer preview = FrameBuffer.LoadPng(_previewCandidate.ImagePath).ApplyChroma(options).ApplyAlphaCutoff(options);
                AlphaPreviewImage.Source = preview.ToBitmapSource();
                AlphaPreviewHintText.Text = options.AlphaCutoffEnabled
                    ? $"Vista en vivo · corte {options.AlphaCutoffPercent:0} % · suavizado {options.AlphaSoftnessPercent:0} %."
                    : "Vista en vivo · corte alfa desactivado.";
            }
            catch
            {
                AlphaPreviewHintText.Text = "No pude actualizar la vista: revisá el color y los valores del paso 03.";
            }
        }

        private async void Process_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selected = Candidates.Where(item => item.IsSelected).OrderBy(item => item.Number).ToArray();
                ProcessingOptions options = ReadProcessingOptions();
                string folder = Path.Combine(_sessionFolder, "processed-" + DateTime.Now.ToString("HHmmssfff"));
                SetBusy(true, "Limpiando, registrando y auditando…");
                var processed = await Task.Run(() => FrameProcessor.Process(selected, options, folder));
                AuditReport audit = AuditService.Audit(processed, options);
                _auditReport = audit;
                _lastProcessingOptions = options;
                ProcessedFrames.Clear();
                foreach (ProcessedFrame frame in processed) ProcessedFrames.Add(frame);
                AuditFindings.Clear();
                foreach (AuditFinding finding in audit.Findings) AuditFindings.Add(finding);
                _previewIndex = 0;
                ShowProcessedFrame(0);
                PlayButton.IsEnabled = true;
                ExportButton.IsEnabled = true;
                ProcessedSummaryText.Text = audit.HasErrors ? "Hay alertas estructurales antes de exportar." : "Listos para revisión y exportación.";
            }
            catch (Exception exception) { ShowError("No pude procesar la selección", exception); }
            finally { SetBusy(false); }
        }

        private ProcessingOptions ReadProcessingOptions()
        {
            (byte r, byte g, byte b) = ParseHex(ChromaHexText.Text);
            return new ProcessingOptions
            {
                ChromaEnabled = ChromaEnabledCheck.IsChecked == true,
                KeyR = r, KeyG = g, KeyB = b,
                ChromaTolerance = ToleranceSlider.Value,
                EdgeSoftness = SoftnessSlider.Value,
                SpillSuppression = Math.Clamp(ReadDouble(SpillText.Text, 65) / 100.0, 0, 1),
                HaloPixels = Math.Clamp(ReadInt(HaloText.Text, 1), 0, 3),
                IslandCleanupPixels = Math.Clamp(ReadInt(IslandText.Text, 24), 0, 4096),
                AlphaCutoffEnabled = AlphaCutoffEnabledCheck.IsChecked == true,
                AlphaCutoffPercent = Math.Clamp(AlphaCutoffSlider.Value, 0, 100),
                AlphaSoftnessPercent = Math.Clamp(AlphaSoftnessSlider.Value, 0, 25),
                CanvasWidth = ReadInt(CanvasWidthText.Text, 256),
                CanvasHeight = ReadInt(CanvasHeightText.Text, 256),
                RootX = ReadInt(RootXText.Text, 128),
                GroundY = ReadInt(GroundYText.Text, 234),
                Padding = Math.Clamp(ReadInt(PaddingText.Text, 10), 0, 256),
                RegistrationMode = RegistrationCombo.SelectedIndex == 1 ? RegistrationMode.GroundPreserveMotion : RegistrationCombo.SelectedIndex == 2 ? RegistrationMode.CameraLocked : RegistrationMode.RootAndGround
            };
        }

        private void ProcessedFrame_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is ProcessedFrame frame) ShowProcessedFrame(Math.Max(0, ProcessedFrames.IndexOf(frame)));
        }

        private void ShowProcessedFrame(int index)
        {
            if (ProcessedFrames.Count == 0) return;
            _previewIndex = (index + ProcessedFrames.Count) % ProcessedFrames.Count;
            ProcessedFrame frame = ProcessedFrames[_previewIndex];
            ProcessedPreview.Source = frame.Preview;
            PreviewFrameText.Text = (_previewIndex + 1).ToString("00") + " / 16";
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            _isPlaying = !_isPlaying;
            if (_isPlaying) { UpdatePreviewInterval(); _previewTimer.Start(); PlayButton.Content = "❚❚ PAUSAR"; }
            else { _previewTimer.Stop(); PlayButton.Content = "▶ REPRODUCIR"; }
        }

        private void PreviewTimer_Tick(object? sender, EventArgs e) => ShowProcessedFrame(_previewIndex + 1);

        private void PreviewFpsSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (PreviewFpsText == null) return;
            PreviewFpsText.Text = Math.Round(PreviewFpsSlider.Value) + " FPS";
            UpdatePreviewInterval();
        }

        private void UpdatePreviewInterval()
        {
            if (_previewTimer == null || PreviewFpsSlider == null) return;
            _previewTimer.Interval = TimeSpan.FromSeconds(1.0 / Math.Max(1, PreviewFpsSlider.Value));
        }

        private void PreviewBackgroundCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PreviewSurface == null) return;
            PreviewSurface.Background = PreviewBackgroundCombo.SelectedIndex switch
            {
                1 => new SolidColorBrush(Color.FromRgb(242, 233, 213)),
                2 => new SolidColorBrush(Color.FromRgb(24, 23, 22)),
                3 => new SolidColorBrush(Color.FromRgb(0, 255, 0)),
                _ => (Brush)FindResource("CheckerBrush")
            };
        }

        private void BrowseExport_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new WinForms.FolderBrowserDialog { Description = "Elegí dónde guardar los paquetes de Forja de Cuadros", SelectedPath = ExportFolderText.Text, UseDescriptionForTitle = true };
            if (dialog.ShowDialog() == WinForms.DialogResult.OK) ExportFolderText.Text = dialog.SelectedPath;
        }

        private async void Export_Click(object sender, RoutedEventArgs e)
        {
            if (ProcessedFrames.Count != 16 || _auditReport == null || _lastProcessingOptions == null) return;
            try
            {
                ProcessingOptions processing = _lastProcessingOptions;
                string animation = AnimationNameText.Text.Trim();
                if (string.IsNullOrWhiteSpace(animation)) throw new InvalidOperationException("Escribí un nombre de animación.");
                if (GodotPathText.Text.Contains("nueva_animacion", StringComparison.OrdinalIgnoreCase)) GodotPathText.Text = "res://assets/sprites/generated/" + animation.ToLowerInvariant().Replace(' ', '_') + "_atlas.png";
                var options = new ExportOptions
                {
                    BaseFolder = ExportFolderText.Text,
                    AnimationName = animation,
                    Columns = AtlasLayoutCombo.SelectedIndex == 1 ? 16 : 4,
                    FramesPerSecond = Math.Clamp(ReadDouble(ExportFpsText.Text, 12), 1, 60),
                    GodotTexturePath = GodotPathText.Text
                };
                SetBusy(true, "Empaquetando PNG, atlas, GIF, JSON y Godot…");
                _operationCancellation = new CancellationTokenSource();
                ExportResult result = await ExportService.ExportAsync(ProcessedFrames.ToArray(), processing, _auditReport, options, _ffmpeg, _operationCancellation.Token);
                ProcessedSummaryText.Text = "Exportado en " + result.Folder;
                var answer = MessageBox.Show(this, "Paquete exportado correctamente.\n\n" + result.Folder + "\n\n¿Abrir la carpeta?", "Forja de Cuadros", MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (answer == MessageBoxResult.Yes) Process.Start(new ProcessStartInfo { FileName = result.Folder, UseShellExecute = true });
            }
            catch (OperationCanceledException) { ProcessedSummaryText.Text = "Exportación cancelada."; }
            catch (Exception exception) { ShowError("No pude exportar el paquete", exception); }
            finally
            {
                _operationCancellation?.Dispose();
                _operationCancellation = null;
                SetBusy(false);
            }
        }

        private void SetBusy(bool busy, string message = "Trabajando…")
        {
            BusyText.Text = message;
            BusyOverlay.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
            WorkspaceGrid.IsEnabled = !busy;
            if (busy) BusyOverlay.IsEnabled = true;
        }

        private static int ReadInt(string value, int fallback)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.CurrentCulture, out int parsed) ? parsed : fallback;
        }

        private static double ReadDouble(string value, double fallback)
        {
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out double parsed)) return parsed;
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed) ? parsed : fallback;
        }

        private static (byte R, byte G, byte B) ParseHex(string value)
        {
            string hex = value.Trim().TrimStart('#');
            if (hex.Length != 6 || !int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int rgb)) throw new InvalidOperationException("El color clave debe tener formato #RRGGBB.");
            return ((byte)((rgb >> 16) & 255), (byte)((rgb >> 8) & 255), (byte)(rgb & 255));
        }

        private void ShowError(string title, Exception exception)
        {
            MessageBox.Show(this, exception.Message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            _windowSource?.RemoveHook(WindowMessageHook);
            _previewTimer.Stop();
            _edgePreviewTimer?.Stop();
            _operationCancellation?.Cancel();
            _ffmpeg.CancelActive();
            _ffmpeg.Dispose();
            try { if (Directory.Exists(_sessionFolder)) Directory.Delete(_sessionFolder, true); } catch { }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr handle, uint flags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo monitorInfo);

        [StructLayout(LayoutKind.Sequential)]
        private struct NativePoint
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MinMaxInfo
        {
            public NativePoint Reserved;
            public NativePoint MaxSize;
            public NativePoint MaxPosition;
            public NativePoint MinTrackSize;
            public NativePoint MaxTrackSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MonitorInfo
        {
            public int Size;
            public NativeRect MonitorArea;
            public NativeRect WorkArea;
            public uint Flags;
        }
    }
}

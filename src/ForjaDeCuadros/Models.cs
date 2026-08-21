using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

namespace ForjaDeCuadros
{
    public sealed class FrameItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        private BitmapImage? _thumbnail;

        public int Number { get; set; }
        public double Timestamp { get; set; }
        public string ImagePath { get; set; } = string.Empty;
        public string Caption => Number.ToString("00") + "  ·  " + Timestamp.ToString("0.00") + " s";
        public bool IsSelected
        {
            get => _isSelected;
            set { if (_isSelected != value) { _isSelected = value; OnPropertyChanged(); } }
        }
        public BitmapImage Thumbnail => _thumbnail ?? (_thumbnail = ImageLoading.LoadBitmap(ImagePath, 150));
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public sealed class ProcessedFrame
    {
        public int Number { get; set; }
        public double Timestamp { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public FrameBuffer Buffer { get; set; } = null!;
        public PixelBounds Bounds { get; set; }
        public double RootX { get; set; }
        public string Sha256 { get; set; } = string.Empty;
        public BitmapImage Preview => ImageLoading.LoadBitmap(FilePath, 320);
        public string Caption => Number.ToString("00") + " / 16";
    }

    public sealed class VideoInfo
    {
        public double Duration { get; set; }
        public double FramesPerSecond { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string Summary => Width + "×" + Height + "  ·  " + FramesPerSecond.ToString("0.##") + " FPS  ·  " + Duration.ToString("0.00") + " s";
    }

    public enum RegistrationMode
    {
        RootAndGround,
        GroundPreserveMotion,
        CameraLocked
    }

    public sealed class ProcessingOptions
    {
        public bool ChromaEnabled { get; set; } = true;
        public byte KeyR { get; set; }
        public byte KeyG { get; set; } = 255;
        public byte KeyB { get; set; }
        public double ChromaTolerance { get; set; } = 30;
        public double EdgeSoftness { get; set; } = 12;
        public double SpillSuppression { get; set; } = 0.65;
        public int HaloPixels { get; set; } = 1;
        public int IslandCleanupPixels { get; set; } = 24;
        public bool AlphaCutoffEnabled { get; set; } = true;
        public double AlphaCutoffPercent { get; set; } = 10;
        public double AlphaSoftnessPercent { get; set; } = 4;
        public int CanvasWidth { get; set; } = 256;
        public int CanvasHeight { get; set; } = 256;
        public int GroundY { get; set; } = 234;
        public int RootX { get; set; } = 128;
        public int Padding { get; set; } = 10;
        public RegistrationMode RegistrationMode { get; set; } = RegistrationMode.RootAndGround;
    }

    public readonly struct PixelBounds
    {
        public PixelBounds(int left, int top, int right, int bottom)
        {
            Left = left; Top = top; Right = right; Bottom = bottom;
        }
        public int Left { get; }
        public int Top { get; }
        public int Right { get; }
        public int Bottom { get; }
        public int Width => IsEmpty ? 0 : Right - Left + 1;
        public int Height => IsEmpty ? 0 : Bottom - Top + 1;
        public bool IsEmpty => Right < Left || Bottom < Top;
        public static PixelBounds Empty => new PixelBounds(0, 0, -1, -1);
    }

    public enum FindingLevel { Pass, Warning, Error, Info }

    public sealed class AuditFinding
    {
        public FindingLevel Level { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Symbol => Level == FindingLevel.Pass ? "✓" : Level == FindingLevel.Error ? "×" : Level == FindingLevel.Warning ? "!" : "i";
        public string Color => Level == FindingLevel.Pass ? "#287F76" : Level == FindingLevel.Error ? "#C73B2F" : Level == FindingLevel.Warning ? "#B87927" : "#6C665C";
    }

    public sealed class AuditReport
    {
        public List<AuditFinding> Findings { get; } = new List<AuditFinding>();
        public bool HasErrors => Findings.Exists(f => f.Level == FindingLevel.Error);
        public int UniqueFrames { get; set; }
        public double HeightDriftPercent { get; set; }
        public double LoopSeamRatio { get; set; }
    }

    public sealed class ExportOptions
    {
        public string BaseFolder { get; set; } = string.Empty;
        public string AnimationName { get; set; } = "nueva_animacion";
        public int Columns { get; set; } = 4;
        public double FramesPerSecond { get; set; } = 12;
        public string GodotTexturePath { get; set; } = "res://assets/sprites/generated/nueva_animacion_atlas.png";
    }

    public sealed class ExportResult
    {
        public string Folder { get; set; } = string.Empty;
        public string AtlasPath { get; set; } = string.Empty;
        public string ReviewPath { get; set; } = string.Empty;
    }
}

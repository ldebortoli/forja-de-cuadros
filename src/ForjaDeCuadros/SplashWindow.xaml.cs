using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace ForjaDeCuadros
{
    public partial class SplashWindow : Window
    {
        private bool _startupCompleted;
        private bool _cancellationRaised;

        public SplashWindow()
        {
            InitializeComponent();
            Closing += SplashWindow_Closing;
        }

        public event EventHandler? StartupCancellationRequested;

        public void CloseForStartup()
        {
            _startupCompleted = true;
            Close();
        }

        private void CloseSplash_Click(object sender, RoutedEventArgs e) => RequestCancellation();

        private void SplashWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape) return;
            e.Handled = true;
            RequestCancellation();
        }

        private void RequestCancellation()
        {
            RaiseCancellationOnce();
            Close();
        }

        private void SplashWindow_Closing(object? sender, CancelEventArgs e)
        {
            if (!_startupCompleted) RaiseCancellationOnce();
        }

        private void RaiseCancellationOnce()
        {
            if (_cancellationRaised) return;
            _cancellationRaised = true;
            StartupCancellationRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}

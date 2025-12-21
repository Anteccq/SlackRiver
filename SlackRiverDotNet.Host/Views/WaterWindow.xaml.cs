using System.ComponentModel;
using System.Windows;
using Microsoft.Extensions.Logging;
using R3;
using SlackRiverDotNet.Host.ViewModels;

namespace SlackRiverDotNet.Host.Views
{
    /// <summary>
    /// WaterWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class WaterWindow : Window
    {
        private readonly ILogger _logger;
        private readonly IDisposable _movement;
        private readonly WaterWindowViewModel _viewModel;

        public WaterWindow(WaterWindowViewModel viewModel, ILogger logger)
        {
            _viewModel = viewModel;
            _logger = logger;
            InitializeComponent();
            DataContext = viewModel;
            Left = SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth;
            Top = 10;
            Topmost = true;
            _movement = Observable
                .Interval(TimeSpan.FromMilliseconds(100))
                .ObserveOnCurrentSynchronizationContext()
                .TakeWhile(_ => Left > SystemParameters.VirtualScreenLeft)
                .Subscribe(_ => Left -= 50);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _logger.LogInformation($"GoodBye! {_viewModel.Content}");
            _movement.Dispose();
            base.OnClosing(e);
        }
    }
}

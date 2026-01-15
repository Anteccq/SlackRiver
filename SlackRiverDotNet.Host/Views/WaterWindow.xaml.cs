using System.ComponentModel;
using System.Windows;
using R3;
using SlackRiverDotNet.Host.ViewModels;

namespace SlackRiverDotNet.Host.Views
{
    /// <summary>
    /// WaterWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class WaterWindow : Window
    {
        private readonly IDisposable _movement;
        private static readonly Random Random = new();
        private const int ZonesNumber = 80;

        public WaterWindow(WaterWindowViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            Left = SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth;
            Top = Random.Next(ZonesNumber) * SystemParameters.PrimaryScreenHeight / ZonesNumber;
            Topmost = true;
            _movement = Observable
                .Interval(TimeSpan.FromMilliseconds(100))
                .ObserveOnCurrentSynchronizationContext()
                .TakeWhile(_ => Left > SystemParameters.VirtualScreenLeft - (Width+10))
                .Subscribe(_ => Left -= 50);

            LocationChanged += (_, _) =>
            {
                if (double.IsNaN(Width))
                    return;

                if (Left > SystemParameters.VirtualScreenLeft - Width)
                    return;

                Close();
            };
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _movement.Dispose();
            base.OnClosing(e);
        }
    }
}

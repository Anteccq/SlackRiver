using System.Diagnostics;
using R3;
using SlackRiverDotNet.Host.Models.Manager;
using System.Windows;

namespace SlackRiverDotNet.Host;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App(RiverManager river) : Application
{
    private IDisposable? _runWater;
    protected override void OnStartup(StartupEventArgs e)
    {
        WpfProviderInitializer.SetDefaultObservableSystem(ex => Trace.WriteLine($"R3 UnhandledException:{ex}"));
        _runWater = river.RunWater();
        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _runWater?.Dispose();
        base.OnExit(e);
    }
}

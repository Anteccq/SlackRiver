using R3;
using SlackRiverDotNet.Host.Models.Services;
using SlackRiverDotNet.Host.ViewModels;
using SlackRiverDotNet.Host.Views;
using System.Windows;
using Microsoft.Extensions.Logging;

namespace SlackRiverDotNet.Host.Models.Manager;

public sealed class RiverManager(IQuerySlackService service, ILogger<RiverManager> logger)
{
    private readonly HashSet<Task<Unit>> _windows = [];

    public IDisposable RunWater()
        => Observable
            .CreateFrom(x => service.GetMessagesAsync(DateTimeOffset.FromUnixTimeSeconds(0)))
            .Subscribe(messages =>
            {
                _windows.RemoveWhere(x => x.IsCompleted);
                foreach (var message in messages)
                {
                    _windows.Add(MakeWindow(message.Content));
                }
            });

    private Task<Unit> MakeWindow(string text)
    {
        var vm = new WaterWindowViewModel(text);
        var w = new WaterWindow(vm, logger);
        w.Show();
        return Observable
            .Interval(TimeSpan.FromSeconds(10))
            .ObserveOnCurrentSynchronizationContext()
            .SkipWhile(_ => w.Left > SystemParameters.VirtualScreenLeft)
            .Do(_ => w.Close())
            .FirstAsync();
    }
}
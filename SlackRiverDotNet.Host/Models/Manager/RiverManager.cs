using R3;
using SlackRiverDotNet.Host.Models.Services;
using SlackRiverDotNet.Host.ViewModels;
using SlackRiverDotNet.Host.Views;

namespace SlackRiverDotNet.Host.Models.Manager;

public sealed class RiverManager(IQuerySlackService service)
{
    public IDisposable RunWater()
        => Observable
            .CreateFrom(_ => service.GetMessagesAsync(DateTimeOffset.FromUnixTimeSeconds(0)))
            .Subscribe(m =>
            {
                var vm = new WaterWindowViewModel(m.Content);
                var w = new WaterWindow(vm);
                w.Show();
            });
}
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SlackRiverDotNet.Host.Models.Manager;
using SlackRiverDotNet.Host.Models.Services;
using SlackRiverDotNet.Host.Views;

namespace SlackRiverDotNet.Host;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(args);
        var configuration = builder.Configuration;

        configuration
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json");

        builder.Services.AddSingleton<App>();
        builder.Services.AddSingleton<RiverManager>();
        builder.Services.AddSingleton<IQuerySlackService, QuerySlackService>();
        builder.Services.AddHttpClient<IQuerySlackService, QuerySlackService>(x =>
        {
            x.BaseAddress = new Uri(configuration["SlackApiUrl"] ?? string.Empty);
            x.DefaultRequestHeaders.Add("Authorization", $"Bearer {configuration["SlackOAuthToken"]}");
        });

        builder.Services.Configure<SlackServiceOptions>(configuration.GetSection("SlackServiceOptions"));

        var host = builder.Build();

        var app = host.Services.GetRequiredService<App>();

        host.Start();

        app.Run(new FakeWindow());

        host.StopAsync().GetAwaiter().GetResult();
    }
}
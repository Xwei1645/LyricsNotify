using System.IO;
using ClassIsland.Core.Abstractions;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Extensions.Registry;
using ClassIsland.Shared.Helpers;
using LyricsNotify.Controls.Automation;
using LyricsNotify.Controls.NotificationProviders;
using LyricsNotify.Models;
using LyricsNotify.Services.Automation;
using LyricsNotify.Services.NotificationProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LyricsNotify;

[PluginEntrance]
public class Plugin : PluginBase
{
    [STAThread]
    public override void Initialize(HostBuilderContext context, IServiceCollection services)
    {
        services.AddNotificationProvider<LyricsNotificationProvider, LyricsNotificationProviderSettingsControl>();
        services.AddSingleton(s => s.GetServices<IHostedService>().OfType<LyricsNotificationProvider>().First());
        services.AddAction<ShowLyricsAction, ShowLyricsActionSettingsControl>();
    }
}
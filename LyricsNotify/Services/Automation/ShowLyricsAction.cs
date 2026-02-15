using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.Notification;
using ClassIsland.Core.Abstractions.Services;
using LyricsNotify.Models;
using LyricsNotify.Services.NotificationProviders;
using LyricsNotify.Helpers;

namespace LyricsNotify.Services.Automation;

[ActionInfo("lyricsnotify.show_notification", "显示歌词提醒", "\ue93c")]
public class ShowLyricsAction : ActionBase<ShowLyricsActionSettings>
{
    private LyricsNotificationProvider Provider { get; }

    public ShowLyricsAction(LyricsNotificationProvider provider)
    {
        Provider = provider;
    }

    protected override async Task OnInvoke()
    {
        await Provider.RunLyricsNotificationAsync(InterruptCancellationToken);
    }

    protected override async Task OnInterrupted()
    {
        await base.OnInterrupted();
    }
}

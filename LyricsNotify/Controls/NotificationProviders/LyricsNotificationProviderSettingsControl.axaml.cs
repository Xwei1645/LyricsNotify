using System;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Models.Notification;
using ClassIsland.Shared;
using LyricsNotify.Models;
using LyricsNotify.Services.NotificationProviders;
using LyricsNotify.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LyricsNotify.Controls.NotificationProviders;

public partial class LyricsNotificationProviderSettingsControl : NotificationProviderControlBase<LyricsNotificationSettings>
{
    private LyricsNotificationProvider Provider => IAppHost.GetService<LyricsNotificationProvider>();
    private ILogger<LyricsNotificationProviderSettingsControl> Logger => IAppHost.GetService<ILogger<LyricsNotificationProviderSettingsControl>>();

    public LyricsNotificationProviderSettingsControl()
    {
        InitializeComponent();
    }

    private void TestNotification_OnClick(object? sender, RoutedEventArgs e)
    {
        Logger.LogInformation("用户点击了测试提醒按钮。");
        _ = Provider.RunLyricsNotificationAsync();
    }

    private async void SelectAudio_OnClick(object? sender, RoutedEventArgs e)
    {
        Logger.LogInformation("用户点击了选择音频文件按钮。");
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择音频文件 (支持 MP3、WAV)",
            FileTypeFilter = new[] { new FilePickerFileType("音频文件") { Patterns = new[] { "*.mp3", "*.wav" } } },
            AllowMultiple = false
        });

        if (files.Count > 0)
        {
            var path = files[0].Path.LocalPath;
            Logger.LogInformation("音频文件已选择：{AudioPath}", path);
            Settings.AudioPath = path;
            try
            {
                Settings.AudioDuration = AudioHelper.GetDuration(path);
                Logger.LogInformation("音频时长获取成功：{Duration}", Settings.AudioDuration);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "获取所选音频文件的时长失败，使用默认值 0。");
                Settings.AudioDuration = TimeSpan.Zero;
            }
        }
        else
        {
            Logger.LogInformation("用户未选择任何音频文件。");
        }
    }

    private async void SelectLrc_OnClick(object? sender, RoutedEventArgs e)
    {
        Logger.LogInformation("用户点击了选择 LRC 歌词文件按钮。");
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择 LRC 歌词文件",
            FileTypeFilter = new[] { new FilePickerFileType("LRC 歌词") { Patterns = new[] { "*.lrc" } } },
            AllowMultiple = false
        });

        if (files.Count > 0)
        {
            Settings.LrcPath = files[0].Path.LocalPath;
            Logger.LogInformation("LRC 歌词文件已选择：{LrcPath}", Settings.LrcPath);
        }
        else
        {
            Logger.LogInformation("用户未选择任何 LRC 歌词文件。");
        }
    }
}

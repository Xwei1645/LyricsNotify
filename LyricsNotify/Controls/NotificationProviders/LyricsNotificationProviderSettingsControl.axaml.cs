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

namespace LyricsNotify.Controls.NotificationProviders;

public partial class LyricsNotificationProviderSettingsControl : NotificationProviderControlBase<LyricsNotificationSettings>
{
    private LyricsNotificationProvider Provider => IAppHost.GetService<LyricsNotificationProvider>();

    public LyricsNotificationProviderSettingsControl()
    {
        InitializeComponent();
    }

    private void TestNotification_OnClick(object? sender, RoutedEventArgs e)
    {
        _ = Provider.RunLyricsNotificationAsync();
    }

    private async void SelectAudio_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择音频文件",
            FileTypeFilter = new[] { new FilePickerFileType("音频文件") { Patterns = new[] { "*.mp3", "*.wav", "*.m4a" } } },
            AllowMultiple = false
        });

        if (files.Count > 0)
        {
            var path = files[0].Path.LocalPath;
            Settings.AudioPath = path;
            try
            {
                Settings.AudioDuration = AudioHelper.GetDuration(path);
            }
            catch
            {
                Settings.AudioDuration = TimeSpan.Zero;
            }
        }
    }

    private async void SelectLrc_OnClick(object? sender, RoutedEventArgs e)
    {
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
        }
    }
}

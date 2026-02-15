using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.NotificationProviders;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.Notification;
using ClassIsland.Core.Models.Notification.Templates;
using LyricsNotify.Models;
using LyricsNotify.Helpers;
using LyricsNotify.Controls.NotificationProviders;
using Microsoft.Extensions.Logging;

namespace LyricsNotify.Services.NotificationProviders;

[NotificationProviderInfo("C22EBC56-9EA5-4327-92B1-F21B159F0546", "歌词提醒", "\ue93c", "显示来自 LyricsNotify 的歌词提醒。点击以进行相关设置。")]
public class LyricsNotificationProvider : NotificationProviderBase<LyricsNotificationSettings>
{
    private ILogger<LyricsNotificationProvider> Logger { get; }
    private IAudioService AudioService { get; }

    public LyricsNotificationProvider(ILogger<LyricsNotificationProvider> logger, IAudioService audioService) : base()
    {
        Logger = logger;
        AudioService = audioService;
    }

    public async Task RunLyricsNotificationAsync(CancellationToken cancellationToken = default)
    {
        // 1. 安全检查
        if (Settings == null)
            return;

        Logger.LogInformation("开始播放歌词提醒。音频：{}，歌词：{}", Settings.AudioPath, Settings.LrcPath);

        // 2. 加载歌词
        var lyrics = new List<LrcLine>();
        if (!string.IsNullOrEmpty(Settings.LrcPath) && File.Exists(Settings.LrcPath))
        {
            try
            {
                var content = await File.ReadAllTextAsync(Settings.LrcPath, cancellationToken);
                lyrics = LrcParser.Parse(content);

                // 忽略空行歌词
                if (Settings.IsIgnoreEmptyLyrics)
                {
                    lyrics.RemoveAll(l => string.IsNullOrWhiteSpace(l.Content));
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "解析歌词文件失败。");
            }
        }

        // 3. 确定总显示时长 (歌曲时长)
        var totalDuration = Settings.AudioDuration;
        if (totalDuration <= TimeSpan.Zero && !string.IsNullOrEmpty(Settings.AudioPath) && File.Exists(Settings.AudioPath))
        {
            try
            {
                totalDuration = AudioHelper.GetDuration(Settings.AudioPath);
                Settings.AudioDuration = totalDuration;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "获取音频时长失败，正在使用备选方案。");
                totalDuration = lyrics.LastOrDefault()?.Time + TimeSpan.FromSeconds(3) ?? TimeSpan.FromSeconds(10);
            }
        }
        
        if (totalDuration <= TimeSpan.Zero)
        {
            totalDuration = TimeSpan.FromSeconds(10);
        }

        // 4. 计算遮罩和正文时长
        var maskDuration = Settings.MaskDuration;
        if (maskDuration >= totalDuration)
        {
            maskDuration = TimeSpan.FromSeconds(Math.Min(3, totalDuration.TotalSeconds / 2));
        }
        var overlayDuration = totalDuration - maskDuration;

        // 5. 构建提醒请求
        var initialText = lyrics.Count > 0 ? "准备播放..." : "正在播放歌词音频...";
        var lyricsControl = new LyricsNotificationControl 
        { 
            Text = initialText, 
            EnableAnimation = Settings.EnableAnimation 
        };
        var overlayContent = new NotificationContent(lyricsControl);

        var request = new NotificationRequest
        {
            MaskContent = NotificationContent.CreateTwoIconsMask(Settings.MaskText, "\ue93c"),
            OverlayContent = overlayContent
        };

        // 设置遮罩时长
        request.MaskContent.Duration = maskDuration;
        // 设置正文时长 (总时长减去遮罩时长)
        if (request.OverlayContent != null)
        {
            request.OverlayContent.Duration = overlayDuration;
        }

        // 6. 发送并等待提醒
        ShowNotification(request);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, request.CompletedToken);

        // 7. 播放音频
        if (!string.IsNullOrEmpty(Settings.AudioPath) && File.Exists(Settings.AudioPath))
        {
            try
            {
                _ = AudioService.PlayAudioAsync(File.OpenRead(Settings.AudioPath),
                    (float)Settings.Volume, cts.Token);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "播放音频失败。");
            }
        }

        try
        {
            var startTime = DateTime.Now;
            // 如果存在歌词，则进入更新循环
            if (lyrics.Count > 0)
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    var elapsed = DateTime.Now - startTime;
                    if (elapsed >= totalDuration) break;

                    var currentLine = lyrics.LastOrDefault(l => l.Time <= elapsed);
                    if (currentLine != null && lyricsControl.Text != currentLine.Content)
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            lyricsControl.Text = currentLine.Content;
                        });
                    }

                    await Task.Delay(100, cts.Token);
                }
            }
            else
            {
                await Task.Delay(totalDuration, cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            request.Cancel();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "歌词播放循环发生异常。");
            request.Cancel();
        }
    }
}
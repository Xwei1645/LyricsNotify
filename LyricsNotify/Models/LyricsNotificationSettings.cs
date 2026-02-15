using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LyricsNotify.Models;

public partial class LyricsNotificationSettings : ObservableRecipient
{
    [ObservableProperty]
    private string _maskText = "歌词";

    [ObservableProperty]
    private string? _audioPath;

    [ObservableProperty]
    private string? _lrcPath;

    [ObservableProperty]
    private TimeSpan _audioDuration;

    [ObservableProperty]
    private TimeSpan _maskDuration = TimeSpan.FromSeconds(3);

    [ObservableProperty]
    private bool _isIgnoreEmptyLyrics = true;

    [ObservableProperty]
    private bool _enableAnimation = true;

    [ObservableProperty]
    private double _volume = 1.0;
}

using Avalonia.Controls;

namespace LyricsNotify.Controls.NotificationProviders;

public partial class LyricsNotificationControl : UserControl
{
    private string _text = string.Empty;
    private int _currentIndex = 0;

    public LyricsNotificationControl()
    {
        InitializeComponent();
        MainListBox.SelectedIndex = 0;
    }

    public string Text
    {
        get => _text;
        set
        {
            if (_text == value) return;
            _text = value;
            UpdateText(value);
        }
    }

    public bool EnableAnimation { get; set; } = true;

    private void UpdateText(string text)
    {
        if (!EnableAnimation)
        {
            Text1.Text = text;
            Text2.Text = text;
            MainListBox.SelectedIndex = 0;
            return;
        }

        var nextIndex = (_currentIndex + 1) % 2;
        if (nextIndex == 0)
        {
            Text1.Text = text;
        }
        else
        {
            Text2.Text = text;
        }
        _currentIndex = nextIndex;
        MainListBox.SelectedIndex = _currentIndex;
    }
}

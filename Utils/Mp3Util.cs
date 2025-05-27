using System.Windows.Media;

namespace CloudreveDesktop.utils;

public static class Mp3Util
{
    private static readonly MediaPlayer Player = new();

    public static void Play(string name)
    {
        var uri = new Uri(App.FullPath + $@"Resources\Sound\{name}.mp3");
        Player.Open(uri);
        Player.Volume = 1.0;
        Player.Play();
    }
}
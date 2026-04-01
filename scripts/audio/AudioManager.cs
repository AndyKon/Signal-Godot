using Godot;

namespace Signal.Audio;

public partial class AudioManager : Node
{
    public static AudioManager Instance { get; private set; }

    private AudioStreamPlayer _musicPlayer;
    private AudioStreamPlayer _ambiencePlayer;
    private AudioStreamPlayer _sfxPlayer;
    private float _crossfadeDuration = 1.5f;

    public override void _Ready()
    {
        Instance = this;

        _musicPlayer = new AudioStreamPlayer();
        _musicPlayer.Bus = "Music";
        AddChild(_musicPlayer);

        _ambiencePlayer = new AudioStreamPlayer();
        _ambiencePlayer.Bus = "Music";
        AddChild(_ambiencePlayer);

        _sfxPlayer = new AudioStreamPlayer();
        _sfxPlayer.Bus = "Music";
        AddChild(_sfxPlayer);
    }

    public async void PlayMusic(AudioStream clip)
    {
        if (_musicPlayer.Stream == clip && _musicPlayer.Playing) return;
        await CrossfadeTo(_musicPlayer, clip);
    }

    public async void PlayAmbience(AudioStream clip)
    {
        if (_ambiencePlayer.Stream == clip && _ambiencePlayer.Playing) return;
        await CrossfadeTo(_ambiencePlayer, clip);
    }

    public void PlaySFX(AudioStream clip)
    {
        _sfxPlayer.Stream = clip;
        _sfxPlayer.Play();
    }

    public void StopMusic() => FadeOut(_musicPlayer);
    public void StopAmbience() => FadeOut(_ambiencePlayer);

    private async System.Threading.Tasks.Task CrossfadeTo(AudioStreamPlayer player, AudioStream newClip)
    {
        if (player.Playing)
        {
            var tween = CreateTween();
            tween.TweenProperty(player, "volume_db", -80.0f, _crossfadeDuration);
            await ToSignal(tween, Tween.SignalName.Finished);
            player.Stop();
        }

        player.Stream = newClip;
        player.VolumeDb = -80.0f;
        player.Play();

        var fadeIn = CreateTween();
        fadeIn.TweenProperty(player, "volume_db", 0.0f, _crossfadeDuration);
        await ToSignal(fadeIn, Tween.SignalName.Finished);
    }

    private async void FadeOut(AudioStreamPlayer player)
    {
        if (!player.Playing) return;
        var tween = CreateTween();
        tween.TweenProperty(player, "volume_db", -80.0f, _crossfadeDuration);
        await ToSignal(tween, Tween.SignalName.Finished);
        player.Stop();
    }
}

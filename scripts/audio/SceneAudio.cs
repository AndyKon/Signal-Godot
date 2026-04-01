using Godot;
using Signal.Audio;
using Signal.Core;

namespace Signal.Audio;

public partial class SceneAudio : Node
{
    [Export] public AudioStream AmbienceClip { get; set; }
    [Export] public AudioStream MusicClip { get; set; }

    public override void _Ready()
    {
        if (AudioManager.Instance == null) return;

        if (AmbienceClip != null)
            AudioManager.Instance.PlayAmbience(AmbienceClip);

        if (MusicClip != null)
            AudioManager.Instance.PlayMusic(MusicClip);

        GameLog.Event("Audio", $"SceneAudio: ambience={AmbienceClip != null}, music={MusicClip != null}");
    }
}

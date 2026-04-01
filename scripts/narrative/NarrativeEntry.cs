using Godot;

namespace Signal.Narrative;

[GlobalClass]
public partial class NarrativeEntry : Resource
{
    [Export] public string EntryId { get; set; } = "";

    [ExportGroup("Default Content")]
    [Export(PropertyHint.MultilineText)] public string Text { get; set; } = "";
    [Export] public AudioStream VoiceClip { get; set; }

    [ExportGroup("Alternative Content")]
    [Export] public string AltConditionFlag { get; set; } = "";
    [Export(PropertyHint.MultilineText)] public string AltText { get; set; } = "";
    [Export] public AudioStream AltVoiceClip { get; set; }

    [ExportGroup("Effects")]
    [Export] public string FlagToSet { get; set; } = "";
}

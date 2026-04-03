using Godot;

namespace Signal.Interaction;

public enum HotspotType
{
    Examine,
    PickUp,
    Door,
    Terminal,
    Narration
}

[GlobalClass]
public partial class HotspotData : Resource
{
    [Export] public HotspotType Type { get; set; }
    [Export(PropertyHint.MultilineText)] public string ExamineText { get; set; } = "";
    [Export] public string ItemToGrant { get; set; } = "";
    [Export] public string ItemToConsume { get; set; } = "";
    [Export] public string FlagToSet { get; set; } = "";
    [Export] public string EvidenceToDiscover { get; set; } = "";
    [Export] public string TargetScene { get; set; } = "";
    [Export] public bool IsNewSection { get; set; }
    [Export] public string NarrativeEntryId { get; set; } = "";
}

[GlobalClass]
public partial class HotspotCondition : Resource
{
    [Export] public string RequiredFlag { get; set; } = "";
    [Export] public string RequiredItem { get; set; } = "";
    [Export] public string BlockedByFlag { get; set; } = "";
}

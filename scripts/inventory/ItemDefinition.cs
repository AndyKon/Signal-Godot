using Godot;

namespace Signal.Inventory;

[GlobalClass]
public partial class ItemDefinition : Resource
{
    [Export] public string ItemId { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "";
    [Export(PropertyHint.MultilineText)] public string Description { get; set; } = "";
    [Export] public Texture2D Icon { get; set; }
}

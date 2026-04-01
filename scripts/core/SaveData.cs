using System;
using System.Collections.Generic;

namespace Signal.Core;

[Serializable]
public class SaveData
{
    public List<string> Flags { get; set; } = new();
    public List<int> PoweredSections { get; set; } = new();
    public string CurrentScene { get; set; } = "";
    public List<string> InventoryItems { get; set; } = new();
    public int TotalOptionalFlags { get; set; }
}

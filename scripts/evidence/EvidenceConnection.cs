namespace Signal.Evidence;

public class EvidenceConnection
{
    public string EntryAId { get; }
    public string EntryBId { get; }
    public string EchoReaction { get; }
    public string FlagToSet { get; }

    /// <summary>Stable key for tracking fired state. Always alphabetically sorted.</summary>
    public string Key => string.CompareOrdinal(EntryAId, EntryBId) <= 0
        ? $"{EntryAId}:{EntryBId}"
        : $"{EntryBId}:{EntryAId}";

    public EvidenceConnection(string entryAId, string entryBId, string echoReaction,
                               string flagToSet = null)
    {
        EntryAId = entryAId;
        EntryBId = entryBId;
        EchoReaction = echoReaction;
        FlagToSet = flagToSet;
    }
}

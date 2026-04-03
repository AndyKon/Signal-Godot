namespace Signal.Evidence;

public enum EvidenceType
{
    TerminalLog,
    Environmental,
    SensorData,
    CrewDialogue
}

public enum EvidenceGroup
{
    PlatformSystems,
    Personnel,
    Operations,
    NEREUS,
    Timeline,
    ECHO,
    External
}

public class EvidenceEntry
{
    public string Id { get; }
    public string Title { get; }
    public string Body { get; }
    public string Source { get; }
    public EvidenceType Type { get; }
    public EvidenceGroup Group { get; }

    public EvidenceEntry(string id, string title, string body, string source,
                         EvidenceType type, EvidenceGroup group)
    {
        Id = id;
        Title = title;
        Body = body;
        Source = source;
        Type = type;
        Group = group;
    }
}

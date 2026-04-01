namespace Signal.Core;

public enum Ending
{
    Escape,
    Truth,
    Confrontation
}

public static class EndingEvaluator
{
    private const float TruthThreshold = 0.6f;
    private const float ConfrontationThreshold = 0.9f;
    private const string MirrorFlag = "saw_reflection";

    public static Ending Evaluate(GameState state)
    {
        float ratio = state.FlagRatio;

        if (ratio >= ConfrontationThreshold && state.HasFlag(MirrorFlag))
            return Ending.Confrontation;

        if (ratio >= TruthThreshold)
            return Ending.Truth;

        return Ending.Escape;
    }
}

using System;
using System.Collections.Generic;

namespace Signal.Minigame;

/// <summary>
/// Core logic for the Mastermind-style decryption puzzle.
/// Pure C# — no Godot dependencies. Testable independently.
/// </summary>
public class DecryptionPuzzle
{
    public int SlotCount { get; }
    public int ValueCount { get; }
    public bool AllowRepeats { get; }
    public int LiesPerRound { get; }
    public float ReplayLieChance { get; }
    public int MaxReplayLiesPerCycle { get; }

    private readonly int[] _answer;
    private readonly Random _rng;
    private readonly List<GuessResult> _history = new();

    private bool _solved;

    public bool IsSolved => _solved;
    public int GuessesMade => _history.Count;
    public IReadOnlyList<GuessResult> History => _history;
    public int[] Answer => _solved ? (int[])_answer.Clone() : null; // Only reveal after solved

    public DecryptionPuzzle(int slots, int values, bool allowRepeats, int liesPerRound,
                            float replayLieChance, int maxReplayLiesPerCycle, int seed)
    {
        SlotCount = slots;
        ValueCount = values;
        AllowRepeats = allowRepeats;
        LiesPerRound = liesPerRound;
        ReplayLieChance = replayLieChance;
        MaxReplayLiesPerCycle = maxReplayLiesPerCycle;

        _rng = new Random(seed);
        _answer = GenerateAnswer();
    }

    private int[] GenerateAnswer()
    {
        var answer = new int[SlotCount];
        if (AllowRepeats)
        {
            for (int i = 0; i < SlotCount; i++)
                answer[i] = _rng.Next(ValueCount);
        }
        else
        {
            // Fisher-Yates shuffle to pick unique values
            var pool = new List<int>();
            for (int i = 0; i < ValueCount; i++) pool.Add(i);
            for (int i = 0; i < SlotCount; i++)
            {
                int idx = _rng.Next(pool.Count);
                answer[i] = pool[idx];
                pool.RemoveAt(idx);
            }
        }
        return answer;
    }

    /// <summary>
    /// Submit a guess. Returns the result with feedback per slot.
    /// Returns null only if already solved or guess has wrong length.
    /// No guessing limits — the player can always keep guessing.
    /// </summary>
    public GuessResult SubmitGuess(int[] guess)
    {
        if (_solved || guess.Length != SlotCount)
            return null;

        // Calculate true feedback
        var feedback = new SlotFeedback[SlotCount];
        var answerUsed = new bool[SlotCount];
        var guessUsed = new bool[SlotCount];

        // Pass 1: find exact matches (green)
        for (int i = 0; i < SlotCount; i++)
        {
            if (guess[i] == _answer[i])
            {
                feedback[i] = SlotFeedback.Correct;
                answerUsed[i] = true;
                guessUsed[i] = true;
            }
        }

        // Pass 2: find value-present-wrong-position (yellow)
        for (int i = 0; i < SlotCount; i++)
        {
            if (guessUsed[i]) continue;
            for (int j = 0; j < SlotCount; j++)
            {
                if (answerUsed[j]) continue;
                if (guess[i] == _answer[j])
                {
                    feedback[i] = SlotFeedback.WrongPosition;
                    answerUsed[j] = true;
                    guessUsed[i] = true;
                    break;
                }
            }
        }

        // Pass 3: remaining are not in sequence (red)
        for (int i = 0; i < SlotCount; i++)
        {
            if (!guessUsed[i])
                feedback[i] = SlotFeedback.NotPresent;
        }

        // Apply NEREUS lies
        var displayFeedback = (SlotFeedback[])feedback.Clone();
        var liedSlots = new bool[SlotCount];

        if (LiesPerRound > 0)
        {
            var candidateSlots = new List<int>();
            for (int i = 0; i < SlotCount; i++) candidateSlots.Add(i);

            int liesToApply = Math.Min(LiesPerRound, SlotCount);
            for (int lie = 0; lie < liesToApply; lie++)
            {
                if (candidateSlots.Count == 0) break;
                int idx = _rng.Next(candidateSlots.Count);
                int slot = candidateSlots[idx];
                candidateSlots.RemoveAt(idx);

                // Invert the feedback
                displayFeedback[slot] = InvertFeedback(feedback[slot]);
                liedSlots[slot] = true;
            }
        }

        // Check if solved
        bool allCorrect = true;
        for (int i = 0; i < SlotCount; i++)
        {
            if (feedback[i] != SlotFeedback.Correct)
            {
                allCorrect = false;
                break;
            }
        }
        _solved = allCorrect;

        var result = new GuessResult
        {
            Guess = (int[])guess.Clone(),
            TrueFeedback = feedback,
            DisplayFeedback = displayFeedback,
            LiedSlots = liedSlots,
            IsSolution = allCorrect
        };

        _history.Add(result);
        return result;
    }

    /// <summary>
    /// Prepare replay data for all historical guesses. For each entry, may alter one
    /// feedback slot based on ReplayLieChance and MaxReplayLiesPerCycle — either
    /// fixing a prior lie or introducing a new one.
    /// </summary>
    public ReplayResult[] PrepareReplay()
    {
        var results = new ReplayResult[_history.Count];
        int liesThisCycle = 0;

        for (int h = 0; h < _history.Count; h++)
        {
            var entry = _history[h];
            var displayFeedback = (SlotFeedback[])entry.DisplayFeedback.Clone();
            var alteredSlots = new bool[SlotCount];

            bool shouldAlter = liesThisCycle < MaxReplayLiesPerCycle
                               && (float)_rng.NextDouble() < ReplayLieChance;

            if (shouldAlter)
            {
                int slot = _rng.Next(SlotCount);
                displayFeedback[slot] = InvertFeedback(displayFeedback[slot]);
                alteredSlots[slot] = true;
                liesThisCycle++;
            }

            results[h] = new ReplayResult
            {
                DisplayFeedback = displayFeedback,
                AlteredSlots = alteredSlots
            };
        }

        return results;
    }

    private SlotFeedback InvertFeedback(SlotFeedback real)
    {
        // Rotate: Correct → NotPresent, WrongPosition → Correct, NotPresent → WrongPosition
        return real switch
        {
            SlotFeedback.Correct => SlotFeedback.NotPresent,
            SlotFeedback.WrongPosition => SlotFeedback.Correct,
            SlotFeedback.NotPresent => SlotFeedback.WrongPosition,
            _ => real
        };
    }

    // --- Factory methods ---

    public static DecryptionPuzzle CreateSection1(int seed) =>
        new(slots: 4, values: 6, allowRepeats: false, liesPerRound: 0,
            replayLieChance: 0f, maxReplayLiesPerCycle: 0, seed: seed);

    public static DecryptionPuzzle CreateSection2(int seed) =>
        new(slots: 4, values: 6, allowRepeats: true, liesPerRound: 0,
            replayLieChance: 0f, maxReplayLiesPerCycle: 0, seed: seed);

    public static DecryptionPuzzle CreateSection3(int seed) =>
        new(slots: 5, values: 8, allowRepeats: true, liesPerRound: 1,
            replayLieChance: 0.3f, maxReplayLiesPerCycle: 1, seed: seed);

    public static DecryptionPuzzle CreateSection4(int seed) =>
        new(slots: 5, values: 8, allowRepeats: true, liesPerRound: 1,
            replayLieChance: 0.6f, maxReplayLiesPerCycle: 1, seed: seed);

    public static DecryptionPuzzle CreateSection5Hostile(int seed) =>
        new(slots: 6, values: 8, allowRepeats: true, liesPerRound: 2,
            replayLieChance: 0.8f, maxReplayLiesPerCycle: 2, seed: seed);

    public static DecryptionPuzzle CreateSection5Cooperative(int seed) =>
        new(slots: 4, values: 6, allowRepeats: false, liesPerRound: 0,
            replayLieChance: 0f, maxReplayLiesPerCycle: 0, seed: seed);
}

public enum SlotFeedback
{
    Correct,       // Green — right value, right position
    WrongPosition, // Yellow — value exists but in different position
    NotPresent     // Red — value not in the sequence
}

public class GuessResult
{
    public int[] Guess;
    public SlotFeedback[] TrueFeedback;    // What the real answer is
    public SlotFeedback[] DisplayFeedback; // What NEREUS shows (may include lies)
    public bool[] LiedSlots;               // Which slots NEREUS lied about
    public bool IsSolution;
}

public class ReplayResult
{
    public SlotFeedback[] DisplayFeedback;
    public bool[] AlteredSlots;
}

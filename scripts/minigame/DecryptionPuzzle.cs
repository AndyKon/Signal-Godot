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
    public int GuessesPerBatch { get; }
    public float CooldownSeconds { get; }

    private readonly int[] _answer;
    private readonly Random _rng;
    private readonly List<GuessResult> _history = new();

    private int _guessesRemaining;
    private bool _solved;

    public bool IsSolved => _solved;
    public int GuessesMade => _history.Count;
    public int GuessesRemaining => _guessesRemaining;
    public IReadOnlyList<GuessResult> History => _history;
    public int[] Answer => _solved ? (int[])_answer.Clone() : null; // Only reveal after solved

    public DecryptionPuzzle(int slots, int values, bool allowRepeats, int liesPerRound,
                            int guessesPerBatch, float cooldownSeconds, int seed)
    {
        SlotCount = slots;
        ValueCount = values;
        AllowRepeats = allowRepeats;
        LiesPerRound = liesPerRound;
        GuessesPerBatch = guessesPerBatch;
        CooldownSeconds = cooldownSeconds;

        _rng = new Random(seed);
        _answer = GenerateAnswer();
        _guessesRemaining = guessesPerBatch;
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
    /// Returns null if no guesses remaining (need cooldown reset).
    /// </summary>
    public GuessResult SubmitGuess(int[] guess)
    {
        if (_solved || _guessesRemaining <= 0 || guess.Length != SlotCount)
            return null;

        _guessesRemaining--;

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
    /// Reset the guess counter after a cooldown period.
    /// </summary>
    public void ResetGuesses()
    {
        _guessesRemaining = GuessesPerBatch;
    }

    public bool NeedsCooldown => _guessesRemaining <= 0 && !_solved;

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
            guessesPerBatch: 5, cooldownSeconds: 0, seed: seed);

    public static DecryptionPuzzle CreateSection2(int seed) =>
        new(slots: 4, values: 6, allowRepeats: true, liesPerRound: 0,
            guessesPerBatch: 5, cooldownSeconds: 0, seed: seed);

    public static DecryptionPuzzle CreateSection3(int seed) =>
        new(slots: 5, values: 8, allowRepeats: true, liesPerRound: 1,
            guessesPerBatch: 4, cooldownSeconds: 12, seed: seed);

    public static DecryptionPuzzle CreateSection4(int seed) =>
        new(slots: 5, values: 8, allowRepeats: true, liesPerRound: 1,
            guessesPerBatch: 4, cooldownSeconds: 12, seed: seed);

    public static DecryptionPuzzle CreateSection5Hostile(int seed) =>
        new(slots: 6, values: 10, allowRepeats: true, liesPerRound: 2,
            guessesPerBatch: 3, cooldownSeconds: 15, seed: seed);

    public static DecryptionPuzzle CreateSection5Cooperative(int seed) =>
        new(slots: 4, values: 6, allowRepeats: false, liesPerRound: 0,
            guessesPerBatch: 5, cooldownSeconds: 0, seed: seed);
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

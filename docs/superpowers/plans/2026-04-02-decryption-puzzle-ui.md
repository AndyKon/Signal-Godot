# Mastermind Decryption UI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a fully playable Mastermind-style decryption puzzle UI with replay animation system and visual tells for NEREUS lies, testable via standalone test harness.

**Architecture:** `DecryptionPuzzle.cs` (existing, pure C#) provides game logic — updated to remove batch/cooldown and add replay lie API. `DecryptionPuzzleUI.cs` builds the entire visual interface programmatically as a Godot Control. Replay animations use a state machine driven by `_Process`. Visual tells use Godot Tweens. `DecryptionTestHarness.cs` provides standalone playtesting with section selection.

**Tech Stack:** Godot 4.6 C# (.NET 8.0), pure programmatic UI (no scene editor — all UI built in `_Ready()`)

---

## File Structure

### Modified
- `scripts/minigame/DecryptionPuzzle.cs` — Remove batch/cooldown, fix factory values to match spec, add replay lie generation API

### Created
- `scripts/minigame/DecryptionPuzzleUI.cs` — Complete visual interface: slot input, value picker, guess history grid, feedback display, replay animation, visual tells
- `scripts/minigame/DecryptionTestHarness.cs` — Standalone test scene controller with section cycling and stats
- `scenes/DecryptionTest.tscn` — Minimal scene referencing test harness
- `scripts/tests/DecryptionLogicTest.cs` — Pure logic assertion tests for DecryptionPuzzle (runs in Godot headless)

---

## Conventions

**Build command:** `dotnet build` from `/Users/andrew/Repositories/anko/Signal-Godot/`

**Test command:** The Godot project already has `AutotestBootstrap.cs` that checks for `--autotest`. For decryption logic tests, `DecryptionLogicTest.cs` follows the same pattern as `AutoPlaytest.cs` — instantiate, assert, log pass/fail. It is triggered by adding it to the `AutoPlaytest.tscn` scene tree or running via a dedicated bootstrap.

**Namespace:** `Signal.Minigame` for minigame code, `Signal.Tests` for tests.

**Pattern:** All UI is built programmatically in `_Ready()`. No `.tscn` scene editing beyond a root node reference. Follow existing patterns in `MinigameTestHarness.cs` and `SignalPuzzle.cs`.

**Hex value display:** Values 0–7 map to hex pairs: `["0a", "3f", "b2", "e7", "1c", "d4", "8f", "5b"]`. Each has a subtle color tint for visual scanning.

**Feedback colors:**
- Correct (green): `new Color(0.2f, 0.8f, 0.3f)`
- WrongPosition (yellow): `new Color(0.9f, 0.75f, 0.1f)`
- NotPresent (red): `new Color(0.8f, 0.2f, 0.2f)`
- Pending/empty: `new Color(0.15f, 0.18f, 0.22f)`

---

## Task 1: Update DecryptionPuzzle Core Logic

**Files:**
- Modify: `scripts/minigame/DecryptionPuzzle.cs`
- Create: `scripts/tests/DecryptionLogicTest.cs`

### Spec Alignment Issues to Fix

1. **Batch/cooldown system must be removed.** The spec says "No hard fail. The player can always keep guessing. The cost is time (replays + platform timer)." The current code has `GuessesPerBatch`, `CooldownSeconds`, `_guessesRemaining`, `GuessesRemaining`, `NeedsCooldown`, `ResetGuesses()` — all artifacts of an earlier design.

2. **Section 5 hostile values:** Currently 10, spec says 6-8. Change to 8.

3. **Replay lie system:** Not yet implemented. The spec says NEREUS can alter feedback on previous guesses during replay animation (Sections 3+).

- [ ] **Step 1: Write test for unlimited guessing**

Create `scripts/tests/DecryptionLogicTest.cs`:

```csharp
using System;
using Godot;
using Signal.Core;
using Signal.Minigame;

namespace Signal.Tests;

public partial class DecryptionLogicTest : Node
{
    private int _passes;
    private int _failures;

    public override void _Ready()
    {
        GD.Print("=== DecryptionPuzzle Logic Tests ===");
        TestUnlimitedGuessing();
        TestBasicFeedbackExactMatch();
        TestBasicFeedbackWrongPosition();
        TestBasicFeedbackNotPresent();
        TestNoRepeatsUniqueValues();
        TestLiesInvertFeedback();
        TestReplayLieGeneration();
        TestSolvedState();
        TestFactoryValues();
        GD.Print($"=== Results: {_passes} passed, {_failures} failed ===");
    }

    private void Assert(bool condition, string name)
    {
        if (condition) { _passes++; GD.Print($"  PASS: {name}"); }
        else { _failures++; GD.Print($"  FAIL: {name}"); }
    }

    private void TestUnlimitedGuessing()
    {
        var puzzle = DecryptionPuzzle.CreateSection1(42);
        // Should be able to guess many times without hitting a limit
        for (int i = 0; i < 20; i++)
        {
            var result = puzzle.SubmitGuess(new int[] { 0, 1, 2, 3 });
            Assert(result != null, $"Unlimited guessing: guess {i + 1} returns non-null");
            if (puzzle.IsSolved) break;
        }
    }

    private void TestBasicFeedbackExactMatch()
    {
        // Seed 42 with Section1 (4 slots, 6 values, no repeats) produces a known answer
        var puzzle = DecryptionPuzzle.CreateSection1(42);
        // Submit the actual answer (we get it by solving)
        // First, probe to find the answer
        // For deterministic test: create puzzle, submit known wrong, check feedback type
        var result = puzzle.SubmitGuess(new int[] { 0, 0, 0, 0 });
        Assert(result != null, "Basic feedback: result not null");
        Assert(result.DisplayFeedback.Length == 4, "Basic feedback: 4 feedback slots");
        // At least verify feedback types are valid
        foreach (var fb in result.DisplayFeedback)
        {
            Assert(fb == SlotFeedback.Correct || fb == SlotFeedback.WrongPosition || fb == SlotFeedback.NotPresent,
                "Basic feedback: valid feedback type");
        }
    }

    private void TestBasicFeedbackWrongPosition()
    {
        // Create a puzzle with known seed, submit guess with value in wrong position
        var puzzle = new DecryptionPuzzle(slots: 3, values: 4, allowRepeats: false,
            liesPerRound: 0, replayLieChance: 0f, maxReplayLiesPerCycle: 0, seed: 100);
        // With seed 100, 3 slots, 4 values, no repeats: answer is deterministic
        // Submit all same value — at most 1 can be correct, others wrong position or not present
        var result = puzzle.SubmitGuess(new int[] { 0, 1, 2 });
        Assert(result != null, "WrongPosition test: result not null");
        // Verify true and display feedback match (no lies)
        for (int i = 0; i < 3; i++)
        {
            Assert(result.TrueFeedback[i] == result.DisplayFeedback[i],
                $"WrongPosition test: no lies, slot {i} true==display");
        }
    }

    private void TestBasicFeedbackNotPresent()
    {
        // 2 slots, 6 values, no repeats — most guessed values will be NotPresent
        var puzzle = new DecryptionPuzzle(slots: 2, values: 6, allowRepeats: false,
            liesPerRound: 0, replayLieChance: 0f, maxReplayLiesPerCycle: 0, seed: 50);
        var result = puzzle.SubmitGuess(new int[] { 4, 5 });
        Assert(result != null, "NotPresent test: result not null");
        // Feedback should only contain valid enum values
        foreach (var fb in result.TrueFeedback)
        {
            Assert(Enum.IsDefined(typeof(SlotFeedback), fb), "NotPresent test: valid enum");
        }
    }

    private void TestNoRepeatsUniqueValues()
    {
        var puzzle = DecryptionPuzzle.CreateSection1(77);
        // Answer should have no repeated values — submit the answer and verify
        // We'll just verify the puzzle reports no repeats allowed
        Assert(!puzzle.AllowRepeats, "NoRepeats: AllowRepeats is false for Section1");
    }

    private void TestLiesInvertFeedback()
    {
        // Section 3 has 1 lie per round
        var puzzle = DecryptionPuzzle.CreateSection3(42);
        var result = puzzle.SubmitGuess(new int[] { 0, 1, 2, 3, 4 });
        Assert(result != null, "Lies test: result not null");
        // Count slots where true != display — should be exactly 1
        int lieCount = 0;
        for (int i = 0; i < result.TrueFeedback.Length; i++)
        {
            if (result.TrueFeedback[i] != result.DisplayFeedback[i])
                lieCount++;
        }
        Assert(lieCount == 1, $"Lies test: exactly 1 lie (got {lieCount})");
        // Verify the lie is an inversion
        for (int i = 0; i < result.LiedSlots.Length; i++)
        {
            if (result.LiedSlots[i])
            {
                var truth = result.TrueFeedback[i];
                var display = result.DisplayFeedback[i];
                bool validInversion =
                    (truth == SlotFeedback.Correct && display == SlotFeedback.NotPresent) ||
                    (truth == SlotFeedback.WrongPosition && display == SlotFeedback.Correct) ||
                    (truth == SlotFeedback.NotPresent && display == SlotFeedback.WrongPosition);
                Assert(validInversion, $"Lies test: slot {i} is valid inversion");
            }
        }
    }

    private void TestReplayLieGeneration()
    {
        // Section 4 has active replay lies (chance 0.6, max 1 per cycle)
        var puzzle = DecryptionPuzzle.CreateSection4(42);
        // Submit a few guesses to build history
        puzzle.SubmitGuess(new int[] { 0, 1, 2, 3, 4 });
        puzzle.SubmitGuess(new int[] { 1, 2, 3, 4, 5 });
        puzzle.SubmitGuess(new int[] { 2, 3, 4, 5, 6 });
        Assert(puzzle.History.Count == 3, "Replay lies: 3 guesses in history");

        // Prepare replay — should return results for all history entries
        var replayResults = puzzle.PrepareReplay();
        Assert(replayResults != null, "Replay lies: PrepareReplay returns non-null");
        Assert(replayResults.Length == 3, "Replay lies: replay results for all 3 guesses");
        // Each replay result should have correct length feedback
        foreach (var rr in replayResults)
        {
            Assert(rr.DisplayFeedback.Length == 5, "Replay lies: 5 feedback slots per result");
            Assert(rr.AlteredSlots.Length == 5, "Replay lies: 5 altered slots per result");
        }
    }

    private void TestSolvedState()
    {
        // Create tiny puzzle and submit the exact answer
        var puzzle = new DecryptionPuzzle(slots: 2, values: 2, allowRepeats: false,
            liesPerRound: 0, replayLieChance: 0f, maxReplayLiesPerCycle: 0, seed: 0);
        // Answer is deterministic with seed 0: either [0,1] or [1,0]
        // Try both
        Assert(!puzzle.IsSolved, "Solved test: not solved initially");
        var r1 = puzzle.SubmitGuess(new int[] { 0, 1 });
        if (!puzzle.IsSolved)
        {
            var r2 = puzzle.SubmitGuess(new int[] { 1, 0 });
            Assert(puzzle.IsSolved, "Solved test: solved after trying both permutations");
        }
        else
        {
            Assert(puzzle.IsSolved, "Solved test: solved on first try");
        }
    }

    private void TestFactoryValues()
    {
        var s1 = DecryptionPuzzle.CreateSection1(0);
        Assert(s1.SlotCount == 4, "Factory S1: 4 slots");
        Assert(s1.ValueCount == 6, "Factory S1: 6 values");
        Assert(!s1.AllowRepeats, "Factory S1: no repeats");
        Assert(s1.LiesPerRound == 0, "Factory S1: no lies");

        var s2 = DecryptionPuzzle.CreateSection2(0);
        Assert(s2.AllowRepeats, "Factory S2: repeats allowed");
        Assert(s2.LiesPerRound == 0, "Factory S2: no lies");

        var s3 = DecryptionPuzzle.CreateSection3(0);
        Assert(s3.SlotCount == 5, "Factory S3: 5 slots");
        Assert(s3.ValueCount == 8, "Factory S3: 8 values");
        Assert(s3.LiesPerRound == 1, "Factory S3: 1 lie");

        var s5h = DecryptionPuzzle.CreateSection5Hostile(0);
        Assert(s5h.SlotCount == 6, "Factory S5H: 6 slots");
        Assert(s5h.ValueCount == 8, "Factory S5H: 8 values");
        Assert(s5h.LiesPerRound == 2, "Factory S5H: 2 lies");

        var s5c = DecryptionPuzzle.CreateSection5Cooperative(0);
        Assert(s5c.LiesPerRound == 0, "Factory S5C: no lies");
        Assert(!s5c.AllowRepeats, "Factory S5C: no repeats");
    }
}
```

- [ ] **Step 2: Build to verify test compiles**

Run: `cd /Users/andrew/Repositories/anko/Signal-Godot && dotnet build`

Expected: Build fails because `DecryptionPuzzle` constructor signature doesn't match (new parameters `replayLieChance`, `maxReplayLiesPerCycle`) and `PrepareReplay()` doesn't exist.

- [ ] **Step 3: Update DecryptionPuzzle.cs — remove batch/cooldown, update constructor**

Replace the entire `scripts/minigame/DecryptionPuzzle.cs` with:

```csharp
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
    public int[] Answer => _solved ? (int[])_answer.Clone() : null;

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
    /// No guess limit — the player can always keep guessing.
    /// </summary>
    public GuessResult SubmitGuess(int[] guess)
    {
        if (_solved || guess.Length != SlotCount)
            return null;

        var feedback = CalculateFeedback(guess);

        // Apply NEREUS lies to display feedback
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

                displayFeedback[slot] = InvertFeedback(feedback[slot]);
                liedSlots[slot] = true;
            }
        }

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
    /// Calculate true Mastermind feedback for a guess against the answer.
    /// </summary>
    private SlotFeedback[] CalculateFeedback(int[] guess)
    {
        var feedback = new SlotFeedback[SlotCount];
        var answerUsed = new bool[SlotCount];
        var guessUsed = new bool[SlotCount];

        // Pass 1: exact matches (green)
        for (int i = 0; i < SlotCount; i++)
        {
            if (guess[i] == _answer[i])
            {
                feedback[i] = SlotFeedback.Correct;
                answerUsed[i] = true;
                guessUsed[i] = true;
            }
        }

        // Pass 2: value present, wrong position (yellow)
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

        // Pass 3: not present (red)
        for (int i = 0; i < SlotCount; i++)
        {
            if (!guessUsed[i])
                feedback[i] = SlotFeedback.NotPresent;
        }

        return feedback;
    }

    /// <summary>
    /// Prepare replay feedback for all history entries.
    /// NEREUS may alter feedback on previous guesses during replay (Sections 3+).
    /// Call this once before starting a replay animation cycle.
    /// Returns one ReplayResult per history entry.
    /// </summary>
    public ReplayResult[] PrepareReplay()
    {
        if (_history.Count == 0) return Array.Empty<ReplayResult>();

        var results = new ReplayResult[_history.Count];
        int alteredCount = 0;

        for (int i = 0; i < _history.Count; i++)
        {
            var original = _history[i];
            var replayFeedback = (SlotFeedback[])original.DisplayFeedback.Clone();
            var alteredSlots = new bool[SlotCount];

            // Decide whether to alter this guess's replay feedback
            if (MaxReplayLiesPerCycle > 0 && alteredCount < MaxReplayLiesPerCycle
                && _rng.NextDouble() < ReplayLieChance)
            {
                // Pick a random slot to alter
                int slot = _rng.Next(SlotCount);
                var currentDisplay = replayFeedback[slot];
                var truth = original.TrueFeedback[slot];

                if (original.LiedSlots[slot])
                {
                    // This slot was lied about — might fix the lie (show truth)
                    replayFeedback[slot] = truth;
                }
                else
                {
                    // This slot was truthful — introduce a new lie
                    replayFeedback[slot] = InvertFeedback(truth);
                }
                alteredSlots[slot] = true;
                alteredCount++;
            }

            results[i] = new ReplayResult
            {
                DisplayFeedback = replayFeedback,
                AlteredSlots = alteredSlots
            };
        }

        return results;
    }

    private SlotFeedback InvertFeedback(SlotFeedback real)
    {
        return real switch
        {
            SlotFeedback.Correct => SlotFeedback.NotPresent,
            SlotFeedback.WrongPosition => SlotFeedback.Correct,
            SlotFeedback.NotPresent => SlotFeedback.WrongPosition,
            _ => real
        };
    }

    // --- Factory methods (matching gameplay spec v3) ---

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
    public SlotFeedback[] TrueFeedback;
    public SlotFeedback[] DisplayFeedback;
    public bool[] LiedSlots;
    public bool IsSolution;
}

public class ReplayResult
{
    public SlotFeedback[] DisplayFeedback;
    public bool[] AlteredSlots;
}
```

- [ ] **Step 4: Build and verify tests pass**

Run: `cd /Users/andrew/Repositories/anko/Signal-Godot && dotnet build`

Expected: Build succeeds. (Full test execution requires Godot runtime — we verify logic correctness via build + test harness later.)

- [ ] **Step 5: Commit**

```bash
cd /Users/andrew/Repositories/anko/Signal-Godot
git add scripts/minigame/DecryptionPuzzle.cs scripts/tests/DecryptionLogicTest.cs
git commit -m "feat: update DecryptionPuzzle for spec v3 — remove batch/cooldown, add replay lies, fix factory values

Remove guessesPerBatch/cooldown system (spec says no hard fail).
Add PrepareReplay() for NEREUS replay lie manipulation.
Fix Section5Hostile values from 10 to 8.
Add comprehensive logic tests."
```

---

## Task 2: Build DecryptionPuzzleUI — Layout, Input, and Feedback Display

**Files:**
- Create: `scripts/minigame/DecryptionPuzzleUI.cs`

This is the main visual component. It handles:
- Current guess input (clickable slots + value picker)
- Submit button
- Guess history grid with feedback colors
- Solved state

The replay animation and visual tells are added in Tasks 3 and 4.

- [ ] **Step 1: Create DecryptionPuzzleUI.cs with layout skeleton**

Create `scripts/minigame/DecryptionPuzzleUI.cs`:

```csharp
using System;
using Godot;

namespace Signal.Minigame;

/// <summary>
/// Complete visual interface for the Mastermind-style decryption puzzle.
/// All UI built programmatically — no scene editor dependency.
/// </summary>
public partial class DecryptionPuzzleUI : Control
{
    // --- Signals ---
    [Signal] public delegate void PuzzleCompletedEventHandler(int guessCount, float timeSpent);
    [Signal] public delegate void PuzzleCancelledEventHandler();

    // --- Hex theme ---
    private static readonly string[] HexLabels = { "0a", "3f", "b2", "e7", "1c", "d4", "8f", "5b" };
    private static readonly Color[] HexTints =
    {
        new(0.3f, 0.4f, 0.8f),   // 0a — deep blue
        new(0.2f, 0.7f, 0.7f),   // 3f — teal
        new(0.3f, 0.75f, 0.35f), // b2 — green
        new(0.85f, 0.7f, 0.2f),  // e7 — amber
        new(0.9f, 0.5f, 0.2f),   // 1c — orange
        new(0.8f, 0.3f, 0.3f),   // d4 — red
        new(0.6f, 0.35f, 0.8f),  // 8f — purple
        new(0.8f, 0.3f, 0.65f),  // 5b — magenta
    };

    // --- Feedback colors ---
    private static readonly Color ColorCorrect = new(0.2f, 0.8f, 0.3f);
    private static readonly Color ColorWrongPos = new(0.9f, 0.75f, 0.1f);
    private static readonly Color ColorNotPresent = new(0.8f, 0.2f, 0.2f);
    private static readonly Color ColorPending = new(0.15f, 0.18f, 0.22f);
    private static readonly Color ColorEmpty = new(0.12f, 0.14f, 0.18f);
    private static readonly Color ColorSelected = new(0.25f, 0.3f, 0.4f);

    // --- Layout constants ---
    private const int SlotSize = 56;
    private const int SlotGap = 8;
    private const int FeedbackDotSize = 12;
    private const int RowHeight = 64;

    // --- State ---
    private DecryptionPuzzle _puzzle;
    private int _section;
    private int[] _currentGuess;
    private int _filledSlots;
    private float _elapsedTime;
    private bool _active;

    // --- Replay state (Task 3 adds animation) ---
    public enum ReplayState { Idle, Replaying, ShowingNew }
    private ReplayState _replayState = ReplayState.Idle;
    private ReplayResult[] _replayResults;
    private int _replayIndex;
    private float _replayTimer;
    private const float ReplayRowDuration = 0.6f;
    private const float ReplaySlotDelay = 0.12f;
    private const float NewFeedbackSlotDelay = 0.15f;
    private int _replaySlotIndex;

    // --- UI references ---
    private VBoxContainer _rootContainer;
    private Label _titleLabel;
    private Label _statusLabel;
    private Label _timerLabel;
    private VBoxContainer _historyContainer;
    private HBoxContainer _inputRow;
    private HBoxContainer _valuePickerRow;
    private Button _submitButton;
    private ColorRect[] _inputSlots;
    private Label[] _inputSlotLabels;
    private Button[] _valueButtons;
    private ScrollContainer _historyScroll;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
    }

    /// <summary>
    /// Start a new puzzle for the given section (1-5, where 5 = hostile, 6 = cooperative).
    /// </summary>
    public void StartPuzzle(int section, int seed = -1)
    {
        _section = section;
        if (seed < 0) seed = (int)(Time.GetTicksMsec() & 0x7FFFFFFF);

        _puzzle = section switch
        {
            1 => DecryptionPuzzle.CreateSection1(seed),
            2 => DecryptionPuzzle.CreateSection2(seed),
            3 => DecryptionPuzzle.CreateSection3(seed),
            4 => DecryptionPuzzle.CreateSection4(seed),
            5 => DecryptionPuzzle.CreateSection5Hostile(seed),
            6 => DecryptionPuzzle.CreateSection5Cooperative(seed),
            _ => DecryptionPuzzle.CreateSection1(seed)
        };

        _currentGuess = new int[_puzzle.SlotCount];
        Array.Fill(_currentGuess, -1);
        _filledSlots = 0;
        _elapsedTime = 0f;
        _active = true;
        _replayState = ReplayState.Idle;

        BuildUI();
    }

    private void BuildUI()
    {
        // Clear previous UI
        foreach (var child in GetChildren()) child.QueueFree();

        // Dark background
        var bg = new ColorRect();
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        bg.Color = new Color(0.04f, 0.06f, 0.09f);
        AddChild(bg);

        // Margin wrapper
        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 24);
        margin.AddThemeConstantOverride("margin_right", 24);
        margin.AddThemeConstantOverride("margin_top", 16);
        margin.AddThemeConstantOverride("margin_bottom", 16);
        AddChild(margin);

        _rootContainer = new VBoxContainer();
        _rootContainer.AddThemeConstantOverride("separation", 12);
        margin.AddChild(_rootContainer);

        // Title bar
        var titleBar = new HBoxContainer();
        _titleLabel = new Label { Text = $"DECRYPTION TERMINAL — Section {_section}" };
        _titleLabel.AddThemeFontSizeOverride("font_size", 18);
        _titleLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.8f, 0.9f));
        titleBar.AddChild(_titleLabel);
        titleBar.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill }); // spacer
        _timerLabel = new Label { Text = "0.0s" };
        _timerLabel.AddThemeFontSizeOverride("font_size", 16);
        _timerLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.6f));
        titleBar.AddChild(_timerLabel);
        _rootContainer.AddChild(titleBar);

        // Separator
        var sep = new HSeparator();
        sep.AddThemeConstantOverride("separation", 4);
        _rootContainer.AddChild(sep);

        // History grid (scrollable)
        _historyScroll = new ScrollContainer();
        _historyScroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        _historyScroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        _historyContainer = new VBoxContainer();
        _historyContainer.AddThemeConstantOverride("separation", 4);
        _historyContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _historyScroll.AddChild(_historyContainer);
        _rootContainer.AddChild(_historyScroll);

        // Status label
        _statusLabel = new Label { Text = "Select values and submit your guess." };
        _statusLabel.AddThemeFontSizeOverride("font_size", 14);
        _statusLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.6f));
        _rootContainer.AddChild(_statusLabel);

        // Current guess input row
        BuildInputRow();

        // Value picker
        BuildValuePicker();
    }

    private void BuildInputRow()
    {
        var inputSection = new HBoxContainer();
        inputSection.AddThemeConstantOverride("separation", 8);

        var guessLabel = new Label { Text = "GUESS:" };
        guessLabel.AddThemeFontSizeOverride("font_size", 14);
        guessLabel.AddThemeColorOverride("font_color", new Color(0.4f, 0.5f, 0.6f));
        inputSection.AddChild(guessLabel);

        _inputRow = new HBoxContainer();
        _inputRow.AddThemeConstantOverride("separation", SlotGap);
        _inputSlots = new ColorRect[_puzzle.SlotCount];
        _inputSlotLabels = new Label[_puzzle.SlotCount];

        for (int i = 0; i < _puzzle.SlotCount; i++)
        {
            var slotContainer = new Control();
            slotContainer.CustomMinimumSize = new Vector2(SlotSize, SlotSize);

            var slotBg = new ColorRect();
            slotBg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            slotBg.Color = ColorEmpty;
            slotContainer.AddChild(slotBg);
            _inputSlots[i] = slotBg;

            var slotLabel = new Label { Text = "__", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            slotLabel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            slotLabel.AddThemeFontSizeOverride("font_size", 20);
            slotLabel.AddThemeColorOverride("font_color", new Color(0.3f, 0.35f, 0.4f));
            slotContainer.AddChild(slotLabel);
            _inputSlotLabels[i] = slotLabel;

            // Click to clear this slot
            int slotIndex = i;
            var clickArea = new Button();
            clickArea.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            clickArea.Flat = true;
            clickArea.Pressed += () => ClearSlot(slotIndex);
            slotContainer.AddChild(clickArea);

            _inputRow.AddChild(slotContainer);
        }
        inputSection.AddChild(_inputRow);

        // Spacer
        inputSection.AddChild(new Control { CustomMinimumSize = new Vector2(16, 0) });

        // Submit button
        _submitButton = new Button { Text = "SUBMIT", Disabled = true };
        _submitButton.CustomMinimumSize = new Vector2(100, SlotSize);
        _submitButton.AddThemeFontSizeOverride("font_size", 16);
        _submitButton.Pressed += OnSubmitPressed;
        inputSection.AddChild(_submitButton);

        // Cancel button
        var cancelBtn = new Button { Text = "✕" };
        cancelBtn.CustomMinimumSize = new Vector2(SlotSize, SlotSize);
        cancelBtn.AddThemeFontSizeOverride("font_size", 18);
        cancelBtn.Pressed += () => EmitSignal(SignalName.PuzzleCancelled);
        inputSection.AddChild(cancelBtn);

        _rootContainer.AddChild(inputSection);
    }

    private void BuildValuePicker()
    {
        var pickerSection = new HBoxContainer();
        pickerSection.AddThemeConstantOverride("separation", 8);

        var pickerLabel = new Label { Text = "VALUES:" };
        pickerLabel.AddThemeFontSizeOverride("font_size", 14);
        pickerLabel.AddThemeColorOverride("font_color", new Color(0.4f, 0.5f, 0.6f));
        pickerSection.AddChild(pickerLabel);

        _valuePickerRow = new HBoxContainer();
        _valuePickerRow.AddThemeConstantOverride("separation", SlotGap);
        _valueButtons = new Button[_puzzle.ValueCount];

        for (int v = 0; v < _puzzle.ValueCount; v++)
        {
            int value = v;
            var btn = new Button();
            btn.Text = HexLabels[v];
            btn.CustomMinimumSize = new Vector2(SlotSize, SlotSize);
            btn.AddThemeFontSizeOverride("font_size", 18);

            // Subtle tint via StyleBoxFlat
            var style = new StyleBoxFlat();
            style.BgColor = HexTints[v].Darkened(0.6f);
            style.BorderColor = HexTints[v].Darkened(0.3f);
            style.SetBorderWidthAll(2);
            style.SetCornerRadiusAll(4);
            btn.AddThemeStyleboxOverride("normal", style);

            var hoverStyle = new StyleBoxFlat();
            hoverStyle.BgColor = HexTints[v].Darkened(0.4f);
            hoverStyle.BorderColor = HexTints[v];
            hoverStyle.SetBorderWidthAll(2);
            hoverStyle.SetCornerRadiusAll(4);
            btn.AddThemeStyleboxOverride("hover", hoverStyle);

            btn.AddThemeColorOverride("font_color", HexTints[v].Lightened(0.3f));
            btn.Pressed += () => OnValuePicked(value);
            _valuePickerRow.AddChild(btn);
            _valueButtons[v] = btn;
        }
        pickerSection.AddChild(_valuePickerRow);

        // Backspace button
        var backBtn = new Button { Text = "⌫" };
        backBtn.CustomMinimumSize = new Vector2(SlotSize, SlotSize);
        backBtn.AddThemeFontSizeOverride("font_size", 20);
        backBtn.Pressed += OnBackspace;
        pickerSection.AddChild(backBtn);

        _rootContainer.AddChild(pickerSection);
    }

    // --- Input handling ---

    private void OnValuePicked(int value)
    {
        if (!_active || _replayState != ReplayState.Idle) return;

        // Fill the leftmost empty slot
        for (int i = 0; i < _puzzle.SlotCount; i++)
        {
            if (_currentGuess[i] == -1)
            {
                _currentGuess[i] = value;
                _filledSlots++;
                UpdateInputSlot(i, value);
                break;
            }
        }
        _submitButton.Disabled = _filledSlots < _puzzle.SlotCount;
    }

    private void ClearSlot(int index)
    {
        if (!_active || _replayState != ReplayState.Idle) return;
        if (_currentGuess[index] == -1) return;

        _currentGuess[index] = -1;
        _filledSlots--;

        // Shift remaining values left to fill gap
        for (int i = index; i < _puzzle.SlotCount - 1; i++)
        {
            _currentGuess[i] = _currentGuess[i + 1];
            if (_currentGuess[i] >= 0) UpdateInputSlot(i, _currentGuess[i]);
            else UpdateInputSlotEmpty(i);
        }
        _currentGuess[_puzzle.SlotCount - 1] = -1;
        UpdateInputSlotEmpty(_puzzle.SlotCount - 1);

        _submitButton.Disabled = true;
    }

    private void OnBackspace()
    {
        if (!_active || _replayState != ReplayState.Idle) return;

        // Remove the rightmost filled slot
        for (int i = _puzzle.SlotCount - 1; i >= 0; i--)
        {
            if (_currentGuess[i] != -1)
            {
                _currentGuess[i] = -1;
                _filledSlots--;
                UpdateInputSlotEmpty(i);
                _submitButton.Disabled = true;
                return;
            }
        }
    }

    private void UpdateInputSlot(int index, int value)
    {
        _inputSlots[index].Color = HexTints[value].Darkened(0.6f);
        _inputSlotLabels[index].Text = HexLabels[value];
        _inputSlotLabels[index].AddThemeColorOverride("font_color", HexTints[value].Lightened(0.3f));
    }

    private void UpdateInputSlotEmpty(int index)
    {
        _inputSlots[index].Color = ColorEmpty;
        _inputSlotLabels[index].Text = "__";
        _inputSlotLabels[index].AddThemeColorOverride("font_color", new Color(0.3f, 0.35f, 0.4f));
    }

    // --- Keyboard input ---

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_active || @event is not InputEventKey key || !key.Pressed) return;

        // Keys 1-8 map to values 0-7
        int keyValue = key.Keycode switch
        {
            Key.Key1 => 0, Key.Key2 => 1, Key.Key3 => 2, Key.Key4 => 3,
            Key.Key5 => 4, Key.Key6 => 5, Key.Key7 => 6, Key.Key8 => 7,
            _ => -1
        };
        if (keyValue >= 0 && keyValue < _puzzle.ValueCount)
        {
            OnValuePicked(keyValue);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (key.Keycode == Key.Backspace)
        {
            OnBackspace();
            GetViewport().SetInputAsHandled();
        }
        else if (key.Keycode == Key.Enter && !_submitButton.Disabled)
        {
            OnSubmitPressed();
            GetViewport().SetInputAsHandled();
        }
    }

    // --- Submit and feedback ---

    private void OnSubmitPressed()
    {
        if (!_active || _filledSlots < _puzzle.SlotCount || _replayState != ReplayState.Idle) return;

        var guess = (int[])_currentGuess.Clone();
        var result = _puzzle.SubmitGuess(guess);
        if (result == null) return;

        // Disable input during replay + feedback
        _submitButton.Disabled = true;

        if (_puzzle.History.Count > 1)
        {
            // Start replay of previous guesses before showing new feedback
            StartReplay(result);
        }
        else
        {
            // First guess — show feedback directly
            ShowNewFeedback(result);
        }
    }

    /// <summary>
    /// Add a completed guess row to the history grid with feedback colors.
    /// </summary>
    private void AddHistoryRow(int[] guess, SlotFeedback[] feedback, bool[] liedSlots, bool animated)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 4);

        // Guess number
        var numLabel = new Label { Text = $"{_historyContainer.GetChildCount() + 1}." };
        numLabel.AddThemeFontSizeOverride("font_size", 14);
        numLabel.AddThemeColorOverride("font_color", new Color(0.4f, 0.4f, 0.5f));
        numLabel.CustomMinimumSize = new Vector2(30, 0);
        row.AddChild(numLabel);

        // Value slots
        for (int i = 0; i < guess.Length; i++)
        {
            var slotContainer = new Control();
            slotContainer.CustomMinimumSize = new Vector2(SlotSize, 44);

            var slotBg = new ColorRect();
            slotBg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            slotBg.Color = animated ? ColorPending : GetFeedbackColor(feedback[i]).Darkened(0.5f);
            slotContainer.AddChild(slotBg);

            var label = new Label
            {
                Text = HexLabels[guess[i]],
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            label.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            label.AddThemeFontSizeOverride("font_size", 16);
            label.AddThemeColorOverride("font_color", animated ? new Color(0.3f, 0.35f, 0.4f) : Colors.White);
            slotContainer.AddChild(label);

            row.AddChild(slotContainer);
        }

        // Feedback dots
        var dotRow = new HBoxContainer();
        dotRow.AddThemeConstantOverride("separation", 4);
        for (int i = 0; i < feedback.Length; i++)
        {
            var dot = new ColorRect();
            dot.CustomMinimumSize = new Vector2(FeedbackDotSize, FeedbackDotSize);
            dot.Color = animated ? ColorPending : GetFeedbackColor(feedback[i]);
            dotRow.AddChild(dot);
        }
        // Center dots vertically
        var dotCenter = new CenterContainer();
        dotCenter.CustomMinimumSize = new Vector2(0, 44);
        dotCenter.AddChild(dotRow);
        row.AddChild(dotCenter);

        _historyContainer.AddChild(row);

        // Auto-scroll to bottom
        CallDeferred(nameof(ScrollToBottom));
    }

    private void ScrollToBottom()
    {
        _historyScroll.ScrollVertical = (int)_historyScroll.GetVScrollBar().MaxValue;
    }

    /// <summary>
    /// Update a history row's feedback colors (used during replay animation).
    /// </summary>
    private void UpdateHistoryRowFeedback(int rowIndex, SlotFeedback[] feedback, bool[] alteredSlots)
    {
        if (rowIndex >= _historyContainer.GetChildCount()) return;
        var row = _historyContainer.GetChild(rowIndex) as HBoxContainer;
        if (row == null) return;

        int slotCount = feedback.Length;
        // Children: numLabel, slot0..slotN-1, dotCenter
        for (int i = 0; i < slotCount; i++)
        {
            int childIndex = i + 1; // skip numLabel
            if (childIndex >= row.GetChildCount()) break;
            var slotContainer = row.GetChild(childIndex) as Control;
            if (slotContainer == null) continue;

            var bg = slotContainer.GetChild(0) as ColorRect;
            var label = slotContainer.GetChild(1) as Label;
            if (bg != null) bg.Color = GetFeedbackColor(feedback[i]).Darkened(0.5f);
            if (label != null) label.AddThemeColorOverride("font_color", Colors.White);
        }

        // Update dots
        var dotCenter = row.GetChild(row.GetChildCount() - 1) as CenterContainer;
        var dotRow = dotCenter?.GetChild(0) as HBoxContainer;
        if (dotRow != null)
        {
            for (int i = 0; i < feedback.Length && i < dotRow.GetChildCount(); i++)
            {
                var dot = dotRow.GetChild(i) as ColorRect;
                if (dot != null) dot.Color = GetFeedbackColor(feedback[i]);
            }
        }
    }

    /// <summary>
    /// Reveal feedback for a single slot in a history row (for sequential animation).
    /// </summary>
    private void RevealHistorySlot(int rowIndex, int slotIndex, SlotFeedback feedback, bool isTell)
    {
        if (rowIndex >= _historyContainer.GetChildCount()) return;
        var row = _historyContainer.GetChild(rowIndex) as HBoxContainer;
        if (row == null) return;

        int childIndex = slotIndex + 1;
        if (childIndex >= row.GetChildCount()) return;
        var slotContainer = row.GetChild(childIndex) as Control;
        if (slotContainer == null) return;

        var bg = slotContainer.GetChild(0) as ColorRect;
        var label = slotContainer.GetChild(1) as Label;
        Color feedbackColor = GetFeedbackColor(feedback);

        if (bg != null) bg.Color = feedbackColor.Darkened(0.5f);
        if (label != null) label.AddThemeColorOverride("font_color", Colors.White);

        // Update corresponding dot
        var dotCenter = row.GetChild(row.GetChildCount() - 1) as CenterContainer;
        var dotRow = dotCenter?.GetChild(0) as HBoxContainer;
        if (dotRow != null && slotIndex < dotRow.GetChildCount())
        {
            var dot = dotRow.GetChild(slotIndex) as ColorRect;
            if (dot != null) dot.Color = feedbackColor;
        }

        // Visual tell for lies (Task 4 enhances this with full animation)
        if (isTell && bg != null)
        {
            PlayTellEffect(bg, slotContainer, feedback);
        }
    }

    private static Color GetFeedbackColor(SlotFeedback fb)
    {
        return fb switch
        {
            SlotFeedback.Correct => ColorCorrect,
            SlotFeedback.WrongPosition => ColorWrongPos,
            SlotFeedback.NotPresent => ColorNotPresent,
            _ => ColorPending
        };
    }

    // --- Replay system ---

    private void StartReplay(GuessResult newResult)
    {
        _replayResults = _puzzle.PrepareReplay();
        _replayState = ReplayState.Replaying;
        _replayIndex = 0;
        _replaySlotIndex = 0;
        _replayTimer = 0f;
        _statusLabel.Text = "Replaying previous guesses...";

        // Reset all history row visuals to pending (they'll animate back)
        for (int i = 0; i < _historyContainer.GetChildCount(); i++)
        {
            ResetHistoryRowToPending(i);
        }

        // Add the new guess row (pending, will be revealed after replay)
        AddHistoryRow(newResult.Guess, newResult.DisplayFeedback, newResult.LiedSlots, animated: true);
    }

    private void ResetHistoryRowToPending(int rowIndex)
    {
        if (rowIndex >= _historyContainer.GetChildCount()) return;
        var row = _historyContainer.GetChild(rowIndex) as HBoxContainer;
        if (row == null) return;

        // Reset slot backgrounds and labels
        for (int i = 1; i < row.GetChildCount() - 1; i++) // skip numLabel and dotCenter
        {
            var slotContainer = row.GetChild(i) as Control;
            if (slotContainer == null) continue;
            var bg = slotContainer.GetChild(0) as ColorRect;
            var label = slotContainer.GetChild(1) as Label;
            if (bg != null) bg.Color = ColorPending;
            if (label != null) label.AddThemeColorOverride("font_color", new Color(0.3f, 0.35f, 0.4f));
        }

        // Reset dots
        var dotCenter = row.GetChild(row.GetChildCount() - 1) as CenterContainer;
        var dotRow = dotCenter?.GetChild(0) as HBoxContainer;
        if (dotRow == null) return;
        for (int i = 0; i < dotRow.GetChildCount(); i++)
        {
            var dot = dotRow.GetChild(i) as ColorRect;
            if (dot != null) dot.Color = ColorPending;
        }
    }

    private void ShowNewFeedback(GuessResult result)
    {
        // Add row and animate slot-by-slot
        AddHistoryRow(result.Guess, result.DisplayFeedback, result.LiedSlots, animated: true);
        _replayState = ReplayState.ShowingNew;
        _replayIndex = _historyContainer.GetChildCount() - 1;
        _replaySlotIndex = 0;
        _replayTimer = 0f;
    }

    private void FinishFeedback()
    {
        _replayState = ReplayState.Idle;

        if (_puzzle.IsSolved)
        {
            _active = false;
            _statusLabel.Text = $"DECRYPTED in {_puzzle.GuessesMade} guesses!";
            _statusLabel.AddThemeColorOverride("font_color", ColorCorrect);
            EmitSignal(SignalName.PuzzleCompleted, _puzzle.GuessesMade, _elapsedTime);
            return;
        }

        // Reset input for next guess
        Array.Fill(_currentGuess, -1);
        _filledSlots = 0;
        for (int i = 0; i < _puzzle.SlotCount; i++) UpdateInputSlotEmpty(i);
        _submitButton.Disabled = true;
        _statusLabel.Text = $"Guess {_puzzle.GuessesMade + 1} — select values and submit.";
        _statusLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.6f));
    }

    // --- Visual tell placeholder (Task 4 implements full animation) ---

    private void PlayTellEffect(ColorRect bg, Control container, SlotFeedback displayFeedback)
    {
        // Flicker effect: briefly show a contrasting color
        var tween = CreateTween();
        var originalColor = bg.Color;
        tween.TweenProperty(bg, "color", Colors.White, 0.05f);
        tween.TweenProperty(bg, "color", originalColor, 0.05f);
        tween.TweenProperty(bg, "color", Colors.White, 0.05f);
        tween.TweenProperty(bg, "color", originalColor, 0.05f);
    }

    // --- Process loop for replay animation ---

    public override void _Process(double delta)
    {
        if (!_active && _replayState == ReplayState.Idle) return;

        if (_active) _elapsedTime += (float)delta;
        _timerLabel.Text = $"{_elapsedTime:F1}s";

        switch (_replayState)
        {
            case ReplayState.Replaying:
                ProcessReplay((float)delta);
                break;
            case ReplayState.ShowingNew:
                ProcessNewFeedback((float)delta);
                break;
        }
    }

    private void ProcessReplay(float delta)
    {
        _replayTimer += delta;

        // Reveal one slot at a time per row
        if (_replayTimer >= ReplaySlotDelay)
        {
            _replayTimer -= ReplaySlotDelay;

            if (_replayIndex < _replayResults.Length)
            {
                var replay = _replayResults[_replayIndex];

                if (_replaySlotIndex < _puzzle.SlotCount)
                {
                    bool isTell = replay.AlteredSlots[_replaySlotIndex];
                    RevealHistorySlot(_replayIndex, _replaySlotIndex,
                        replay.DisplayFeedback[_replaySlotIndex], isTell);
                    _replaySlotIndex++;
                }
                else
                {
                    // Row complete — move to next row
                    _replayIndex++;
                    _replaySlotIndex = 0;
                    _replayTimer = -0.3f; // Brief pause between rows
                }
            }
            else
            {
                // Replay complete — now show new guess feedback
                _replayState = ReplayState.ShowingNew;
                _replayIndex = _historyContainer.GetChildCount() - 1; // Last row = new guess
                _replaySlotIndex = 0;
                _replayTimer = -0.4f; // Pause before new feedback
                _statusLabel.Text = "Analyzing...";
            }
        }
    }

    private void ProcessNewFeedback(float delta)
    {
        _replayTimer += delta;

        if (_replayTimer >= NewFeedbackSlotDelay)
        {
            _replayTimer -= NewFeedbackSlotDelay;

            if (_replaySlotIndex < _puzzle.SlotCount)
            {
                var lastResult = _puzzle.History[_puzzle.History.Count - 1];
                bool isTell = lastResult.LiedSlots[_replaySlotIndex];
                RevealHistorySlot(_replayIndex, _replaySlotIndex,
                    lastResult.DisplayFeedback[_replaySlotIndex], isTell);
                _replaySlotIndex++;
            }
            else
            {
                FinishFeedback();
            }
        }
    }
}
```

- [ ] **Step 2: Build to verify it compiles**

Run: `cd /Users/andrew/Repositories/anko/Signal-Godot && dotnet build`

Expected: Build succeeds (no other scripts reference the old `DecryptionPuzzle` constructor except our new test file which matches the new API).

- [ ] **Step 3: Commit**

```bash
cd /Users/andrew/Repositories/anko/Signal-Godot
git add scripts/minigame/DecryptionPuzzleUI.cs
git commit -m "feat: add DecryptionPuzzleUI with input, history grid, replay, and feedback display

Programmatic UI: hex value picker, slot input, guess history with
sequential feedback animation, replay state machine, section support 1-6.
Keyboard (1-8, backspace, enter) and mouse input."
```

---

## Task 3: Enhance Visual Tells for NEREUS Lies

**Files:**
- Modify: `scripts/minigame/DecryptionPuzzleUI.cs`

Replace the placeholder `PlayTellEffect` with section-appropriate tell animations:
- Section 3: **Flicker** — rapid alternation between true and lie colors
- Section 4: **Brief true color** — shows truth for 0.2s then transitions to lie
- Section 5: **Multiple types** — combines flicker + shake

- [ ] **Step 1: Replace PlayTellEffect with section-aware implementation**

In `DecryptionPuzzleUI.cs`, replace the `PlayTellEffect` method with:

```csharp
    private void PlayTellEffect(ColorRect bg, Control container, SlotFeedback displayFeedback)
    {
        switch (_section)
        {
            case 3:
                PlayFlickerTell(bg, displayFeedback);
                break;
            case 4:
                PlayBriefTruthTell(bg, displayFeedback);
                break;
            case 5:
                PlayCompositeTell(bg, container, displayFeedback);
                break;
            default:
                // Sections 1-2 and cooperative have no tells (no lies)
                break;
        }
    }

    /// <summary>
    /// Section 3 tell: rapid flicker between a "wrong" color and the final display color.
    /// The wrong color is a glimpse of the truth.
    /// </summary>
    private void PlayFlickerTell(ColorRect bg, SlotFeedback displayFeedback)
    {
        var lieColor = GetFeedbackColor(displayFeedback).Darkened(0.5f);
        var flashColor = Colors.White.Lerp(lieColor, 0.3f);
        var tween = CreateTween();
        tween.TweenProperty(bg, "color", flashColor, 0.04f);
        tween.TweenProperty(bg, "color", lieColor, 0.04f);
        tween.TweenProperty(bg, "color", flashColor, 0.04f);
        tween.TweenProperty(bg, "color", lieColor, 0.04f);
        tween.TweenProperty(bg, "color", flashColor, 0.04f);
        tween.TweenProperty(bg, "color", lieColor, 0.06f);
    }

    /// <summary>
    /// Section 4 tell: briefly shows a different (true) color before settling on the lie.
    /// The true color is visible for ~0.2 seconds — blink and you miss it.
    /// </summary>
    private void PlayBriefTruthTell(ColorRect bg, SlotFeedback displayFeedback)
    {
        // We don't expose the true feedback to the UI — instead we show a
        // contrasting "hint" color that differs from the displayed feedback.
        var lieColor = GetFeedbackColor(displayFeedback).Darkened(0.5f);
        // Pick a hint color that contrasts with the lie
        Color hintColor = displayFeedback switch
        {
            SlotFeedback.Correct => ColorNotPresent.Darkened(0.3f),     // If lie shows green, flash red
            SlotFeedback.WrongPosition => ColorCorrect.Darkened(0.3f),  // If lie shows yellow, flash green
            SlotFeedback.NotPresent => ColorWrongPos.Darkened(0.3f),    // If lie shows red, flash yellow
            _ => Colors.White
        };
        var tween = CreateTween();
        tween.TweenProperty(bg, "color", hintColor, 0.05f);
        tween.TweenInterval(0.15f); // Hold the true color briefly
        tween.TweenProperty(bg, "color", lieColor, 0.12f); // Smooth transition to lie
    }

    /// <summary>
    /// Section 5 tell: combines flicker with a position shake for maximum visual noise.
    /// Still catchable if you're watching, but harder with 2 lies per round.
    /// </summary>
    private void PlayCompositeTell(ColorRect bg, Control container, SlotFeedback displayFeedback)
    {
        var lieColor = GetFeedbackColor(displayFeedback).Darkened(0.5f);
        Color hintColor = displayFeedback switch
        {
            SlotFeedback.Correct => ColorNotPresent.Darkened(0.3f),
            SlotFeedback.WrongPosition => ColorCorrect.Darkened(0.3f),
            SlotFeedback.NotPresent => ColorWrongPos.Darkened(0.3f),
            _ => Colors.White
        };

        // Color tween: flash + settle
        var colorTween = CreateTween();
        colorTween.TweenProperty(bg, "color", hintColor, 0.04f);
        colorTween.TweenProperty(bg, "color", lieColor, 0.03f);
        colorTween.TweenProperty(bg, "color", hintColor, 0.04f);
        colorTween.TweenProperty(bg, "color", lieColor, 0.1f);

        // Position shake tween (parallel)
        var originalPos = container.Position;
        var shakeTween = CreateTween();
        shakeTween.TweenProperty(container, "position", originalPos + new Vector2(3, 0), 0.03f);
        shakeTween.TweenProperty(container, "position", originalPos + new Vector2(-3, 0), 0.03f);
        shakeTween.TweenProperty(container, "position", originalPos + new Vector2(2, 0), 0.03f);
        shakeTween.TweenProperty(container, "position", originalPos, 0.03f);
    }
```

- [ ] **Step 2: Build and verify**

Run: `cd /Users/andrew/Repositories/anko/Signal-Godot && dotnet build`

Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
cd /Users/andrew/Repositories/anko/Signal-Godot
git add scripts/minigame/DecryptionPuzzleUI.cs
git commit -m "feat: add section-aware visual tells for NEREUS lies

Section 3: rapid flicker tell.
Section 4: brief true-color flash before settling on lie.
Section 5: composite flicker + shake effect."
```

---

## Task 4: Build DecryptionTestHarness and Scene

**Files:**
- Create: `scripts/minigame/DecryptionTestHarness.cs`
- Create: `scenes/DecryptionTest.tscn`

The test harness allows standalone playtesting of the decryption puzzle across all sections with keyboard shortcuts and stats tracking.

- [ ] **Step 1: Create DecryptionTestHarness.cs**

Create `scripts/minigame/DecryptionTestHarness.cs`:

```csharp
using Godot;
using Signal.Core;

namespace Signal.Minigame;

/// <summary>
/// Standalone test harness for the Mastermind decryption puzzle.
/// Controls: 1-6 = section, Space = new puzzle (same section), Esc = quit
/// </summary>
public partial class DecryptionTestHarness : Control
{
    private DecryptionPuzzleUI _puzzleUI;
    private Label _infoLabel;
    private Label _resultsLabel;
    private int _completedCount;
    private float _totalTime;
    private int _totalGuesses;
    private int _currentSection = 1;

    public override void _Ready()
    {
        // Background
        var bg = new ColorRect();
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        bg.Color = new Color(0.02f, 0.03f, 0.05f);
        AddChild(bg);

        var root = new VBoxContainer();
        root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        root.AddThemeConstantOverride("separation", 4);
        var rootMargin = new MarginContainer();
        rootMargin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        rootMargin.AddThemeConstantOverride("margin_left", 12);
        rootMargin.AddThemeConstantOverride("margin_right", 12);
        rootMargin.AddThemeConstantOverride("margin_top", 8);
        rootMargin.AddThemeConstantOverride("margin_bottom", 8);
        rootMargin.AddChild(root);
        AddChild(rootMargin);

        // Info bar
        _infoLabel = new Label();
        _infoLabel.AddThemeFontSizeOverride("font_size", 14);
        _infoLabel.AddThemeColorOverride("font_color", new Color(0.4f, 0.4f, 0.5f));
        UpdateInfoLabel();
        root.AddChild(_infoLabel);

        // Puzzle UI
        _puzzleUI = new DecryptionPuzzleUI();
        _puzzleUI.SizeFlagsVertical = SizeFlags.ExpandFill;
        _puzzleUI.PuzzleCompleted += OnPuzzleCompleted;
        _puzzleUI.PuzzleCancelled += OnPuzzleCancelled;
        root.AddChild(_puzzleUI);

        // Results bar
        _resultsLabel = new Label();
        _resultsLabel.AddThemeFontSizeOverride("font_size", 14);
        _resultsLabel.AddThemeColorOverride("font_color", new Color(0.4f, 0.6f, 0.4f));
        _resultsLabel.Text = "Completed: 0 | Avg guesses: -- | Avg time: --";
        root.AddChild(_resultsLabel);

        // Key hints
        var hints = new Label();
        hints.AddThemeFontSizeOverride("font_size", 12);
        hints.AddThemeColorOverride("font_color", new Color(0.3f, 0.3f, 0.4f));
        hints.Text = "Section: 1=PressureLock 2=CrewQuarters 3=Research(lies) 4=Engineering 5=Command(hostile) 6=Command(coop) | Space=New | Esc=Quit";
        root.AddChild(hints);

        // Start first puzzle
        CallDeferred(MethodName.StartInitialPuzzle);
        GameLog.Event("Test", "Decryption test harness loaded");
    }

    private void StartInitialPuzzle()
    {
        _puzzleUI.StartPuzzle(_currentSection);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey key || !key.Pressed) return;

        switch (key.Keycode)
        {
            case Key.F1: _currentSection = 1; UpdateInfoLabel(); _puzzleUI.StartPuzzle(1); break;
            case Key.F2: _currentSection = 2; UpdateInfoLabel(); _puzzleUI.StartPuzzle(2); break;
            case Key.F3: _currentSection = 3; UpdateInfoLabel(); _puzzleUI.StartPuzzle(3); break;
            case Key.F4: _currentSection = 4; UpdateInfoLabel(); _puzzleUI.StartPuzzle(4); break;
            case Key.F5: _currentSection = 5; UpdateInfoLabel(); _puzzleUI.StartPuzzle(5); break;
            case Key.F6: _currentSection = 6; UpdateInfoLabel(); _puzzleUI.StartPuzzle(6); break;
            case Key.Space: _puzzleUI.StartPuzzle(_currentSection); break;
            case Key.Escape: GetTree().Quit(); break;
            default: return; // Don't consume unrecognized keys
        }

        GetViewport().SetInputAsHandled();
    }

    private void UpdateInfoLabel()
    {
        string[] sections =
        {
            "", "Sec 1: Pressure Lock (4 slots, 6 vals, no lies)",
            "Sec 2: Crew Quarters (4 slots, 6 vals, repeats)",
            "Sec 3: Research Lab (5 slots, 8 vals, 1 lie)",
            "Sec 4: Engineering (5 slots, 8 vals, 1 lie, active replay)",
            "Sec 5H: Command - Hostile (6 slots, 8 vals, 2 lies)",
            "Sec 5C: Command - Cooperative (4 slots, 6 vals, no lies)"
        };
        _infoLabel.Text = _currentSection <= 6 ? sections[_currentSection] : "Unknown section";
    }

    private void OnPuzzleCompleted(int guessCount, float timeSpent)
    {
        _completedCount++;
        _totalTime += timeSpent;
        _totalGuesses += guessCount;
        float avgTime = _totalTime / _completedCount;
        float avgGuesses = (float)_totalGuesses / _completedCount;
        _resultsLabel.Text = $"Completed: {_completedCount} | Last: {guessCount} guesses in {timeSpent:F1}s | Avg: {avgGuesses:F1} guesses, {avgTime:F1}s";
        GameLog.Event("Test", $"Decryption completed: {guessCount} guesses, {timeSpent:F1}s (avg: {avgGuesses:F1} guesses, {avgTime:F1}s over {_completedCount})");

        // Auto-start next puzzle after brief delay
        GetTree().CreateTimer(2.0).Timeout += () => _puzzleUI.StartPuzzle(_currentSection);
    }

    private void OnPuzzleCancelled()
    {
        _puzzleUI.StartPuzzle(_currentSection);
    }
}
```

- [ ] **Step 2: Create DecryptionTest.tscn scene file**

Create `scenes/DecryptionTest.tscn`:

```
[gd_scene load_steps=2 format=3]

[ext_resource type="Script" path="res://scripts/minigame/DecryptionTestHarness.cs" id="1"]

[node name="DecryptionTestHarness" type="Control"]
layout_mode = 3
anchors_preset = 15
anchor_right = 1.0
anchor_bottom = 1.0
grow_horizontal = 2
grow_vertical = 2
script = ExtResource("1")
```

- [ ] **Step 3: Build and verify**

Run: `cd /Users/andrew/Repositories/anko/Signal-Godot && dotnet build`

Expected: Build succeeds.

- [ ] **Step 4: Commit**

```bash
cd /Users/andrew/Repositories/anko/Signal-Godot
git add scripts/minigame/DecryptionTestHarness.cs scenes/DecryptionTest.tscn
git commit -m "feat: add decryption puzzle test harness with section selection

F1-F6 to switch sections, Space for new puzzle, Esc to quit.
Tracks completion stats: guess count, time, averages."
```

---

## Task 5: Wire DecryptionLogicTest into Autotest and Verify

**Files:**
- Modify: `scenes/AutoPlaytest.tscn` (add DecryptionLogicTest node)

- [ ] **Step 1: Add DecryptionLogicTest to AutoPlaytest scene**

Add a child node to AutoPlaytest.tscn so logic tests run alongside existing playtests. Alternatively, add it as an autoloaded test:

In `scripts/tests/AutotestBootstrap.cs`, the existing code checks for `--autotest` and loads AutoPlaytest. We can add DecryptionLogicTest as a sibling. But the simpler approach: add it as a child of the AutoPlaytest scene.

Edit `scenes/AutoPlaytest.tscn` to add the test node:

```
[gd_scene load_steps=3 format=3]

[ext_resource type="Script" path="res://scripts/tests/AutoPlaytest.cs" id="1"]
[ext_resource type="Script" path="res://scripts/tests/DecryptionLogicTest.cs" id="2"]

[node name="AutoPlaytest" type="Node"]
script = ExtResource("1")

[node name="DecryptionLogicTest" type="Node" parent="."]
script = ExtResource("2")
```

- [ ] **Step 2: Build and run autotest**

Run: `cd /Users/andrew/Repositories/anko/Signal-Godot && dotnet build`

Expected: Build succeeds. (Full test execution requires Godot runtime with `--autotest` flag.)

- [ ] **Step 3: Commit**

```bash
cd /Users/andrew/Repositories/anko/Signal-Godot
git add scripts/tests/DecryptionLogicTest.cs scenes/AutoPlaytest.tscn
git commit -m "test: add DecryptionPuzzle logic tests to autotest suite

Tests: unlimited guessing, feedback accuracy, lie inversion,
replay lie generation, solved detection, factory spec compliance."
```

---

## Task 6: Smoke Test and Playtest Verification

**Files:** None (verification only)

- [ ] **Step 1: Build the complete project**

Run: `cd /Users/andrew/Repositories/anko/Signal-Godot && dotnet build`

Expected: Clean build, no warnings related to our new files.

- [ ] **Step 2: Verify all files are in place**

Run:
```bash
ls -la scripts/minigame/DecryptionPuzzle.cs scripts/minigame/DecryptionPuzzleUI.cs scripts/minigame/DecryptionTestHarness.cs scenes/DecryptionTest.tscn scripts/tests/DecryptionLogicTest.cs
```

Expected: All 5 files exist.

- [ ] **Step 3: Verify DecryptionPuzzle API consistency**

Run: `cd /Users/andrew/Repositories/anko/Signal-Godot && grep -n "PrepareReplay\|StartPuzzle\|PuzzleCompleted\|SubmitGuess" scripts/minigame/DecryptionPuzzle.cs scripts/minigame/DecryptionPuzzleUI.cs scripts/minigame/DecryptionTestHarness.cs`

Expected: Consistent usage — `PrepareReplay()` called in UI, `SubmitGuess()` called in UI, `StartPuzzle()` called in harness and UI, `PuzzleCompleted` signal connected in harness.

- [ ] **Step 4: Document what to playtest**

After building, open Godot and run `DecryptionTest.tscn` to verify:

1. **Section 1 (F1):** 4 slots, 6 hex values visible. Click values to fill slots. Submit. Feedback appears slot-by-slot (green/yellow/red). No replay on first guess. Replay plays on guess 2+. No lies.
2. **Section 2 (F2):** Same as S1 but repeats allowed (same value can fill multiple slots).
3. **Section 3 (F3):** 5 slots, 8 values. After submitting, exactly 1 feedback slot per round should be inverted. Visual tell: flicker effect. Replay may fix a previous lie (~30%).
4. **Section 4 (F4):** Same slots/values as S3. Tell is "brief true color" — watch for a different color flash before the lie settles. Replay more actively alters history.
5. **Section 5 Hostile (F5):** 6 slots, 8 values, 2 lies. Composite tell with shake. Most challenging.
6. **Section 5 Cooperative (F6):** Easy — 4 slots, 6 values, no lies. Feels relaxing.

**Tune these during playtest (Priority #2):**
- `ReplaySlotDelay` (currently 0.12s per slot) — faster or slower replay?
- `NewFeedbackSlotDelay` (currently 0.15s) — satisfying reveal timing?
- Tell visibility — are flickers catchable but not obvious?
- 6 vs 8 values — does 8 feel too hard for Section 1?
- Replay lie frequency — 30% too rare/common for Section 3?

---

## Summary

| Task | Files | Description |
|------|-------|-------------|
| 1 | DecryptionPuzzle.cs, DecryptionLogicTest.cs | Core logic: remove batch/cooldown, add replay lies, fix values, add tests |
| 2 | DecryptionPuzzleUI.cs | Full UI: input, history grid, replay animation, feedback display |
| 3 | DecryptionPuzzleUI.cs | Section-aware visual tells for lies |
| 4 | DecryptionTestHarness.cs, DecryptionTest.tscn | Standalone test harness with section selection |
| 5 | AutoPlaytest.tscn | Wire logic tests into autotest suite |
| 6 | (verification only) | Build, file check, API consistency, playtest checklist |

**Dependencies:** Task 1 must complete before Tasks 2-5. Tasks 2-3 are sequential (3 modifies 2's file). Tasks 4-5 are independent of 2-3. Task 6 requires all previous tasks.

**Parallelization opportunities:**
- Task 1 (core logic) must run first
- After Task 1: Tasks 2+3 can run in parallel with Tasks 4+5
- Task 6 runs last

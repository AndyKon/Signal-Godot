using System;
using Godot;
using Signal.Minigame;

namespace Signal.Tests;

/// <summary>
/// Unit tests for DecryptionPuzzle core logic.
/// Pure logic tests — no Godot systems required beyond Node lifecycle.
/// Run by adding to autoload or attaching to a scene node.
/// </summary>
public partial class DecryptionLogicTest : Node
{
    private int _passed;
    private int _failed;

    public override void _Ready()
    {
        GD.Print("=== DECRYPTION LOGIC TESTS STARTING ===");

        TestUnlimitedGuessing();
        TestBasicFeedbackExactMatch();
        TestBasicFeedbackWrongPosition();
        TestBasicFeedbackNotPresent();
        TestNoRepeatsUniqueValues();
        TestLiesInvertFeedback();
        TestReplayLieGeneration();
        TestSolvedState();
        TestFactoryValues();

        GD.Print("");
        GD.Print("=== DECRYPTION LOGIC TEST RESULTS ===");
        GD.Print($"PASSED: {_passed}");
        GD.Print($"FAILED: {_failed}");
        GD.Print($"TOTAL:  {_passed + _failed}");
        GD.Print("=== END DECRYPTION LOGIC TESTS ===");
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    private void TestUnlimitedGuessing()
    {
        // Submit 20 guesses; none should return null unless the puzzle is solved.
        var puzzle = DecryptionPuzzle.CreateSection1(seed: 1);
        int nullCount = 0;
        int[] badGuess = new int[] { 0, 1, 2, 3 }; // likely wrong, keeps going

        for (int i = 0; i < 20; i++)
        {
            if (puzzle.IsSolved) break;
            var result = puzzle.SubmitGuess(badGuess);
            if (result == null) nullCount++;
        }

        Check("TestUnlimitedGuessing: 20 guesses never return null", nullCount == 0);
    }

    private void TestBasicFeedbackExactMatch()
    {
        // A no-lies puzzle: TrueFeedback == DisplayFeedback, all valid enum values.
        var puzzle = DecryptionPuzzle.CreateSection1(seed: 42);

        // Build a guess using the first slot count values (may or may not be correct).
        var guess = new int[] { 0, 1, 2, 3 };
        var result = puzzle.SubmitGuess(guess);

        Check("TestBasicFeedbackExactMatch: result not null", result != null);
        if (result == null) return;

        bool allValid = true;
        for (int i = 0; i < result.TrueFeedback.Length; i++)
        {
            var v = result.TrueFeedback[i];
            if (v != SlotFeedback.Correct && v != SlotFeedback.WrongPosition && v != SlotFeedback.NotPresent)
                allValid = false;
        }
        Check("TestBasicFeedbackExactMatch: all TrueFeedback values are valid enums", allValid);

        // No lies in Section1 — TrueFeedback must equal DisplayFeedback
        bool feedbackMatches = true;
        for (int i = 0; i < result.TrueFeedback.Length; i++)
        {
            if (result.TrueFeedback[i] != result.DisplayFeedback[i])
                feedbackMatches = false;
        }
        Check("TestBasicFeedbackExactMatch: no-lies puzzle true==display", feedbackMatches);
    }

    private void TestBasicFeedbackWrongPosition()
    {
        // On a no-lies puzzle, TrueFeedback == DisplayFeedback for every slot.
        var puzzle = DecryptionPuzzle.CreateSection2(seed: 7); // repeats, no lies

        var guess = new int[] { 3, 2, 1, 0 };
        var result = puzzle.SubmitGuess(guess);

        Check("TestBasicFeedbackWrongPosition: result not null", result != null);
        if (result == null) return;

        bool allMatch = true;
        for (int i = 0; i < result.TrueFeedback.Length; i++)
        {
            if (result.TrueFeedback[i] != result.DisplayFeedback[i])
                allMatch = false;
        }
        Check("TestBasicFeedbackWrongPosition: true==display for all slots (no lies)", allMatch);
    }

    private void TestBasicFeedbackNotPresent()
    {
        // Guess all the same high value — should produce some NotPresent for no-repeats puzzle.
        var puzzle = DecryptionPuzzle.CreateSection1(seed: 99); // no repeats, no lies
        var guess = new int[] { 5, 5, 5, 5 }; // invalid for no-repeats but puzzle still evaluates

        // Note: the puzzle doesn't validate guess values are unique, just length.
        var result = puzzle.SubmitGuess(guess);

        Check("TestBasicFeedbackNotPresent: result not null", result != null);
        if (result == null) return;

        bool allValid = true;
        for (int i = 0; i < result.DisplayFeedback.Length; i++)
        {
            var v = result.DisplayFeedback[i];
            if (v != SlotFeedback.Correct && v != SlotFeedback.WrongPosition && v != SlotFeedback.NotPresent)
                allValid = false;
        }
        Check("TestBasicFeedbackNotPresent: all DisplayFeedback values are valid enums", allValid);
    }

    private void TestNoRepeatsUniqueValues()
    {
        var puzzle = DecryptionPuzzle.CreateSection1(seed: 5);
        Check("TestNoRepeatsUniqueValues: Section1 AllowRepeats is false", !puzzle.AllowRepeats);
        Check("TestNoRepeatsUniqueValues: Section1 SlotCount is 4", puzzle.SlotCount == 4);
        Check("TestNoRepeatsUniqueValues: Section1 ValueCount is 6", puzzle.ValueCount == 6);
    }

    private void TestLiesInvertFeedback()
    {
        // Section3 has 1 lie per round. For any guess, exactly 1 slot should differ
        // between TrueFeedback and DisplayFeedback, and the inversion must follow the
        // rotation rule: Correct→NotPresent, WrongPosition→Correct, NotPresent→WrongPosition.
        var puzzle = DecryptionPuzzle.CreateSection3(seed: 10);

        // Try several guesses and verify the lie invariant.
        int[][] guesses = new int[][]
        {
            new int[] { 0, 1, 2, 3, 4 },
            new int[] { 1, 2, 3, 4, 5 },
            new int[] { 0, 0, 1, 1, 2 },
            new int[] { 5, 4, 3, 2, 1 },
            new int[] { 7, 6, 5, 4, 3 },
        };

        bool allGuessesHaveExactlyOneLie = true;
        bool allInversionsAreValid = true;

        foreach (var guess in guesses)
        {
            if (puzzle.IsSolved) break;
            var result = puzzle.SubmitGuess(guess);
            if (result == null) continue;

            int liedCount = 0;
            for (int i = 0; i < puzzle.SlotCount; i++)
            {
                if (result.TrueFeedback[i] != result.DisplayFeedback[i])
                {
                    liedCount++;
                    // Verify the inversion is the canonical rotation
                    var expected = result.TrueFeedback[i] switch
                    {
                        SlotFeedback.Correct      => SlotFeedback.NotPresent,
                        SlotFeedback.WrongPosition => SlotFeedback.Correct,
                        SlotFeedback.NotPresent    => SlotFeedback.WrongPosition,
                        _                          => result.TrueFeedback[i]
                    };
                    if (result.DisplayFeedback[i] != expected)
                        allInversionsAreValid = false;
                }
            }

            if (liedCount != 1)
                allGuessesHaveExactlyOneLie = false;
        }

        Check("TestLiesInvertFeedback: each Section3 round has exactly 1 lied slot", allGuessesHaveExactlyOneLie);
        Check("TestLiesInvertFeedback: all lie inversions follow the rotation rule", allInversionsAreValid);
    }

    private void TestReplayLieGeneration()
    {
        // Section4 puzzle: submit 3 guesses, call PrepareReplay(), verify structure.
        var puzzle = DecryptionPuzzle.CreateSection4(seed: 20);

        var guess1 = new int[] { 0, 1, 2, 3, 4 };
        var guess2 = new int[] { 1, 2, 3, 4, 5 };
        var guess3 = new int[] { 5, 4, 3, 2, 0 };

        puzzle.SubmitGuess(guess1);
        if (!puzzle.IsSolved) puzzle.SubmitGuess(guess2);
        if (!puzzle.IsSolved) puzzle.SubmitGuess(guess3);

        int historyCount = puzzle.GuessesMade;
        var replay = puzzle.PrepareReplay();

        Check("TestReplayLieGeneration: PrepareReplay returns non-null array", replay != null);
        if (replay == null) return;

        Check("TestReplayLieGeneration: replay length matches history count", replay.Length == historyCount);

        bool allCorrectLength = true;
        bool allAlteredSlotsCorrectLength = true;
        for (int i = 0; i < replay.Length; i++)
        {
            if (replay[i].DisplayFeedback == null || replay[i].DisplayFeedback.Length != puzzle.SlotCount)
                allCorrectLength = false;
            if (replay[i].AlteredSlots == null || replay[i].AlteredSlots.Length != puzzle.SlotCount)
                allAlteredSlotsCorrectLength = false;
        }
        Check("TestReplayLieGeneration: each replay DisplayFeedback has SlotCount entries", allCorrectLength);
        Check("TestReplayLieGeneration: each replay AlteredSlots has SlotCount entries", allAlteredSlotsCorrectLength);

        // Verify MaxReplayLiesPerCycle cap is respected
        int alteredEntries = 0;
        for (int i = 0; i < replay.Length; i++)
        {
            for (int j = 0; j < replay[i].AlteredSlots.Length; j++)
            {
                if (replay[i].AlteredSlots[j]) { alteredEntries++; break; }
            }
        }
        Check("TestReplayLieGeneration: altered entries <= MaxReplayLiesPerCycle",
            alteredEntries <= puzzle.MaxReplayLiesPerCycle);
    }

    private void TestSolvedState()
    {
        // Use a tiny 2-slot, 2-value no-lies puzzle and brute-force the answer.
        var puzzle = new DecryptionPuzzle(slots: 2, values: 2, allowRepeats: false,
                                          liesPerRound: 0, replayLieChance: 0f,
                                          maxReplayLiesPerCycle: 0, seed: 77);

        // There are only 2 possible arrangements of {0,1} in 2 slots without repeats.
        int[][] candidates = new int[][]
        {
            new int[] { 0, 1 },
            new int[] { 1, 0 },
        };

        bool solved = false;
        foreach (var candidate in candidates)
        {
            if (puzzle.IsSolved) break;
            var result = puzzle.SubmitGuess(candidate);
            if (result != null && result.IsSolution)
            {
                solved = true;
            }
        }

        Check("TestSolvedState: puzzle was solved by brute force", solved);
        Check("TestSolvedState: IsSolved is true after solve", puzzle.IsSolved);

        // After solving, SubmitGuess should return null
        var postSolveResult = puzzle.SubmitGuess(new int[] { 0, 1 });
        Check("TestSolvedState: SubmitGuess returns null after solved", postSolveResult == null);
    }

    private void TestFactoryValues()
    {
        var s1 = DecryptionPuzzle.CreateSection1(seed: 0);
        Check("TestFactoryValues: Section1 slots=4", s1.SlotCount == 4);
        Check("TestFactoryValues: Section1 values=6", s1.ValueCount == 6);
        Check("TestFactoryValues: Section1 allowRepeats=false", !s1.AllowRepeats);
        Check("TestFactoryValues: Section1 lies=0", s1.LiesPerRound == 0);

        var s2 = DecryptionPuzzle.CreateSection2(seed: 0);
        Check("TestFactoryValues: Section2 slots=4", s2.SlotCount == 4);
        Check("TestFactoryValues: Section2 values=6", s2.ValueCount == 6);
        Check("TestFactoryValues: Section2 allowRepeats=true", s2.AllowRepeats);
        Check("TestFactoryValues: Section2 lies=0", s2.LiesPerRound == 0);

        var s3 = DecryptionPuzzle.CreateSection3(seed: 0);
        Check("TestFactoryValues: Section3 slots=5", s3.SlotCount == 5);
        Check("TestFactoryValues: Section3 values=8", s3.ValueCount == 8);
        Check("TestFactoryValues: Section3 allowRepeats=true", s3.AllowRepeats);
        Check("TestFactoryValues: Section3 lies=1", s3.LiesPerRound == 1);
        Check("TestFactoryValues: Section3 replayChance=0.3", Math.Abs(s3.ReplayLieChance - 0.3f) < 0.0001f);
        Check("TestFactoryValues: Section3 maxReplay=1", s3.MaxReplayLiesPerCycle == 1);

        var s4 = DecryptionPuzzle.CreateSection4(seed: 0);
        Check("TestFactoryValues: Section4 slots=5", s4.SlotCount == 5);
        Check("TestFactoryValues: Section4 values=8", s4.ValueCount == 8);
        Check("TestFactoryValues: Section4 allowRepeats=true", s4.AllowRepeats);
        Check("TestFactoryValues: Section4 lies=1", s4.LiesPerRound == 1);
        Check("TestFactoryValues: Section4 replayChance=0.6", Math.Abs(s4.ReplayLieChance - 0.6f) < 0.0001f);
        Check("TestFactoryValues: Section4 maxReplay=1", s4.MaxReplayLiesPerCycle == 1);

        var s5h = DecryptionPuzzle.CreateSection5Hostile(seed: 0);
        Check("TestFactoryValues: Section5Hostile slots=6", s5h.SlotCount == 6);
        Check("TestFactoryValues: Section5Hostile values=8", s5h.ValueCount == 8);
        Check("TestFactoryValues: Section5Hostile allowRepeats=true", s5h.AllowRepeats);
        Check("TestFactoryValues: Section5Hostile lies=2", s5h.LiesPerRound == 2);
        Check("TestFactoryValues: Section5Hostile replayChance=0.8", Math.Abs(s5h.ReplayLieChance - 0.8f) < 0.0001f);
        Check("TestFactoryValues: Section5Hostile maxReplay=2", s5h.MaxReplayLiesPerCycle == 2);

        var s5c = DecryptionPuzzle.CreateSection5Cooperative(seed: 0);
        Check("TestFactoryValues: Section5Cooperative slots=4", s5c.SlotCount == 4);
        Check("TestFactoryValues: Section5Cooperative values=6", s5c.ValueCount == 6);
        Check("TestFactoryValues: Section5Cooperative allowRepeats=false", !s5c.AllowRepeats);
        Check("TestFactoryValues: Section5Cooperative lies=0", s5c.LiesPerRound == 0);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private void Check(string name, bool passed)
    {
        if (passed)
        {
            _passed++;
            GD.Print($"  PASS: {name}");
        }
        else
        {
            _failed++;
            GD.PrintErr($"  FAIL: {name}");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using Godot;
using Signal.Minigame;

namespace Signal.Tests;

/// <summary>
/// Auto-plays puzzles using a simulated player with deductive strategy,
/// across multiple seeds, and dumps detailed feedback logs.
/// Run via --decryption-scenarios flag.
/// </summary>
public partial class DecryptionScenarioRunner : Node
{
    private const int SeedsPerSection = 5;
    private const int MaxGuesses = 12;

    public override void _Ready()
    {
        GD.Print("╔══════════════════════════════════════════════════════════╗");
        GD.Print("║        DECRYPTION SCENARIO RUNNER — SMART PLAYER        ║");
        GD.Print("╚══════════════════════════════════════════════════════════╝");
        GD.Print("");

        var sections = new (string name, Func<int, DecryptionPuzzle> factory)[]
        {
            ("Section 1 — No lies, no repeats",    DecryptionPuzzle.CreateSection1),
            ("Section 2 — Repeats, no lies",        DecryptionPuzzle.CreateSection2),
            ("Section 3 — 1 lie/round, replay",     DecryptionPuzzle.CreateSection3),
            ("Section 4 — 1 lie, active replay",    DecryptionPuzzle.CreateSection4),
            ("Section 5H — 2 lies, heavy replay",   DecryptionPuzzle.CreateSection5Hostile),
            ("Section 5C — Cooperative (easy)",      DecryptionPuzzle.CreateSection5Cooperative),
        };

        int[] seeds = { 42, 99, 7, 256, 1337 };

        foreach (var (name, factory) in sections)
        {
            var sectionStats = new List<(int guesses, int lies, int replayAlters, bool solved)>();

            foreach (int seed in seeds)
            {
                var puzzle = factory(seed);
                var result = RunSmartPlayer(name, puzzle, seed);
                sectionStats.Add(result);
            }

            // Summary for this section
            PrintSectionSummary(name, seeds, sectionStats);
        }

        GD.Print("=== SCENARIO RUNNER COMPLETE ===");
        GetTree().Quit();
    }

    /// <summary>
    /// Simulated player using elimination strategy:
    /// 1. First guess: spread values to gather max info
    /// 2. Subsequent guesses: use feedback to narrow candidates
    /// For no-lies: trust all feedback. For lies: trust majority across rounds.
    /// </summary>
    private (int guesses, int totalLies, int totalReplayAlters, bool solved) RunSmartPlayer(
        string sectionName, DecryptionPuzzle puzzle, int seed)
    {
        _triedPerSlot = null; // reset for new puzzle

        var sb = new StringBuilder();
        sb.AppendLine($"┌─ {sectionName} (seed={seed}) ──────────────────────");
        sb.AppendLine($"│ {puzzle.SlotCount} slots, {puzzle.ValueCount} vals, " +
                      $"repeats={puzzle.AllowRepeats}, lies={puzzle.LiesPerRound}/rnd, " +
                      $"replayChance={puzzle.ReplayLieChance:F1}");
        sb.AppendLine("│");

        int totalLies = 0;
        int totalReplayAlters = 0;
        var rng = new Random(seed + 1000); // separate RNG for player strategy

        // Track what we know
        var confirmed = new int[puzzle.SlotCount];     // -1 = unknown
        var eliminated = new HashSet<int>[puzzle.SlotCount]; // values ruled out per slot
        var presentValues = new HashSet<int>();          // values known to be in answer
        var absentValues = new HashSet<int>();            // values known to NOT be in answer
        Array.Fill(confirmed, -1);
        for (int i = 0; i < puzzle.SlotCount; i++)
            eliminated[i] = new HashSet<int>();

        for (int g = 0; g < MaxGuesses; g++)
        {
            if (puzzle.IsSolved) break;

            // Show replay analysis
            if (puzzle.History.Count > 0)
            {
                var replay = puzzle.PrepareReplay();
                for (int r = 0; r < replay.Length; r++)
                {
                    for (int s = 0; s < replay[r].AlteredSlots.Length; s++)
                    {
                        if (replay[r].AlteredSlots[s])
                        {
                            var orig = puzzle.History[r].DisplayFeedback[s];
                            var altered = replay[r].DisplayFeedback[s];
                            sb.AppendLine($"│  ⚡ REPLAY: G{r+1}s{s} {Fc(orig)}→{Fc(altered)} (truth={Fc(puzzle.History[r].TrueFeedback[s])})");
                            totalReplayAlters++;
                        }
                    }
                }
            }

            // Build guess
            int[] guess = BuildSmartGuess(puzzle, g, confirmed, eliminated, presentValues, absentValues, rng);

            var result = puzzle.SubmitGuess(guess);
            if (result == null) break;

            // Count lies
            int roundLies = 0;
            for (int i = 0; i < result.LiedSlots.Length; i++)
                if (result.LiedSlots[i]) roundLies++;
            totalLies += roundLies;

            // Format output
            sb.Append($"│ G{g+1}: [");
            for (int i = 0; i < guess.Length; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(guess[i]);
            }
            sb.Append("] → ");
            for (int i = 0; i < result.DisplayFeedback.Length; i++)
            {
                sb.Append(Fc(result.DisplayFeedback[i]));
                sb.Append(result.LiedSlots[i] ? '!' : ' ');
            }
            if (roundLies > 0) sb.Append($" ({roundLies} lie)");
            if (result.IsSolution) sb.Append(" ★ SOLVED");
            sb.AppendLine();

            // Update knowledge from DISPLAYED feedback (player can't see truth)
            // Only trust feedback in no-lie sections
            if (puzzle.LiesPerRound == 0)
            {
                UpdateKnowledge(puzzle, result.DisplayFeedback, guess, confirmed, eliminated, presentValues, absentValues);
            }
            else
            {
                // With lies: still use feedback but more conservatively
                // Trust greens less, use cross-referencing
                UpdateKnowledgeWithLies(puzzle, result.DisplayFeedback, guess, confirmed, eliminated, presentValues, absentValues);
            }
        }

        if (!puzzle.IsSolved)
            sb.AppendLine($"│ (not solved in {MaxGuesses} guesses)");

        sb.AppendLine("└────────────────────────────────────────────────────");
        sb.AppendLine();
        GD.Print(sb.ToString());

        return (puzzle.GuessesMade, totalLies, totalReplayAlters, puzzle.IsSolved);
    }

    // Track which values we've already tried per slot to avoid repeating
    private HashSet<int>[] _triedPerSlot;

    private int[] BuildSmartGuess(DecryptionPuzzle puzzle, int guessNum,
        int[] confirmed, HashSet<int>[] eliminated, HashSet<int> present,
        HashSet<int> absent, Random rng)
    {
        int slots = puzzle.SlotCount;
        int vals = puzzle.ValueCount;
        var guess = new int[slots];

        if (_triedPerSlot == null)
        {
            _triedPerSlot = new HashSet<int>[slots];
            for (int i = 0; i < slots; i++)
                _triedPerSlot[i] = new HashSet<int>();
        }

        if (guessNum == 0)
        {
            // First guess: spread distinct values for maximum info
            for (int i = 0; i < slots; i++)
            {
                guess[i] = i % vals;
                _triedPerSlot[i].Add(guess[i]);
            }
            return guess;
        }

        // Fill confirmed slots
        for (int i = 0; i < slots; i++)
        {
            if (confirmed[i] >= 0)
                guess[i] = confirmed[i];
            else
                guess[i] = -1;
        }

        // For unconfirmed slots: build candidate list per slot, pick best
        for (int i = 0; i < slots; i++)
        {
            if (guess[i] >= 0) continue;

            // Candidates: not eliminated, not absent, prefer untried, prefer present
            int bestVal = -1;
            int bestScore = -1;

            for (int v = 0; v < vals; v++)
            {
                if (eliminated[i].Contains(v)) continue;
                if (absent.Contains(v)) continue;

                int score = 0;
                if (!_triedPerSlot[i].Contains(v)) score += 10; // strongly prefer untried
                if (present.Contains(v)) score += 5;            // prefer known-present

                if (score > bestScore)
                {
                    bestScore = score;
                    bestVal = v;
                }
            }

            // If everything is eliminated/absent (shouldn't happen), try anything
            if (bestVal < 0)
            {
                for (int v = 0; v < vals; v++)
                {
                    if (!_triedPerSlot[i].Contains(v)) { bestVal = v; break; }
                }
                if (bestVal < 0) bestVal = rng.Next(vals);
            }

            guess[i] = bestVal;
            _triedPerSlot[i].Add(bestVal);
        }

        return guess;
    }

    private void UpdateKnowledge(DecryptionPuzzle puzzle, SlotFeedback[] feedback, int[] guess,
        int[] confirmed, HashSet<int>[] eliminated, HashSet<int> present, HashSet<int> absent)
    {
        for (int i = 0; i < feedback.Length; i++)
        {
            switch (feedback[i])
            {
                case SlotFeedback.Correct:
                    confirmed[i] = guess[i];
                    present.Add(guess[i]);
                    break;
                case SlotFeedback.WrongPosition:
                    present.Add(guess[i]);
                    eliminated[i].Add(guess[i]); // right value, wrong slot
                    break;
                case SlotFeedback.NotPresent:
                    if (!puzzle.AllowRepeats)
                        absent.Add(guess[i]);
                    else
                        eliminated[i].Add(guess[i]); // only eliminate from this slot with repeats
                    break;
            }
        }
    }

    private void UpdateKnowledgeWithLies(DecryptionPuzzle puzzle, SlotFeedback[] feedback, int[] guess,
        int[] confirmed, HashSet<int>[] eliminated, HashSet<int> present, HashSet<int> absent)
    {
        // With lies, be conservative: only trust feedback that's consistent
        // across multiple rounds. For now: still update but don't add to absent
        // (since a "not present" could be a lie).
        for (int i = 0; i < feedback.Length; i++)
        {
            switch (feedback[i])
            {
                case SlotFeedback.Correct:
                    // Might be a lie — only soft-confirm
                    // Don't set confirmed[i] unless we've seen it green twice
                    present.Add(guess[i]);
                    break;
                case SlotFeedback.WrongPosition:
                    present.Add(guess[i]);
                    break;
                case SlotFeedback.NotPresent:
                    // Don't trust — could be a lie hiding a present value
                    break;
            }
        }
    }

    private void PrintSectionSummary(string name, int[] seeds,
        List<(int guesses, int lies, int replayAlters, bool solved)> stats)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"╠═══ SUMMARY: {name} ═══════════════════════════");
        sb.AppendLine("║");

        int solvedCount = 0;
        float totalGuesses = 0;
        int totalLies = 0;
        int totalAlters = 0;
        int minGuesses = int.MaxValue;
        int maxGuesses = 0;

        for (int i = 0; i < stats.Count; i++)
        {
            var (guesses, lies, alters, solved) = stats[i];
            sb.AppendLine($"║  seed {seeds[i],5}: {guesses} guesses, {lies} lies, {alters} replay alters — {(solved ? "SOLVED" : "UNSOLVED")}");
            if (solved) { solvedCount++; totalGuesses += guesses; }
            else totalGuesses += guesses;
            totalLies += lies;
            totalAlters += alters;
            if (guesses < minGuesses) minGuesses = guesses;
            if (guesses > maxGuesses) maxGuesses = guesses;
        }

        float avgGuesses = totalGuesses / stats.Count;
        float avgLies = (float)totalLies / stats.Count;

        sb.AppendLine("║");
        sb.AppendLine($"║  Solved: {solvedCount}/{stats.Count} | " +
                      $"Guesses: avg={avgGuesses:F1} min={minGuesses} max={maxGuesses} | " +
                      $"Lies/game: {avgLies:F1} | Replay alters: {totalAlters}");
        sb.AppendLine("╠══════════════════════════════════════════════════════════");
        sb.AppendLine();

        GD.Print(sb.ToString());
    }

    private static string Fc(SlotFeedback fb) => fb switch
    {
        SlotFeedback.Correct => "✓",
        SlotFeedback.WrongPosition => "◐",
        SlotFeedback.NotPresent => "✗",
        _ => "?"
    };
}

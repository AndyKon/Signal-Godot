using System;
using System.Text;
using Godot;
using Signal.Minigame;

namespace Signal.Tests;

/// <summary>
/// Auto-plays predefined guess sequences against seeded puzzles and dumps
/// detailed feedback logs — what the player sees, where lies are, what tells
/// would fire, and how replay alters history. Run via --decryption-scenarios flag.
///
/// Use this to evaluate difficulty, lie frequency, and tell visibility
/// without manual play.
/// </summary>
public partial class DecryptionScenarioRunner : Node
{
    public override void _Ready()
    {
        GD.Print("╔══════════════════════════════════════════════════════════╗");
        GD.Print("║        DECRYPTION SCENARIO RUNNER                       ║");
        GD.Print("╚══════════════════════════════════════════════════════════╝");
        GD.Print("");

        RunScenario("Section 1 — No lies, no repeats", DecryptionPuzzle.CreateSection1(42),
            new[] { new[]{0,1,2,3}, new[]{4,5,0,1}, new[]{3,2,5,4}, new[]{5,3,1,0}, new[]{2,0,4,5} });

        RunScenario("Section 2 — Repeats, no lies", DecryptionPuzzle.CreateSection2(42),
            new[] { new[]{0,1,2,3}, new[]{4,5,0,1}, new[]{3,2,5,4}, new[]{1,1,2,2}, new[]{0,0,3,3} });

        RunScenario("Section 3 — 1 lie/round, replay lies", DecryptionPuzzle.CreateSection3(42),
            new[] { new[]{0,1,2,3,4}, new[]{5,6,7,0,1}, new[]{2,3,4,5,6}, new[]{7,0,1,2,3}, new[]{4,5,6,7,0} });

        RunScenario("Section 4 — 1 lie, active replay", DecryptionPuzzle.CreateSection4(42),
            new[] { new[]{0,1,2,3,4}, new[]{5,6,7,0,1}, new[]{2,3,4,5,6}, new[]{7,0,1,2,3}, new[]{4,5,6,7,0} });

        RunScenario("Section 5 Hostile — 2 lies, heavy replay", DecryptionPuzzle.CreateSection5Hostile(42),
            new[] { new[]{0,1,2,3,4,5}, new[]{6,7,0,1,2,3}, new[]{4,5,6,7,0,1}, new[]{2,3,4,5,6,7} });

        RunScenario("Section 5 Coop — Easy mode", DecryptionPuzzle.CreateSection5Cooperative(42),
            new[] { new[]{0,1,2,3}, new[]{4,5,0,1}, new[]{3,2,5,4} });

        GD.Print("");
        GD.Print("=== SCENARIO RUNNER COMPLETE ===");

        // Quit after running (headless mode)
        GetTree().Quit();
    }

    private void RunScenario(string name, DecryptionPuzzle puzzle, int[][] guesses)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"┌─ {name} ─────────────────────────────────");
        sb.AppendLine($"│ Config: {puzzle.SlotCount} slots, {puzzle.ValueCount} values, " +
                      $"repeats={puzzle.AllowRepeats}, lies={puzzle.LiesPerRound}/round, " +
                      $"replayChance={puzzle.ReplayLieChance:F1}, maxReplayLies={puzzle.MaxReplayLiesPerCycle}");
        sb.AppendLine("│");

        int guessNum = 0;
        foreach (var guess in guesses)
        {
            if (puzzle.IsSolved) break;
            guessNum++;

            // If we have history, show replay analysis before submitting
            if (puzzle.History.Count > 0)
            {
                var replay = puzzle.PrepareReplay();
                bool anyAltered = false;
                for (int r = 0; r < replay.Length; r++)
                {
                    for (int s = 0; s < replay[r].AlteredSlots.Length; s++)
                    {
                        if (replay[r].AlteredSlots[s])
                        {
                            var orig = puzzle.History[r].DisplayFeedback[s];
                            var altered = replay[r].DisplayFeedback[s];
                            sb.AppendLine($"│  ⚡ REPLAY ALTER: Guess {r+1} slot {s}: " +
                                         $"{FeedbackChar(orig)}→{FeedbackChar(altered)} " +
                                         $"(truth={FeedbackChar(puzzle.History[r].TrueFeedback[s])})");
                            anyAltered = true;
                        }
                    }
                }
                if (!anyAltered && puzzle.LiesPerRound > 0)
                    sb.AppendLine("│  (replay: no alterations this cycle)");
            }

            var result = puzzle.SubmitGuess(guess);
            if (result == null) { sb.AppendLine($"│ Guess {guessNum}: null (puzzle solved or invalid)"); continue; }

            // Format guess values
            var guessStr = new StringBuilder();
            for (int i = 0; i < guess.Length; i++)
            {
                if (i > 0) guessStr.Append(' ');
                guessStr.Append(guess[i].ToString().PadLeft(2));
            }

            sb.Append($"│ Guess {guessNum}: [{guessStr}] → ");

            // Format feedback with lie markers
            for (int i = 0; i < result.DisplayFeedback.Length; i++)
            {
                sb.Append(FeedbackChar(result.DisplayFeedback[i]));
                if (result.LiedSlots[i])
                    sb.Append('!'); // ! marks a lie
                else
                    sb.Append(' ');
            }

            // Show lie details
            int lieCount = 0;
            for (int i = 0; i < result.LiedSlots.Length; i++)
                if (result.LiedSlots[i]) lieCount++;

            if (lieCount > 0)
            {
                sb.Append($" [{lieCount} lie(s): ");
                bool first = true;
                for (int i = 0; i < result.LiedSlots.Length; i++)
                {
                    if (result.LiedSlots[i])
                    {
                        if (!first) sb.Append(", ");
                        sb.Append($"slot {i} truth={FeedbackChar(result.TrueFeedback[i])} shown={FeedbackChar(result.DisplayFeedback[i])}");
                        first = false;
                    }
                }
                sb.Append(']');
            }

            sb.AppendLine();

            if (result.IsSolution)
            {
                sb.AppendLine($"│ ✓ SOLVED in {guessNum} guesses");
                break;
            }
        }

        if (!puzzle.IsSolved)
        {
            sb.AppendLine($"│ (not solved after {guessNum} guesses — answer hidden until solved)");
        }

        sb.AppendLine("└──────────────────────────────────────────────────────");
        sb.AppendLine("");

        GD.Print(sb.ToString());
    }

    private static string FeedbackChar(SlotFeedback fb) => fb switch
    {
        SlotFeedback.Correct => "✓",
        SlotFeedback.WrongPosition => "◐",
        SlotFeedback.NotPresent => "✗",
        _ => "?"
    };
}

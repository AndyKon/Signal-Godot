#!/bin/bash
# Launch decryption puzzle playtesting with log capture
#
# Usage:
#   ./playtest.sh                    # Interactive, Section 1
#   ./playtest.sh 3                  # Interactive, Section 3 (lies!)
#   ./playtest.sh scenarios          # Automated scenario dump (headless)
#   ./playtest.sh autotest           # Run logic tests (headless)
#
# Logs saved to: logs/playtest-YYYY-MM-DD-HHMMSS.log

set -euo pipefail
cd "$(dirname "$0")"

GODOT="/Applications/Godot_mono.app/Contents/MacOS/Godot"
if [ ! -x "$GODOT" ]; then
    echo "Error: Godot not found at $GODOT"
    echo "Update the GODOT variable in this script to match your install."
    exit 1
fi

mkdir -p logs

TIMESTAMP=$(date +%Y-%m-%d-%H%M%S)
LOGFILE="logs/playtest-${TIMESTAMP}.log"

MODE="${1:-1}"

case "$MODE" in
    scenarios)
        echo "Running automated scenario dump..."
        echo "Output: $LOGFILE"
        "$GODOT" --headless --path . -- --decryption-scenarios 2>&1 | tee "$LOGFILE"
        echo ""
        echo "Log saved: $LOGFILE"
        ;;
    autotest)
        echo "Running logic tests..."
        echo "Output: $LOGFILE"
        "$GODOT" --headless --path . -- --autotest 2>&1 | tee "$LOGFILE"
        echo ""
        echo "Log saved: $LOGFILE"
        ;;
    evidence)
        echo "Launching with pre-loaded evidence — press J to open evidence web"
        echo "Log: $LOGFILE"
        echo ""
        "$GODOT" --path . -- --evidence-test 2>&1 | tee "$LOGFILE"
        echo ""
        echo "Log saved: $LOGFILE"
        ;;
    [1-6])
        echo "Launching DecryptionTest — Section $MODE"
        echo "Controls: F1-F6=section, 1-8=values, Enter=submit, Backspace=undo, Esc=quit"
        echo "Log: $LOGFILE"
        echo ""
        "$GODOT" --path . res://scenes/DecryptionTest.tscn -- --section "$MODE" 2>&1 | tee "$LOGFILE"
        echo ""
        echo "Log saved: $LOGFILE"
        ;;
    *)
        echo "Usage: ./playtest.sh [1-6|scenarios|autotest|evidence]"
        echo "  1-6        Interactive playtest starting at that section"
        echo "  scenarios  Automated scenario dump (headless)"
        echo "  autotest   Run logic tests (headless)"
        echo "  evidence   Pre-load 20 evidence entries, press J to view web"
        exit 1
        ;;
esac

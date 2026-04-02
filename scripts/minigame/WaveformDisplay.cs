using Godot;
using System;

namespace Signal.Minigame;

public partial class WaveformDisplay : Control
{
    // Colors
    private Color _backgroundColor = new Color(0.05f, 0.08f, 0.12f);
    private Color _gridColor = new Color(0.1f, 0.15f, 0.2f);
    private Color _waveformColor = new Color(0.2f, 0.8f, 0.4f); // Green oscilloscope look
    private Color _signalColor = new Color(0.9f, 0.3f, 0.2f);   // Red for isolated signal

    // Colors
    private Color _referenceColor = new Color(0.4f, 0.4f, 0.8f, 0.25f); // Dim blue reference

    // Data
    private float[] _samples;
    private float[] _filteredSamples;
    private float[] _referenceSamples;
    private float _clarity;

    // Display settings
    private float _verticalScale = 1.0f;
    private bool _showFiltered = true;

    // Clarity threshold for "locked on" flash effect
    private const float ClarityThreshold = 0.85f;

    // Flash state
    private float _flashTimer;
    private bool _flashing;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(600, 200);
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical = SizeFlags.ExpandFill;
    }

    /// <summary>
    /// Update the display with new sample data.
    /// </summary>
    public void SetReferenceSamples(float[] reference)
    {
        _referenceSamples = reference;
        QueueRedraw();
    }

    public void SetSamples(float[] raw, float[] filtered, float clarity)
    {
        _samples = raw;
        _filteredSamples = filtered;

        // Detect crossing the clarity threshold to trigger a flash
        if (clarity >= ClarityThreshold && _clarity < ClarityThreshold)
        {
            _flashing = true;
            _flashTimer = 0.6f;
        }

        _clarity = clarity;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (_flashing)
        {
            _flashTimer -= (float)delta;
            if (_flashTimer <= 0f)
            {
                _flashing = false;
                _flashTimer = 0f;
            }
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        Vector2 size = Size;

        // --- Background ---
        DrawRect(new Rect2(Vector2.Zero, size), _backgroundColor);

        // --- Grid ---
        DrawGrid(size);

        // --- Waveform ---
        DrawWaveform(size);

        // --- Clarity indicator ---
        DrawClarityIndicator(size);

        // --- Flash border when clarity hits threshold ---
        if (_flashing && _flashTimer > 0f)
        {
            float alpha = _flashTimer / 0.6f; // fade out over the flash duration
            Color flashColor = new Color(0.2f, 0.9f, 0.5f, alpha * 0.7f);
            float borderWidth = 2f;
            // Top
            DrawRect(new Rect2(0, 0, size.X, borderWidth), flashColor);
            // Bottom
            DrawRect(new Rect2(0, size.Y - borderWidth, size.X, borderWidth), flashColor);
            // Left
            DrawRect(new Rect2(0, 0, borderWidth, size.Y), flashColor);
            // Right
            DrawRect(new Rect2(size.X - borderWidth, 0, borderWidth, size.Y), flashColor);
        }
    }

    private void DrawGrid(Vector2 size)
    {
        float centerY = size.Y * 0.5f;

        // Horizontal center line
        DrawLine(
            new Vector2(0, centerY),
            new Vector2(size.X, centerY),
            _gridColor,
            1f
        );

        // Horizontal quarter lines (25% and 75%)
        float quarterY = size.Y * 0.25f;
        float threeQuarterY = size.Y * 0.75f;
        Color dimGrid = new Color(_gridColor.R, _gridColor.G, _gridColor.B, 0.4f);
        DrawLine(new Vector2(0, quarterY), new Vector2(size.X, quarterY), dimGrid, 1f);
        DrawLine(new Vector2(0, threeQuarterY), new Vector2(size.X, threeQuarterY), dimGrid, 1f);

        // Vertical divisions (8 segments)
        int verticalDivisions = 8;
        for (int i = 1; i < verticalDivisions; i++)
        {
            float x = size.X * i / verticalDivisions;
            DrawLine(new Vector2(x, 0), new Vector2(x, size.Y), dimGrid, 1f);
        }
    }

    private void DrawWaveform(Vector2 size)
    {
        if (_samples == null || _samples.Length < 2)
            return;

        // Draw reference signal (dim blue — the target the player is trying to match)
        if (_referenceSamples != null && _referenceSamples.Length >= 2)
        {
            DrawSampleLine(_referenceSamples, size, _referenceColor, 2f);
        }

        bool hasFiltered = _showFiltered && _filteredSamples != null && _filteredSamples.Length >= 2;

        if (hasFiltered)
        {
            // Draw the filtered/blended waveform (morphs from noisy to clean as clarity increases)
            DrawSampleLine(_filteredSamples, size, _waveformColor, 2f);
        }
        else
        {
            // No filter applied yet — draw raw at full brightness
            DrawSampleLine(_samples, size, _waveformColor, 2f);
        }
    }

    private void DrawSampleLine(float[] samples, Vector2 size, Color color, float width)
    {
        int count = samples.Length;
        Vector2[] points = new Vector2[count];

        float centerY = size.Y * 0.5f;
        float halfHeight = size.Y * 0.5f * _verticalScale;

        for (int i = 0; i < count; i++)
        {
            float x = size.X * i / (count - 1);
            // Clamp the sample to [-1, 1] then map to screen Y (inverted: positive goes up)
            float clamped = Mathf.Clamp(samples[i], -1f, 1f);
            float y = centerY - clamped * halfHeight;
            points[i] = new Vector2(x, y);
        }

        DrawPolyline(points, color, width, true);
    }

    private void DrawClarityIndicator(Vector2 size)
    {
        // Draw a small arc + percentage in the top-right corner
        float margin = 12f;
        float radius = 20f;
        Vector2 center = new Vector2(size.X - margin - radius, margin + radius);

        // Background arc (full circle, dim)
        Color bgArc = new Color(0.15f, 0.15f, 0.15f, 0.6f);
        DrawArc(center, radius, 0f, Mathf.Tau, 32, bgArc, 3f);

        // Clarity arc — sweeps from top (-PI/2) clockwise
        if (_clarity > 0f)
        {
            Color arcColor = ClarityToColor(_clarity);
            float sweepAngle = _clarity * Mathf.Tau;
            float startAngle = -Mathf.Pi / 2f;
            DrawArc(center, radius, startAngle, startAngle + sweepAngle, 32, arcColor, 3f);
        }

        // Percentage text
        string pct = $"{Mathf.RoundToInt(_clarity * 100)}%";
        Color textColor = ClarityToColor(_clarity);
        DrawString(
            ThemeDB.FallbackFont,
            new Vector2(center.X - radius + 4f, center.Y + 5f),
            pct,
            HorizontalAlignment.Center,
            (int)(radius * 2) - 8,
            11,
            textColor
        );
    }

    /// <summary>
    /// Maps clarity (0..1) to a color: red -> yellow -> green.
    /// </summary>
    private static Color ClarityToColor(float clarity)
    {
        if (clarity < 0.5f)
        {
            // Red (0%) to Yellow (50%)
            float t = clarity / 0.5f;
            return new Color(
                Mathf.Lerp(0.9f, 0.95f, t),
                Mathf.Lerp(0.3f, 0.85f, t),
                Mathf.Lerp(0.2f, 0.15f, t)
            );
        }
        else
        {
            // Yellow (50%) to Green (85%+)
            float t = Mathf.Clamp((clarity - 0.5f) / 0.35f, 0f, 1f);
            return new Color(
                Mathf.Lerp(0.95f, 0.2f, t),
                Mathf.Lerp(0.85f, 0.9f, t),
                Mathf.Lerp(0.15f, 0.4f, t)
            );
        }
    }
}

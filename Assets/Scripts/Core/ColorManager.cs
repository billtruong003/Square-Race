using UnityEngine;
using System.Collections.Generic;

public class ColorManager : Singleton<ColorManager>
{
    [SerializeField] private Gradient _defaultGradient;

    // Runtime Config Data
    private ColorAssignmentMode _currentMode;
    private Gradient _runtimeGradient;
    private List<Color> _runtimeFixedColors;
    private List<Sprite> _runtimeShapes;
    private Sprite _runtimeDeathSprite;

    protected override void Awake()
    {
        base.Awake();
        ResetToDefaults();
    }

    private void ResetToDefaults()
    {
        _currentMode = ColorAssignmentMode.Gradient;
        _runtimeGradient = _defaultGradient;
        _runtimeFixedColors = new List<Color>();
        _runtimeShapes = new List<Sprite>();
        _runtimeDeathSprite = null;
    }

    public void SetRaceConfig(LevelData data)
    {
        if (data == null)
        {
            ResetToDefaults();
            return;
        }

        _currentMode = data.ColorMode;

        // Setup Gradient (Fallback to default if null)
        _runtimeGradient = data.RacerColorGradient != null ? data.RacerColorGradient : _defaultGradient;

        // Setup Fixed Colors
        _runtimeFixedColors = data.FixedColorSequence ?? new List<Color>();

        // Setup Shapes
        _runtimeShapes = data.RacerShapes ?? new List<Sprite>();

        // Setup Death Sprite
        _runtimeDeathSprite = data.DeathBodySprite;
    }

    public void ApplyToRacers(List<SquareController> racers)
    {
        if (racers == null || racers.Count == 0) return;

        int count = racers.Count;
        float gradientStep = 1f / Mathf.Max(1, count - 1);

        for (int i = 0; i < count; i++)
        {
            var racer = racers[i];
            if (racer == null) continue;

            // 1. Calculate Color based on Mode
            Color assignedColor = Color.white;

            switch (_currentMode)
            {
                case ColorAssignmentMode.FixedSequence:
                    if (_runtimeFixedColors.Count > 0)
                    {
                        // Modulo operator (%) giúp lặp lại màu nếu số xe > số màu
                        assignedColor = _runtimeFixedColors[i % _runtimeFixedColors.Count];
                    }
                    else
                    {
                        // Fallback nếu list rỗng
                        assignedColor = _runtimeGradient.Evaluate(i * gradientStep);
                    }
                    break;

                case ColorAssignmentMode.Gradient:
                default:
                    assignedColor = _runtimeGradient.Evaluate(i * gradientStep);
                    break;
            }

            // 2. Assign Shape (Always Cyclic)
            Sprite assignedShape = null;
            if (_runtimeShapes.Count > 0)
            {
                assignedShape = _runtimeShapes[i % _runtimeShapes.Count];
            }

            // 3. Apply Configuration to Racer
            racer.ConfigureVisuals(i + 1, assignedShape, assignedColor);

            // 4. Apply Death Sprite
            if (_runtimeDeathSprite != null)
            {
                racer.SetDeathSprite(_runtimeDeathSprite);
            }
        }
    }
}
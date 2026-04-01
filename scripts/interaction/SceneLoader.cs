using Godot;
using Signal.Core;

namespace Signal.Interaction;

public partial class SceneLoader : Node
{
    public static SceneLoader Instance { get; private set; }

    private ColorRect _overlay;
    private bool _isLoading;
    private float _fadeDuration = 0.5f;

    public override void _Ready()
    {
        Instance = this;

        var canvas = new CanvasLayer();
        canvas.Layer = 100;
        AddChild(canvas);

        _overlay = new ColorRect();
        _overlay.Color = new Color(0, 0, 0, 0);
        _overlay.AnchorsPreset = (int)Control.LayoutPreset.FullRect;
        _overlay.MouseFilter = Control.MouseFilterEnum.Ignore;
        canvas.AddChild(_overlay);

        GameLog.ManagerReady("SceneLoader");
    }

    public async void LoadScene(string scenePath, bool isNewSection = false)
    {
        if (_isLoading) return;
        _isLoading = true;

        string currentScene = GameManager.Instance?.State.CurrentScene ?? "unknown";
        string fullPath = scenePath.StartsWith("res://") ? scenePath : $"res://scenes/{scenePath}.tscn";

        GameLog.SceneTransition(currentScene, scenePath);

        // Fade out
        var tween = CreateTween();
        tween.TweenProperty(_overlay, "color:a", 1.0f, _fadeDuration);
        await ToSignal(tween, Tween.SignalName.Finished);

        GetTree().ChangeSceneToFile(fullPath);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.State.CurrentScene = scenePath;
            if (isNewSection)
                GameManager.Instance.SaveToSlot(0);
        }

        GameLog.SceneLoaded(scenePath);

        // Fade in
        tween = CreateTween();
        tween.TweenProperty(_overlay, "color:a", 0.0f, _fadeDuration);
        await ToSignal(tween, Tween.SignalName.Finished);

        _isLoading = false;
    }
}

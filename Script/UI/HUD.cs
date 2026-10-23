using Godot;
using Godot.Collections;

public partial class HUD : Control
{
    [Export] public UIHealthBar BossHealthBar;
    [Export] public EndUI endUi;
    [Export] public ColorRect black;
    [Export] Array<Control> tips;

    public ShaderMaterial shader;
    public int blackState;

    public override void _Ready()
    {
        base._Ready();
        shader = black.Material as ShaderMaterial;
    }

    public void SetBlack(float radius)
    {
        black.Visible = true;
        shader.SetShaderParameter("hole_radius", radius);
    }

    public void BlackOpen(float start = 0, float time = 1)
    {
        blackState = 0;
        Tween tween = black.CreateTween();
        tween.TweenMethod(Callable.From((float v) =>
        {
            shader.SetShaderParameter("hole_radius", v);
        }), start, 0.5f, time);
        tween.Finished += () => {
            blackState = 1;
            black.Visible = false;
        };
    }

    public void BlackClose(float end = 0, float time = 1)
    {
        blackState = 0;
        black.Visible = true;
        Tween tween = black.CreateTween();
        tween.TweenMethod(Callable.From((float v) =>
        {
            shader.SetShaderParameter("hole_radius", v);
        }), 0.5f, end, time);
        tween.Finished += () => blackState = -1;
    }

    public void ShowTips(int index, float time = 5)
    {
        if(index < 0 || index >= tips.Count)
        {
            return;
        }
        Control tip = tips[index];
        tip.Visible = true;
        tip.Modulate = Colors.Transparent;
        Tween tween = tip.CreateTween();
        tween.TweenProperty(tip, "modulate:a", 1, 0.4f);
        tween.TweenInterval(time);
        tween.TweenProperty(tip, "modulate:a", 0, 0.4f);
        tween.Finished += () => tip.Visible = false;
    }


}

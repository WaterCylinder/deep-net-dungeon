using Godot;
using Godot.Collections;

public partial class HUD : Control
{
    [Export] public UIHealthBar BossHealthBar;
    [Export] public EndUI endUi;
    [Export] public ColorRect black;
    [Export] Array<Control> tips;

    public ShaderMaterial shader;
    /// <summary>
    /// 是否结束变换
    /// </summary>
    public bool blackEnd;

    public override void _Ready()
    {
        base._Ready();
        shader = black.Material as ShaderMaterial;
    }

    public void SetBlack(float radius = 0, Vector2 holeCenter = default)
    {
        if(holeCenter == default)
        {
            holeCenter = new Vector2(0.5f, 0.5f);
        }
        black.Visible = true;
        shader.SetShaderParameter("hole_radius", radius);
        shader.SetShaderParameter("hole_center", holeCenter);
    }

    public void BlackOpen(float start = 0, float end = 0.5f, float time = 1)
    {
        blackEnd = false;
        Tween tween = black.CreateTween();
        tween.TweenMethod(Callable.From((float v) =>
        {
            shader.SetShaderParameter("hole_radius", v);
        }), start, end, time);
        tween.Finished += () => {
            blackEnd = true;
            black.Visible = false;
        };
    }

    public void BlackClose(float end = 0, float start = 0.5f, float time = 1)
    {
        blackEnd = false;
        black.Visible = true;
        Tween tween = black.CreateTween();
        tween.TweenMethod(Callable.From((float v) =>
        {
            shader.SetShaderParameter("hole_radius", v);
        }), start, end, time);
        tween.Finished += () => blackEnd = true;
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

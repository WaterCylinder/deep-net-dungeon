using Godot;
using System;

/// <summary>
/// 自动旋转并且跟随相机zoom调整大小的背景
/// </summary>
public partial class BGAutoRotateZoom : Node2D
{
    [Export] public CameraController camera;
    [Export] public Sprite2D bg;

    /// <summary>
    /// 1倍大小是背景贴图对应的视窗尺寸
    /// </summary>
    public Vector2 standradScreenSize = new Vector2(640, 360);

    public override void _PhysicsProcess(double delta)
    {
        Vector2 scale = camera.Zoom;
        scale.X = 1 / scale.X;
        scale.Y = 1 / scale.Y;
        Scale = scale;

        /*
        Vector2 screenSize = GetWindow().Size;
        Vector2 diff = screenSize / standradScreenSize;
        float s = Mathf.Max(diff.X, diff.Y);
        bg.Scale = new Vector2(s,s);
        */

        GlobalPosition = camera.GlobalPosition;
    }

    public override void _Process(double delta)
    {
        //Rotate((float)delta * 0.1f);
    }

}

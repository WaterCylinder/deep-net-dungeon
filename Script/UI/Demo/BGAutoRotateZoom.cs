using Godot;
using System;

/// <summary>
/// 自动旋转并且跟随相机zoom调整大小的背景
/// </summary>
public partial class BGAutoRotateZoom : Node2D
{
    [Export] public CameraController camera;
    public override void _PhysicsProcess(double delta)
    {
        Vector2 scale = camera.Zoom;
        scale.X = 1 / scale.X;
        scale.Y = 1 / scale.Y;
        Scale = scale;

        GlobalPosition = camera.GlobalPosition;
    }

    public override void _Process(double delta)
    {
        Rotate((float)delta * 0.1f);
    }

}

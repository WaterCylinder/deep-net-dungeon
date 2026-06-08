using Godot;
using System;

public partial class Player : CharacterBody2D
{
    [Export]
    public float Speed { get; set; } = 200.0f;

    private Vector2 _velocity = Vector2.Zero;

    public override void _PhysicsProcess(double delta)
    {
        // 获取输入
        Vector2 direction = InputManager.Instance.GetMoveDirection();
        
        // 计算速度
        _velocity = direction * Speed;
        
        // 应用移动
        Velocity = _velocity;
        MoveAndSlide();
    }
}
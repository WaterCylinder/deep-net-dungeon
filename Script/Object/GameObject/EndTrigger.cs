using Godot;
using System;

public partial class EndTrigger : Area2D
{
    Game game => GameManager.Instance.game;
    
    public override void _Ready()
    {
        base._Ready();

        BodyEntered += other =>
        {
            game.NotifyEnd(true);
        };
    }

}

using Godot;
using System;

public partial class HealthBar : EntityComponent
{
    [Export] public ColorRect bar;
    [Export] public Label label;
    protected override void _Init()
    {
        //血条组件初始化
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if(entity == null)
        {
            return;
        }
        float health = entity.currentHealth;
        float maxHealth = entity.maxHealth;
        bar.Scale = new Vector2(health / maxHealth, 1);
        label.Text = $"{Math.Round(health, 0)} / {Math.Round(maxHealth, 0)}";
    }

}

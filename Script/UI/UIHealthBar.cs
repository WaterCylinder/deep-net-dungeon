using Godot;
using System;

public partial class UIHealthBar : Control
{
    [Export] public Control healthInnerBar;
    [Export] public Control healthCover;
    [Export] public Control healthSlowCover;

    [Export] public Control shieldBar;
    [Export] public Label shieldLabel;

    [Export] public Label healthText;

    [Export] public Entity target;

    public void SetTarget(Entity entity)
    {
        target = entity;
        healthSlowCover.Scale = new(1f, 1f);
    }

    protected virtual void OnUpdate(double delta){}

    public override void _Process(double delta)
    {
        // 血条覆盖条
        if (Game.IsAble(target))
        {
            float rate = 1;
            float maxH = target.maxHealth.Get();
            if (target.currentHealth <= maxH)
            {
                rate = target.currentHealth / maxH;
            }
            healthCover.Scale = new(1 - rate, 1f);

            //血条缓慢覆盖条

            float slowX = healthSlowCover.Scale.X;
            if (slowX < (1 - rate))
            {
                slowX += (float)delta * 0.32f;
            }
            else
            {
                slowX = 1 - rate;
            }
            healthSlowCover.Scale = new(slowX, 1f);
            healthText.Text = $"{target.currentHealth:0.#}/{maxH:0.#}";

            //设置护盾条
            if (target.shield <= maxH)
            {
                rate = target.shield  / maxH;
                rate = Mathf.Min(rate, 1);
            }
            shieldBar.Scale = new(rate, 1f);
            if(rate > 0)
            {
                shieldLabel.Text = target.shield.ToString("0.#");
            }
            else
            {
                shieldBar.Scale = new(0f, 1f);
                shieldLabel.Text = "";
            }
        }
        else
        {
            healthCover.Scale = new(1f, 1f);
            healthSlowCover.Scale = new(1f, 1f);
            healthText.Text = "";
            shieldBar.Scale = new(0f, 1f);
            shieldLabel.Text = "";
        }

        // 血条滚动
        /*
        float diff = healthInnerBar.PivotOffsetRatio.X;
        diff -= (float)delta * 0.08f;
        if (diff < 0f)
        {
            diff = 1f;
        }
        healthInnerBar.PivotOffsetRatio = new Vector2(diff, PivotOffsetRatio.Y);
        */
        OnUpdate(delta);
    }

}

using Godot;
using System;
using Utils;

public partial class PUIHealthBar : Control
{
    [Export] public Control healthInnerBar;
    [Export] public Control healthCover;
    [Export] public Control healthSlowCover;

    [Export] public Control smallTextPoint;

    [Export] public PackedScene buffLayoutItemPrefab;
    [Export] public Control buffNode;
    [Export] public Control buffIntroNode;
    [Export] public RichTextLabel buffIntroLabel;

    Game game => GameManager.Instance.game;
    Player player => game.player;

    public override void _Ready()
    {
        player.OnHurtSignal += dmg =>
        {
            string text = dmg.ToString("F1");
            SmallText smt = SmallText.CreateSmallText(
                SmallTextType.QuickShow, 
                Tools.ScreenToWorld(smallTextPoint, game.camera),
                "-" + text  
            );
            smt.SetColor(new Color("#460051"));
            smt.onUpdate += dalte =>
            {
                smt.GlobalPosition = Tools.ScreenToWorld(smallTextPoint, game.camera);
            };
        };

        //初始化SlowCover的状态
        healthSlowCover.Scale = new(1f, 1f);

        //玩家信号添加更新buffs
        player.OnAttachBuffSignal += UpdateBuffs;
        foreach (var child in buffNode.GetChildren())
        {
            child.QueueFree();
        }
    }

    public override void _Process(double delta)
    {
        // 血条覆盖条
        if (Game.IsAble(player))
        {
            float rate = 1;
            if (player.currentHealth <= player.maxHealth)
            {
                rate = player.currentHealth / player.maxHealth;
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
        }
        else
        {
            healthCover.Scale = new(1f, 1f);
            healthSlowCover.Scale = new(1f, 1f);
        }

        // 血条滚动
        float diff = healthInnerBar.PivotOffsetRatio.X;
        diff -= (float)delta * 0.64f;
        if (diff < 0f)
        {
            diff = 1f;
        }
        healthInnerBar.PivotOffsetRatio = new Vector2(diff, PivotOffsetRatio.Y);
    }

    public void UpdateBuffs(Buff _buff)
    {
        //Buff获取
        var buffs = player.Buffs;
        foreach (var child in buffNode.GetChildren())
        {
            child.QueueFree();
        }
        foreach (var buff in buffs)
        {
            Control layoutItem = buffLayoutItemPrefab.Instantiate<Control>();
            TextureRect textureRect = layoutItem.GetNodeOrNull<TextureRect>("TextureRect");
            if(textureRect != null)
            {
                textureRect.Texture = buff.sprite;
            }
            buffNode.AddChild(layoutItem); 
            layoutItem.MouseEntered += () =>
            {
                buffIntroNode.Visible = true;
                buffIntroNode.Position = GetLocalMousePosition();
                buffIntroLabel.Text = buff.intro;
            };
            layoutItem.MouseExited += () =>
            {
                buffIntroNode.Visible = false;
            };
        }
    }
}

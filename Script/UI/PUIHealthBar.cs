using Godot;
using System;
using Utils;

public partial class PUIHealthBar : UIHealthBar
{
    [Export] public Control smallTextPoint;

    [Export] public PackedScene buffLayoutItemPrefab;
    [Export] public Control buffNode;
    [Export] public Control buffIntroNode;
    [Export] public RichTextLabel buffIntroLabel;

    Game game => GameManager.Instance.game;
    Player player => game.player;

    public override void _Ready()
    {
        SetTarget(player);

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

        //玩家信号添加更新buffs
        player.OnAttachBuffSignal += UpdateBuffs;
        foreach (var child in buffNode.GetChildren())
        {
            child.QueueFree();
        }
    }

    public void UpdateBuffs(Buff _buff)
    {
        //Buff获取
        var buffs = player.buffs;
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

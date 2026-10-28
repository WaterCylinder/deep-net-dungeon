using Godot;
using System;

public partial class LockedServer : Node2D
{
    [Export] Sprite2D lockedSprite;
    [Export] Sprite2D brokenSprite;
    [Export] Area2D area;

    public bool locked = true;

    public override void _Ready()
    {
        base._Ready();
        area.BodyEntered += other =>
        {
            if(!locked)return;
            if(other is Player player)
            {
                Bag bag = player.GetComponent<Bag>();
                if(bag != null)
                {
                    if(bag.specialItemDict[ItemTag.KeyItem] != null)
                    {
                        if(bag.specialItemDict[ItemTag.KeyItem].ItemName == "Key")
                        {
                            //如果玩家有关键物品密钥，则触发特殊事件信号
                            Game game = GameManager.Instance.game;
                            if(game != null)
                            {
                                GD.Print();
                                EmitSignal(Game.SignalName.SpecialEvent, "UnlockServer");
                                //之后做服务器解锁动画，现在先把服务器设置为另一个sprite
                                locked = true;
                            }
                        }
                    }
                }
            }
        };
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (locked)
        {
            lockedSprite.Visible = true;
            brokenSprite.Visible = false;
            area.Visible = true;
        }
        else
        {
            lockedSprite.Visible = false;
            brokenSprite.Visible = true;
            area.Visible = false;
        }
    }


}

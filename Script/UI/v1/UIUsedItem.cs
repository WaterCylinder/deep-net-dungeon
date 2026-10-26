using Godot;
using System;

public partial class UIUsedItem : Control
{
    [Export] public TextureRect sprite;
    [Export] public ColorRect colorRect;
    [Export] public ColorRect endColor;
    [Export] public Label cd;
    [Export] public AudioStream skillR;

    public bool set = false;

    Player player => GameManager.Instance.game.player;

    public override void _Process(double delta)
    {
        if (Game.IsAble(player))
        {
            if (!set)
            {
                set = true;
                player.OnUseItem += (e, i) =>
                {
                    Vector2 pos = GlobalPosition + Size / 2;
                    GameManager.Instance.game.CreateParticles("item_use", player.GlobalPosition);
                };
            }
            Item item = player.usableItem;
            if(item != null)
            {
                Visible = true;
                sprite.Texture = item.data.sprite;
                float rate = player.usableItemCDNow / player.usableItemCD;
                if(rate == 0)
                {
                    if (!endColor.Visible)
                    {
                        GameManager.Instance.game.PlaySound(skillR);
                    }
                    endColor.Visible = true;
                    cd.Text = "";
                }
                else
                {
                    endColor.Visible = false;
                    cd.Text = player.usableItemCDNow.ToString("0.#");
                    colorRect.Scale = new Vector2(1, 1-rate);
                }
            }
            else
            {
                Visible = false;
            }
        }
        else
        {
            Visible = false;
        }
    }

}

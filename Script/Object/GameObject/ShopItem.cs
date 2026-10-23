using Godot;
using System;
/// <summary>
/// 商店物品
/// </summary>
public partial class ShopItem : Area2D
{
    [Export] public ItemPool pool;
    [Export] public ItemData data;
    [Export] public int price;
    [Export] public Sprite2D sprite;
    [Export] public Label priceLabel;
    [Export] public ColorRect colorRect;

    public override void _Ready()
    {
        base._Ready();
        BodyEntered += other =>
        {
            if(other is Player player)
            {
                if(player.score >= price)
                {
                    Buy(player);
                }
            }
        };

        data ??= pool.Pick();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if(data != null)
        {
            sprite.Texture = data.sprite;
            price = GetPrice(data);
            priceLabel.Text = price.ToString();
            colorRect.SelfModulate = Item.GetLevelColor(data.itemLevel);
        }
    }


    public void Buy(Player player)
    {
        player.score -= price;
        Item.CreateItem(data, GlobalPosition);
        QueueFree();
    }

    /// <summary>
    /// 获取道具价格，根据道具等级决定
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public int GetPrice(ItemData data)
    {
        return data.itemLevel switch
        {
            1 => 16,
            2 => 64,
            3 => 256,
            4 => 1024,
            _ => 1,
        };

    }
}

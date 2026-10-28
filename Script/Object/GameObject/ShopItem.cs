using Godot;
using System;
using System.Collections.Generic;
/// <summary>
/// 商店物品
/// </summary>
public partial class ShopItem : Area2D
{
    public static Dictionary<int, int> ItemLevelPriceMapping = new Dictionary<int, int>
    {
        {0, 1},
        {1, 16},
        {2, 64},
        {3, 256},
        {4, 1024},
    };

    [Export] public ItemPool pool;
    [Export] public ItemData data;
    [Export] public int price;
    [Export] public Sprite2D sprite;
    [Export] public Label priceLabel;
    [Export] public ColorRect colorRect;

    [Signal] public delegate void OnShopItemResetEventHandler();

    public int upPrice = 0;

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

        ResetData();
    }

    public void ResetData(ItemData data = null)
    {
        if(data == null)
        {
            this.data ??= pool.Pick();
        }
        else
        {
            this.data = data;
        }
        EmitSignal(SignalName.OnShopItemReset);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if(data != null)
        {
            sprite.Texture = data.sprite;
            price = GetPrice(data) + upPrice;
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
        if (ItemLevelPriceMapping.ContainsKey(data.itemLevel))
        {
            return ItemLevelPriceMapping[data.itemLevel];
        }
        else
        {
            return 0;
        }

    }
}

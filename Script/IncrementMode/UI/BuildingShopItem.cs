using Godot;
using System;
using System.Diagnostics.Tracing;

public partial class BuildingShopItem : Control
{
    [Export] public BuildingData data;
    [Export] public TextureButton button;
    [Export] public Label name;
    [Export] public TextureRect textureRect;
    [Export] public TextureRect rarity;
    [Export] public Label tag;
    [Export] public Label priceLabel;
    [Export] public RichTextLabel introLabel;

    [Export] public int price;

    Game game => GameManager.Instance.game;
    IncrementWorld world => game.incrementWorld;

    public void Init(BuildingData data)
    {
        this.data = data;

        name.Text = data.buildingName;
        textureRect.Texture = data.sprite;
        priceLabel.Text = data.buyPrice.ToString();
        rarity.SelfModulate = Building.rarityMapper[data.rarity];
        tag.Text = "";
        foreach(var t in data.tags)
        {
            if (Building.tagMapper.TryGetValue(t, out string value))
            {
                tag.Text += value + " ";
            }
        }
        price = data.buyPrice;
        introLabel.Text = data.GetDefaultIntro();

        button.SelfModulate = new Color("ffffff00");
        button.Pressed += () =>
        {
            world.Buy(this);
            game.PlaySound(world.ui.uiSound);
        };
    }

    public void Sell()
    {
        button.Disabled = true;
        button.SelfModulate = new Color("3b3b3bc5");
    }
}

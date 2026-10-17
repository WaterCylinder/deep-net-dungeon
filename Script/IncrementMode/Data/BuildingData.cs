using Godot;
using Godot.Collections;

[GlobalClass]
public partial class BuildingData : Resource
{
    [Export] public string id;
    [Export] public string buildingName;
    [Export] public int rarity;
    [Export] public string intro;
    [Export] public Array<BuildingTag> tags = new();
    [Export] public Dictionary<string, Variant> defaultArgs;
    [Export] public Value value = new(1);
    [Export] public Texture2D sprite;
    [Export] public SpriteFrames spriteFrames;

    [Export] public int buyPrice;
    [Export] public int sellPrice = -1;

    [Export] public Effect onBuy;
    [Export] public Effect onSet;
    [Export] public Effect onTurnStart;
    [Export] public Effect onUpdate;
    [Export] public Effect onTurnEnd;
    [Export] public Effect onSell;
    [Export] public Effect onPress;
    [Export] public Effect onLevelUp;
    [Export] public Effect onSomeBuildingBeSell;
    [Export] public Effect onSomeBuildingBeBuy;
    [Export] public Effect onShopRefresh;

    public string GetDefaultIntro()
    {
        string result = intro;
        foreach(var i in defaultArgs)
        {
            result = result.Replace($"{{{{{i.Key}}}}}", i.Value.ToString());
        }
        result = result.Replace("{{value}}", value.Get().ToString("#"));
        return result;
    }

    public int GetDefaultSellPrice()
    {
        if(sellPrice < 0)
        {
            return buyPrice / 2;
        }
        return sellPrice;
    }
}

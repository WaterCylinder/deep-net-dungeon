using Godot;
using System;

public partial class BuildingInfo : Control
{
    [Export] public Building building;
    [Export] public TextureRect texture;
    [Export] public Label nameLabel;
    [Export] public Label levelLabel;
    [Export] public RichTextLabel introLabel;
    [Export] public TextureRect rarity;
    [Export] public Label tag;
    [Export] public Button returnButton;
    [Export] public Button sellButton;
    [Export] public Label sellTips;
    [Export] public RichTextLabel logPanel;

    Game game => GameManager.Instance.game;
    IncrementWorld world => game.incrementWorld;

    public override void _Ready()
    {
        base._Ready();
        returnButton.Pressed += Close;
        sellButton.Pressed += () =>
        {
            //出售建筑
            world.Sell(building);
            Visible = false;
            game.PlaySound(world.ui.scoreSound);
        };
    }


    public void Init(Building building)
    {
        if(!Game.IsAble(building))
            return;

        this.building = building;

        texture.Texture = building.spriteNode.Texture;
        nameLabel.Text = building.name;
        levelLabel.Text = "等级" + building.level.ToString();
        introLabel.Text = building.GetIntro();
        sellTips.Text = $"出售获得 {building.sellPrice * building.level} ";

        rarity.SelfModulate = Building.rarityMapper[building.rarity];
        tag.Text = "";
        foreach(var t in building.tags)
        {
            if (Building.tagMapper.TryGetValue(t, out string value))
            {
                tag.Text += value + " ";
            }
        }
        
        string text = "";
        foreach(string str in building.log)
        {
            text += str + "\n";
        }
        logPanel.Text = text;
    }

    public void Update()
    {
        Init(building);
    }

    public void Close()
    {
        Visible = false;
    }
}

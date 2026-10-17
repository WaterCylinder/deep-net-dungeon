using Godot;
using System;

public partial class BuildPoint : Node2D
{
    [Export] public Vector2I pos;
    [Export] public Building buildingNow = null;
    [Export] public Node2D buildingNode;

    [Export] public Sprite2D mask;
    [Export] public TextureButton button;

    [Export] public Color green = new Color("04ff0020");
    [Export] public Color red = new Color("ff000020");

    Game game => GameManager.Instance.game;
    IncrementWorld world => game.incrementWorld;

    public override void _Ready()
    {
        button.SelfModulate = green;
        base._Ready();
        button.Pressed += OnPress;
    }


    public void SetBuilding(Building building)
    {
        mask.Visible = true;
        buildingNode.AddChild(building);
        building.Position = Vector2.Zero;
        building.pos = pos;
        buildingNow = building;
        building.point = this;
        button.SelfModulate = red;
        world.Update();
    }

    public void RemoveBuilding()
    {
        mask.Visible = false;
        buildingNow.QueueFree();
        buildingNow = null;
        button.SelfModulate = green;
        world.Update();
    }

    public void OnPress()
    {
        GD.Print($"点击建筑点{pos}， 内有建筑{buildingNow}");
        //shoping时无法点击
        if (world.shoping)
        {
            return;
        }
            
        world.Update();
        if (Game.IsAble(buildingNow))
        {
            buildingNow.OnPress();
            world.ui.info.Visible = true;
            world.ui.info.Init(buildingNow);
        }
        else
        {
            world.ui.Tips("拖动建筑到建筑点位来放置建筑");;
        }
        game.PlaySound(world.ui.uiSound);
    }

    public Building Check(BuildingTag tag)
    {
        if(tag == BuildingTag.Normal)
            return buildingNow;
        if(Game.IsAble(buildingNow) && buildingNow.tags.Contains(tag))
        {
            return buildingNow;
        }
        return null;
    }
}

using Godot;
using System;

public partial class BuildingPanelItem : Control
{
    [Export] public BuildingData data;
    [Export] public TextureButton button;

    Game game => GameManager.Instance.game;
    IncrementWorld world => game.incrementWorld;

    public override void _Ready()
    {
        button.ButtonDown += () =>
        {
            if (world.selectBuildingData == null)
            {
                GD.Print($"[BuildingPanel] 选择建筑项 {data.buildingName}");
                world.selectBuildingData = data;
                world.selectBuildingPanelItem = this;

                world.ui.buildingItemfollow.Size = Size;
                
                button.SelfModulate = new Color("ffffff00");

                game.PlaySound(world.ui.uiSound);
            }
        };

        button.ButtonUp += () =>
        {
            if(world.selectBuildingData != null)
            {
                bool set = false;
                InputManager input = InputManager.Instance;
                Vector2 mouse_pos = input.GetMouseWorldPosition();
                //松开建筑项
                if (!world.ui.buildingPanelNode.GetGlobalRect().HasPoint(input.GetMousePosition())
                && !world.shoping)
                {
                    foreach(var point in world.buildPointMap.allPoints)
                    {
                        if (point.button.GetGlobalRect().HasPoint(mouse_pos))
                        {
                            if(point.buildingNow != null)
                            {
                                if(point.buildingNow.name == world.selectBuildingData.buildingName)
                                {
                                    point.buildingNow.LevelUp();
                                    set = true;
                                }
                                break;
                            }
                            else
                            {
                                set = true;
                            }
                            Building.SetBuilding(point, world.selectBuildingData);
                            break;
                        }
                    }
                }
                
                if (set)
                {
                    world.selectBuildingPanelItem.QueueFree();
                }
                else
                {
                    GD.Print($"[BuildingPanel] 取消建筑项选择");
                }
                
                world.selectBuildingData = null;
                world.selectBuildingPanelItem = null;
                button.SelfModulate = new Color("ffffffff");
            }
        };
    }

    public void Init(BuildingData data)
    {
        if(data == null)
        {
            data = Building.DefaultData;
        }
        this.data = data;
        button.TextureNormal = data.sprite;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        InputManager input = InputManager.Instance;
        if (input.IsActionJustReleasedRaw(InputManager.Actions.OtherSkill))
        {
            Vector2 mouse_pos = input.GetMousePosition();
            if (button.GetGlobalRect().HasPoint(mouse_pos))
            {
                world.ui.buildingItemController.Visible = true;
                world.ui.buildingItemController.GlobalPosition = mouse_pos
                    - new Vector2(0, world.ui.buildingItemController.Size.Y);
                world.ui.buildingItemController.item = this;
                world.ui.buildingItemController.Init(data);
            }
        }
    }

}

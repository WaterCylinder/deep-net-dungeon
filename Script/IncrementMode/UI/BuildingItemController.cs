using Godot;
using System;

public partial class BuildingItemController : Control
{
    [Export] public BuildingPanelItem item;
    [Export] public Label name;
    [Export] public RichTextLabel intro;
    [Export] public Button sellButton;

    Game game => GameManager.Instance.game;
    IncrementWorld world => game.incrementWorld;

    public override void _Ready()
    {
        base._Ready();
        
        sellButton.Pressed += () =>
        {
            GD.Print("按下出售按钮");
            item.QueueFree();
            world.Sell(item.data);
            Visible = false;
            game.PlaySound(world.ui.scoreSound);
        };
    }

    public void Init(BuildingData data)
    {
        name.Text = data.buildingName;
        intro.Text = data.GetDefaultIntro();
        sellButton.Text = $"出售({data.GetDefaultSellPrice()})";
    }

    public override void _Process(double delta)
    {
        InputManager input = InputManager.Instance;
        if (input.IsActionJustPressedRaw(InputManager.Actions.ActiveSkill)
        || input.IsActionJustPressedRaw(InputManager.Actions.OtherSkill))
        {
            if (Visible)
            {
                Vector2 mouse_pos = input.GetMousePosition();
                if (!GetGlobalRect().HasPoint(mouse_pos))
                {
                    Visible = false;
                }
            }
            
        }
    }


}

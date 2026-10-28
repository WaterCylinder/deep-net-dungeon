using Godot;
using System;

public partial class UIItemInfoLayout : Control
{
    [Export] public ColorRect colorRect;
    [Export] public TextureRect textureRect;
    [Export] public Label name;
    [Export] public RichTextLabel intro;

    public ItemData data;
    Game game => GameManager.Instance.game;

    public void Init(ItemData data)
    {
        colorRect.SelfModulate = Item.GetLevelColor(data.itemLevel);
        textureRect.Texture = data.sprite;
        name.Text = data.showName;
        intro.Text = data.intro;
    }

    public override void _Process(double delta)
    {
        if(game == null)return;

        data = null;

        Vector2 mousePos = InputManager.Instance.GetMouseWorldPosition();
        var query = new PhysicsPointQueryParameters2D
        {
            Position = mousePos,
            CollideWithAreas = true,
            CollideWithBodies = false // 只查 Area，不查物理体
        };

        var results = GetWorld2D().DirectSpaceState.IntersectPoint(query, 32);

        foreach (var result in results)
        {
            var collider = result["collider"].As<Area2D>();
            if (collider != null)
            {
                if(collider is Item item)
                {
                    data = item.data;
                    break;
                }
                if(collider is ShopItem shopItem)
                {
                    data = shopItem.data;
                    break;
                }
            }
        }
        if (data != null)
        {
            Init(data);
            Visible = true;
            Vector2 pos = InputManager.Instance.GetMousePosition();
            Vector2 band = GetViewport().GetVisibleRect().Size - Size;
            if(pos.X > band.X)pos.X = band.X;
            if(pos.Y > band.Y)pos.Y = band.Y;
            GlobalPosition = pos;
        }
        else
        {
            Visible = false;
        }
    }



}

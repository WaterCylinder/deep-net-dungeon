using Godot;
using System;

public partial class ItemGenerator : Node2D
{
	[Export] public ItemData itemData;
	[Export] public bool GenerateImmediately = false;

	public override void _Ready()
    {
        //生成器在运行时不可见
        Visible = false;
        if (GenerateImmediately)
        {
            GenerateItem();
        }
    }

	public virtual Item GenerateItem(Node2D parent = null, bool del = true)
	{
		Item item = GenerateItem(itemData, parent);
		if(del)QueueFree();
		return item;
	}

	public Item GenerateItem(ItemData data, Node2D parent = null, Vector2 pos = default)
	{
		if(pos == default)
        {
            pos = GlobalPosition;
        }
		Item item = GameManager.Instance.game.SpawnItem(pos, data, parent);
		return item;
	}
}

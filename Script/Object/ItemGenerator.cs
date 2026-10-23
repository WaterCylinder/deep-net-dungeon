using Godot;
using System;

public partial class ItemGenerator : Node2D
{
	[Export] public ItemData itemData;
	[Export] public ItemPool pool;
	[Export] public bool GenerateImmediately = false;

	[Signal]
    public delegate void BeforeGenerateEventHandler(ItemData data);

    [Signal]
    public delegate void AfterGenerateEventHandler(Item enemy);

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
		if(itemData == null)
		{
			//没设定物品数据则在物品池中抽取，注意重复生成时该抽取的物品数据会保留。
			itemData = pool.Pick();
		}
		EmitSignal(SignalName.BeforeGenerate, itemData);
		Item item = GenerateItem(itemData, parent);
		EmitSignal(SignalName.AfterGenerate, item);
		if(del)QueueFree();
		return item;
	}

	public ItemData RePick()
	{
		itemData = pool.Pick();
		return itemData;
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

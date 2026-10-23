using System;
using Godot;
using Godot.Collections;

public partial class Bag : EntityComponent
{
    [Export] public Area2D area;
    public bool canPick = true;

    public delegate bool PickEventHandler(Item item);
    /// <summary>
    /// 拾取物品前判断，返回false则不进行拾取
    /// </summary>
    public PickEventHandler beforePick;
    public PickEventHandler onPick;

    [Signal]
    public delegate void OnUpdateEventHandler();
    [Signal]
    public delegate void OnPickItemEventHandler(ItemData itemdata);

    #region 背包数据

    [Export] public Dictionary<ItemData, int> itemDict;
    /// <summary>
    /// 特殊物品槽
    /// </summary>
    [Export] public Dictionary<ItemTag, ItemData> specialItemDict = new()
    {
        { ItemTag.BulletItem, null },
        { ItemTag.SpecialItem, null },
        { ItemTag.UsableItem, null },
    };
    [Export] public Array<Item> dropList = new();

    /// <summary>
    /// 添加道具，返回添加后的道具数量元组
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public (ItemData, int) AddItem(ItemData data, int count = 1)
    {
        int result = count;
        if (data.tags.Contains(ItemTag.BulletItem))
        {
            Drop(specialItemDict[ItemTag.BulletItem]);
            specialItemDict[ItemTag.BulletItem] = data;
        }
        else if (data.tags.Contains(ItemTag.SpecialItem))
        {
            Drop(specialItemDict[ItemTag.SpecialItem]);
            specialItemDict[ItemTag.SpecialItem] = data;
        }
        else if (data.tags.Contains(ItemTag.UsableItem))
        {
            Drop(specialItemDict[ItemTag.UsableItem]);
            specialItemDict[ItemTag.UsableItem] = data;
        }
        else if (itemDict.ContainsKey(data))
        {
            itemDict[data] += count;
            result = itemDict[data];
        }
        else
        {
            itemDict.Add(data, count);
        }
        Update();
        return (data, result);
    }

    /// <summary>
    /// 移除普通道具，返回是否移除成功
    /// </summary>
    /// <param name="data"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public bool RemoveItem(ItemData data, int count = 1)
    {
        if (itemDict.ContainsKey(data))
        {
            itemDict[data] -= count;
            if (itemDict[data] <= 0)
            {
                itemDict.Remove(data);
            }
            return true;
        }
        Update();
        return false;
    }

    public int Count(ItemData data)
    {
        if (itemDict.ContainsKey(data))
        {
            return itemDict[data];
        }
        return 0;
    }

    #endregion

    protected override void _Init()
    {
        area.AreaEntered += OnAreaEnter;
        area.AreaExited += OnAreaExit;
    }

    public void OnAreaEnter(Area2D other)
    {
        if(!canPick)return;
        if(other is Item item)
        {
            //如果在刚才丢弃物品列表里，则不拾取
            if(dropList.Contains(item))
            {
                return;
            }

            if(beforePick != null && !beforePick.Invoke(item))
            {
                return;
            }

            //检测最大堆叠数量
            if(item.data.maxCount > 0 && Count(item.data) >= item.data.maxCount)
            {
                return;
            }

            Pick(item);
            onPick?.Invoke(item);
            item.Pick(entity);
        }
    }

    public void OnAreaExit(Area2D other)
    {
        if(other is Item item)
        {
            dropList.Remove(item);
        }
    }

    public void Update()
    {
        EmitSignal(SignalName.OnUpdate);
    }

    public void Drop(ItemData data)
    {
        if(data == null)return;
        Item item = Item.DropItem(data, GlobalPosition, entity);
        dropList.Add(item);
    }

    public void Pick(Item item)
    {
        AddItem(item.data);
        EmitSignal(SignalName.OnPickItem, item.data);
    }

}

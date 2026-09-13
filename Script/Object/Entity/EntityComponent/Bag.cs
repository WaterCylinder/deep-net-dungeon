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

    #region 背包数据

    [Export] public Dictionary<ItemData, int> itemDict;

    /// <summary>
    /// 添加道具，返回添加后的道具数量元组
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public (ItemData, int) AddItem(ItemData data, int count = 1)
    {
        if (itemDict.ContainsKey(data))
        {
            itemDict[data] += count;
        }
        else
        {
            itemDict.Add(data, count);
        }
        return (data, itemDict[data]);
    }

    /// <summary>
    /// 移除道具，返回是否移除成功
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
    }

    public void OnAreaEnter(Area2D other)
    {
        if(!canPick)return;
        if(other is Item item)
        {
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

    public void Pick(Item item)
    {
        AddItem(item.data);
    }

}

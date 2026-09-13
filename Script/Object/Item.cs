using Godot;
using Godot.Collections;

public enum ItemTag
{
    None,
    Coin,
    NormalItem,
    KeyItem,
}

/// <summary>
/// Item类，继承自Area2D，表示游戏中的物品/掉落物
/// </summary>
public partial class Item : Area2D
{
    #region 默认数据

    // 默认数据区域，存储物品的默认数据
    /// <summary>
    /// 默认物品数据
    /// </summary>
    private static ItemData _defaultData;

    /// <summary>
    /// 获取默认物品数据的属性
    /// 使用单例模式，确保只加载一次
    /// </summary>
    public static ItemData DefaultData
    {
        get
        {
            if (_defaultData == null)
            {
                // 从资源管理器加载默认物品数据
                _defaultData = AssetManager.Instance.GetDefault<ItemData>("Data/item_data.tres");
                if (_defaultData == null)
                {
                    // 如果加载失败，打印错误信息并创建新的默认数据
                    GD.PrintErr("未找到默认敌人数据！");
                    _defaultData = new ItemData();
                }
            }
            return _defaultData;
        }
    }

    #endregion

    #region 工厂方法

    // 工厂方法区域，用于创建和生成物品实例
    /// <summary>
    /// 物品的预制体
    /// </summary>
    private static PackedScene _itemPrefab = null;

    /// <summary>
    /// 获取物品预制体的属性
    /// 使用单例模式，确保只加载一次
    /// </summary>
    public static PackedScene ItemPrefab
    {
        get
        {
            if (_itemPrefab == null)
            {
                // 从资源管理器加载预制体
                _itemPrefab = AssetManager.Instance.GetPrefab("Asset.Object.item", null);
            }
            return _itemPrefab;
        }
    }

    /// <summary>
    /// 创建一个掉落物
    /// </summary>
    /// <param name="data"></param>
    /// <param name="pos"></param>
    /// <param name="parent"></param>
    /// <returns></returns>
    public static Item CreateItem(ItemData data, Vector2 pos, Node2D parent = null)
    {
        Item item = ItemPrefab.Instantiate() as Item;

        if(parent == null)
        {
            parent = GameManager.Instance.game.dropItemNode;
        }

        item.GlobalPosition = pos;
        parent.AddChild(item);
        item.Initialize(data);

        return item;
    }

    /// <summary>
    /// 丢弃一个掉落物
    /// </summary>
    /// <param name="data"></param>
    /// <param name="pos"></param>
    /// <param name="droper"></param>
    /// <returns></returns>
    public static Item DropItem(ItemData data, Vector2 pos, Entity droper = null)
    {
        Item item = CreateItem(data, pos);
        item.Drop(droper);
        return item;
    }

    #endregion

    #region 道具属性
    [Export] public ItemData data;
    /// <summary>
    /// 放置在场景中时立刻初始化，用于测试以及房间固有掉落物
    /// </summary>
    [Export] public bool initImmediately;

    [Export] public string ItemName;
    [Export] public string intro;
    [Export] public Array<ItemTag> tags = new();
    [Export] public int itemLevel;

    [Export] public Behavior onInit;
    [Export] public Behavior onPick;
    [Export] public Behavior onUse;
    [Export] public Behavior onRemove;
    [Export] public Behavior onDrop;
    [Export] public Behavior onUpdate;

    #endregion

    #region 标签管理

    /// <summary>
    /// 检查掉落物是否有指定标签
    /// </summary>
    /// <param name="tag">要检查的标签</param>
    /// <returns>是否拥有该标签</returns>
    public bool CheckTag(ItemTag tag)
    {
        return tags.Contains(tag);
    }

    /// <summary>
    /// 添加掉落物标签
    /// </summary>
    /// <param name="tag">要添加的标签</param>
    public void AddTag(ItemTag tag)
    {
        if (!tags.Contains(tag))
        {
            tags.Add(tag);
        }
    }

    /// <summary>
    /// 移除掉落物标签
    /// </summary>
    /// <param name="tag">要移除的标签</param>
    public void RemoveTag(ItemTag tag)
    {
        if (tags.Contains(tag))
        {
            tags.Remove(tag);
        }
    }

    #endregion

    #region 信号

    [Signal] public delegate void OnPickSignalEventHandler(Entity Picker);
    [Signal] public delegate void OnDropSignalEventHandler(Entity droper);
    [Signal] public delegate void OnRemoveSignalEventHandler();
    [Signal] public delegate void OnUseSignalEventHandler(Entity user);

    #endregion

    #region 初始化

    public void Initialize(ItemData data)
    {
        if (data == null)
        {
            data = DefaultData;
            GD.Print($"掉落物使用默认数据初始化");
        }

        this.data = data;


        ItemName = data.ItemName;
        Name = data.showName + Time.GetUnixTimeFromSystem();
        intro = data.intro;
        foreach (var tag in data.tags)
        {
            AddTag(tag);
        }
        itemLevel = data.itemLevel;

        onPick = data.onPick;
        onUse = data.onUse;
        onDrop = data.onDrop;
        onRemove = data.onRemove;
        onUpdate = data.onUpdate;

        Sprite2D spriteNode = GetNodeOrNull<Sprite2D>("Sprite2D");
        if (spriteNode != null)
        {
            if(data.sprite != null)
            {
                spriteNode.Texture = data.sprite;
            }
        }

        AnimatedSprite2D animeNode = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
        if (animeNode != null)
        {
            if(data.spriteFrames != null)
            {
                animeNode.SpriteFrames = data.spriteFrames;
                if (spriteNode != null)
                {
                    spriteNode.Texture = null;
                }
            }
        }

        onInit?.Run();

    }

    #endregion

    #region 生命周期

    public override void _Ready()
    {
        base._Ready();
        if (initImmediately)
        {
            Initialize(this.data);
        }
    }

    public override void _Process(double delta)
    {
        onUpdate?.Run((float)delta);
    }

    #endregion

    #region 工具方法

    public void Pick(Entity picker)
    {
        onPick?.Run(picker);
        EmitSignal(nameof(OnPickSignal), picker);
        ///被拾取时移除自身
        QueueFree();
    }

    public void Use(Entity user)
    {
        onPick?.Run(user);
        EmitSignal(nameof(OnUseSignal), user);
    }

    public void Remove()
    {
        onRemove?.Run();
        EmitSignal(nameof(OnRemoveSignal));
    }

    public void Drop(Entity droper)
    {
        onDrop?.Run(droper);
        EmitSignal(nameof(OnDropSignal), droper);
    }

    #endregion
}

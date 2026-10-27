using Godot;
using Godot.Collections;

public enum ItemTag
{
    None,
    EnergyPoint,
    NormalItem,
    KeyItem,
    BulletItem,
    SpecialItem,
    UsableItem,
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
    public static Item CreateItem(ItemData data, Vector2 pos, Node parent = null, bool interactive = true)
    {
        Item item = ItemPrefab.Instantiate() as Item;

        item.Interactive = interactive;

        if(parent == null)
        {
            parent = GameManager.Instance.game.dropItemNode;
        }

        Callable.From(() =>
        {
            parent.AddChild(item);
            item.GlobalPosition = pos;
        }).CallDeferred();
        
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

    #region 映射表

    public static Dictionary<ItemTag, string> ItemTagMapping = new Dictionary<ItemTag, string>()
    {
        {ItemTag.EnergyPoint, "能量点"},
        {ItemTag.BulletItem, "弹幕道具"},
        {ItemTag.KeyItem, "关键道具"},
        {ItemTag.NormalItem, "通常道具"},
        {ItemTag.SpecialItem, "特殊道具"},
        {ItemTag.UsableItem, "主动道具"},
    };

    /// <summary>
    /// 道具等级 → 稀有度颜色。
    /// 不是"把纯色降低饱和度"，而是按设计色取相：1 级天青、2 级淡紫、3 级琥珀金、4 级珊瑚红，
    /// 色相 187°→263°→40°→359°，饱和度 40%~55%、明度统一在 95% 的高亮粉彩区间。
    /// 明度已经顶到 95%（再高就往纯白走了，颜色会互相分不开），
    /// 因为色块还要被 Spiral 着色器乘一次 0.72，留这一档余量后游戏内观感才够亮、又不至于糊成霓虹。
    /// 4 级红的饱和度单独给到 55%：高亮下红色的彩色感掉得最快，不加会变粉。
    /// </summary>
    public static Dictionary<int, Color> ItemLevelColorMapping = new Dictionary<int, Color>()
    {
        // 1 级：天青
        {1, new Color("79e4f2")},
        // 2 级：淡紫
        {2, new Color("b691f2")},
        // 3 级：琥珀金
        {3, new Color("f2ca7b")},
        // 4 级：珊瑚红
        {4, new Color("f26d6f")},
    };

    public static Color GetLevelColor(int level)
    {
        if (ItemLevelColorMapping.ContainsKey(level))
        {
            return ItemLevelColorMapping[level];
        }
        else
        {
            return Colors.Gray;
        }
    }

    #endregion

    #region 道具属性
    [Export] public ItemData data;
    /// <summary>
    /// 放置在场景中时立刻初始化，用于测试以及房间固有掉落物
    /// </summary>
    [Export] public bool initImmediately;

    [Export] public Args args = new();

    [Export] public string ItemName;
    [Export] public string intro;
    [Export] public Array<ItemTag> tags = new();
    [Export] public int itemLevel;
    /// <summary>
    /// 是否可交互（可以被拾取）
    /// </summary>
    [Export] public bool Interactive = true;

    /// <summary>
    /// 携带的Buff，value为挂载层数
    /// </summary>
    [Export] public Dictionary<BuffData, int> carryBuffs = new();

    /// <summary>
    /// 拾取时是否给拾取者挂载carryBuffs
    /// </summary>
    [Export] public bool addBuffOnPick = false;

    /// <summary>
    /// 丢弃时是否移除拾取者身上的carryBuffs
    /// </summary>
    [Export] public bool removeBuffOnDrop = false;

    [Export] public Behavior onInit;
    [Export] public Behavior onPick;
    [Export] public Behavior onUse;
    [Export] public Behavior onRemove;
    [Export] public Behavior onDrop;
    [Export] public Behavior onUpdate;

    [Export] AudioStream pickSound;

    public Sprite2D spriteNode;
    public AnimatedSprite2D animNode;

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
        args.Init(data.args);
        itemLevel = data.itemLevel;

        carryBuffs = data.carryBuffs;
        addBuffOnPick = data.addBuffOnPick;
        removeBuffOnDrop = data.removeBuffOnDrop;

        onInit = data.onInit;
        onPick = data.onPick;
        onUse = data.onUse;
        onDrop = data.onDrop;
        onRemove = data.onRemove;
        onUpdate = data.onUpdate;

        spriteNode = GetNodeOrNull<Sprite2D>("Sprite2D");
        animNode = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");

        if (spriteNode != null)
        {
            if(data.sprite != null)
            {
                spriteNode.Texture = data.sprite;
            }
        }

        if (animNode != null)
        {
            if(data.spriteFrames != null)
            {
                animNode.SpriteFrames = data.spriteFrames;
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

        //设为不可交互
        if (!Interactive)
        {
            Monitoring = false;
            Monitorable = false;
            Visible = false;
            //只关 monitorable 不够：道具是"加入场景树时就和拾取范围重叠"的（例如挂在玩家身上的手持道具），
            //这一帧引擎仍可能让拾取范围收到一次 area_entered，进而被当成第二次拾取。
            //这里连碰撞层和形状一起关掉，保证它永远不会被任何 Area2D 检测到
            CollisionLayer = 0;
            CollisionMask = 0;
            CollisionShape2D shape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
            if (shape != null)
            {
                shape.Disabled = true;
            }
        }

        float FloatHeight = 10f;   // 浮动幅度（像素）
        float Duration = 1.0f;     // 单程时间（秒）

        Tween tween = CreateTween();
        tween.SetLoops(); // 无限循环

        // 向上移动 FloatHeight
        tween.TweenProperty(spriteNode, "position:y", spriteNode.Position.Y - FloatHeight, Duration)
             .SetTrans(Tween.TransitionType.Sine)
             .SetEase(Tween.EaseType.InOut);

        // 回到原来的 Y
        tween.TweenProperty(spriteNode, "position:y", spriteNode.Position.Y, Duration)
             .SetTrans(Tween.TransitionType.Sine)
             .SetEase(Tween.EaseType.InOut);
    }

    public override void _Process(double delta)
    {
        onUpdate?.Run((float)delta);
    }

    #endregion

    #region 工具方法

    public void Pick(Entity picker)
    {
        if (addBuffOnPick && Game.IsAble(picker))
        {
            AddCarryBuffs(picker);
        }
        onPick?.Run(this, picker);
        EmitSignal(nameof(OnPickSignal), picker);
        GameManager.Instance.game.PlaySound(pickSound);
        ///被拾取时移除自身
        QueueFree();
    }

    public void Use(Entity user)
    {
        onUse?.Run(this, user);
        EmitSignal(nameof(OnUseSignal), user);
    }

    public void Remove()
    {
        onRemove?.Run();
        EmitSignal(nameof(OnRemoveSignal));
    }

    public void Drop(Entity droper)
    {
        if (removeBuffOnDrop && Game.IsAble(droper))
        {
            RemoveCarryBuffs(droper);
        }
        onDrop?.Run(this, droper);
        EmitSignal(nameof(OnDropSignal), droper);
    }

    /// <summary>
    /// 给实体挂载carryBuffs中的全部Buff
    /// </summary>
    /// <param name="target">目标实体</param>
    public void AddCarryBuffs(Entity target)
    {
        if (carryBuffs == null)
        {
            return;
        }
        foreach (var pair in carryBuffs)
        {
            if (pair.Key == null)
            {
                continue;
            }
            target.AttachBuff(pair.Key, Mathf.Max(pair.Value, 1));
        }
    }

    /// <summary>
    /// 从实体身上按层数移除carryBuffs中的Buff，层数归零时由Entity自动移除该Buff
    /// </summary>
    /// <param name="target">目标实体</param>
    public void RemoveCarryBuffs(Entity target)
    {
        if (carryBuffs == null)
        {
            return;
        }
        foreach (var pair in carryBuffs)
        {
            if (pair.Key == null || string.IsNullOrEmpty(pair.Key.id))
            {
                continue;
            }
            Buff buff = target.GetBuff(pair.Key.id);
            if (buff == null)
            {
                //目标身上没有该Buff，跳过
                continue;
            }
            target.ChangeBuffStack(buff, -Mathf.Max(pair.Value, 1));
        }
    }

    #endregion
}

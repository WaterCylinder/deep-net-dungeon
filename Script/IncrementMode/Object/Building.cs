using Godot;
using Godot.Collections;

public enum BuildingTag
{
    Normal,
    /// <summary>
    /// 核心
    /// </summary>
    Core,
    /// <summary>
    /// 机器
    /// </summary>
    Machine,
    /// <summary>
    /// 化工
    /// </summary>
    Chemical,
    /// <summary>
    /// 算力
    /// </summary>
    Computational,
}

public partial class Building : Node2D
{
    #region 默认数据

    /// <summary>
    /// 默认建筑数据缓存
    /// </summary>
    private static BuildingData _defaultData = null;

    /// <summary>
    /// 获取默认建筑数据（从Asset/Default/Data/enemy_data.tres加载）
    /// </summary>
    public static BuildingData DefaultData
    {
        get
        {
            if (_defaultData == null)
            {
                _defaultData = AssetManager.Instance.GetDefault<BuildingData>("Data/building_data.tres");
                if (_defaultData == null)
                {
                    GD.PrintErr("未找到默认建筑数据！");
                    _defaultData = new BuildingData();
                }
            }
            return _defaultData;
        }
    }

    #endregion

    #region 工厂方法

    /// <summary>
    /// 建筑预制体缓存
    /// </summary>
    private static PackedScene _buildingPrefab = null;
    
    /// <summary>
    /// 建筑预制体
    /// </summary>
    public static PackedScene BuildingPrefab
    {
        get
        {
            if (_buildingPrefab == null)
            {
                _buildingPrefab = AssetManager.Instance.GetPrefab("Asset.Object.building", null);
            }
            return _buildingPrefab;
        }
    }

    /// <summary>
    /// 在建筑点创建建筑
    /// </summary>
    /// <param name="point"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public static Building SetBuilding(BuildPoint point, BuildingData data)
    {
        if(point.buildingNow != null)
        {
            GD.PrintErr("创建建筑：点位已存在建筑");
            return null;
        }

        // 如果数据为空，使用默认数据
        if (data == null)
        {
            GD.PrintErr("创建建筑：使用默认数据");
            data = DefaultData;
        }

        if (BuildingPrefab == null)
        {
            GD.PrintErr("无法创建建筑：预制体未找到");
            return null;
        }

        Building building = BuildingPrefab.Instantiate<Building>();

        point.SetBuilding(building);
        
        building.Initialize(data);

        building.OnSet();

        GD.Print($"在{point.pos}放置建筑[{building}]");

        return building;
    }

    #endregion

    #region 标签和稀有度映射表

    public static Dictionary<int, Color> rarityMapper = new Dictionary<int, Color>
    {
        {0, Colors.White},
        {1, Colors.Blue},
        {2, Colors.Purple},
        {3, Colors.Gold},
    };

    public static Dictionary<BuildingTag, string> tagMapper =new Dictionary<BuildingTag, string>
    {
        {BuildingTag.Core, "核心"},
        {BuildingTag.Computational, "算力"},
        {BuildingTag.Machine, "机械"},
        {BuildingTag.Chemical, "化工"},
    };

    #endregion

    #region 属性

    [Export] public BuildingData data;
    [Export] public string id;
    [Export] public string name;
    [Export] public int rarity;
    [Export] public string intro;
    [Export] public Vector2I pos;

    [Export] public Array<BuildingTag> tags = new();

    [Export] public int sellPrice = 0;

    [Export] public Effect onBuy;
    [Export] public Effect onSet;
    [Export] public Effect onTurnStart;
    [Export] public Effect onUpdate;
    [Export] public Effect onTurnEnd;
    [Export] public Effect onSell;
    [Export] public Effect onPress;
    [Export] public Effect onLevelUp;
    [Export] public Effect onSomeBuildingBeSell;
    [Export] public Effect onSomeBuildingBeBuy;
    [Export] public Effect onShopRefresh;

    [Export] public Args args = new();

    /// <summary>
    /// 基础数值
    /// </summary>
    [Export] public Value value = new(1);
    [Export] public int level = 1;

    [Export] public BuildPoint point;

    public System.Collections.Generic.Queue<string> log = new();

    public Node2D layout;
    public Sprite2D spriteNode;
    public AnimatedSprite2D animNode;

    #endregion

    #region 信号

    [Signal] public delegate void OnSetSignalEventHandler(Building building, Vector2 pos);
    [Signal] public delegate void OnPressSignalEventHandler(Building building);
    [Signal] public delegate void OnSellSignalEventHandler(Building building);

    #endregion

    Game game => GameManager.Instance.game;
    IncrementWorld world => game.incrementWorld;

    #region 初始化

    public void Initialize(BuildingData data)
    {
        this.data = data;
        id = data.id;
        Name = data.buildingName;
        name = data.buildingName;
        rarity = data.rarity;
        intro = data.intro;

        //卖出返还按"实际买入价"的一半算（含阶段通胀）：
        //这样后期把旧建筑拆掉换新构筑不吃亏，但"买了再卖"永远亏一半，反复买卖赚不到钱
        int paidPrice = data.buyPrice;
        if (world != null)
        {
            paidPrice = world.GetPrice(data);
        }
        sellPrice = data.sellPrice;
        if(sellPrice < 0)
        {
           sellPrice = Mathf.FloorToInt(paidPrice * 0.5f);
        }

        onBuy = data.onBuy?.Duplicate() as Effect;
        onSell = data.onSell?.Duplicate() as Effect;
        onSet = data.onSet?.Duplicate() as Effect;
        onTurnEnd = data.onTurnEnd?.Duplicate() as Effect;
        onUpdate = data.onUpdate?.Duplicate() as Effect;
        onTurnStart = data.onTurnStart?.Duplicate() as Effect;
        onPress = data.onPress?.Duplicate() as Effect;
        onLevelUp = data.onLevelUp?.Duplicate() as Effect;
        onSomeBuildingBeSell = data.onSomeBuildingBeSell?.Duplicate() as Effect;
        onSomeBuildingBeBuy = data.onSomeBuildingBeBuy?.Duplicate() as Effect;
        onShopRefresh = data.onShopRefresh?.Duplicate() as Effect;

        value.SetBase(data.value.Get());

        args = new();
        args.Init(data.defaultArgs);

        foreach(BuildingTag tag in data.tags)
        {
            tags.Add(tag);
        }

        if(data.sprite != null)
        {
            spriteNode.Texture = data.sprite;
        }

        if(data.spriteFrames != null)
        {
            animNode.SpriteFrames = data.spriteFrames;
            spriteNode.Visible = false;
        }
    }

    #endregion

    #region 生命周期

    public override void _Ready()
    {
        base._Ready();
        layout = GetNodeOrNull("Layout") as Node2D;
        spriteNode = layout.GetNodeOrNull<Sprite2D>("Sprite2D");
        animNode = layout.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
    }


    public void OnSet()
    {
        onSet?.Run(this, pos);
        EmitSignal(SignalName.OnSetSignal, this, pos);
        Log("该建筑被放置。");
    }

    public void OnPress()
    {
        onPress?.Run(this);
        EmitSignal(SignalName.OnPressSignal, this);
    }

    /// <summary>
    /// 出售时触发
    /// </summary>
    /// <param name="allowEffect">是否触发本建筑的出售效果（联动限次时传false，建筑仍会被移除）</param>
    public void OnSell(bool allowEffect = true)
    {
        if (allowEffect)
        {
            onSell?.Run(this);
        }
        EmitSignal(SignalName.OnSellSignal, this);
        if(point != null)
        {
            point.RemoveBuilding();
        }
    }

    #endregion

    #region 工具方法

    public Array<Building> GetAround4(BuildingTag tag = BuildingTag.Normal)
    {
        Array<Vector2I> tlist =[Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right];

        Array<Building> result = new();

        foreach(var t in tlist)
        {
            var p = world.Map(pos + t);
            if(p == null)continue;
            var b = p.Check(tag);
            if(b != null)result.Add(b);
        }

        return result;
    }

    #endregion

    public void LevelUp()
    {
        level += 1;
        Log($"建筑由 {level - 1} 级升到 {level} 级");
        onLevelUp?.Run(this);
        SmallText.CreateSmallText(
            SmallTextType.ValueChangeShow,
            GlobalPosition,
            "↑升级↑",
            Colors.Green
        );
    }

    /// <summary>
    /// 获取描述，自动匹配变量传入
    /// </summary>
    /// <returns></returns>
    public string GetIntro()
    {
        string result = intro;
        args.ForEach( (k,v) =>
        {
            result = result.Replace($"{{{{{k}}}}}", v.ToString());
        });
        result = result.Replace("{{value}}", value.Get().ToString("#"));
        return result;
    }

    //日志保存到对象里，可以在游戏里查看
    public void Log(string text)
    {
        log.Enqueue($"[回合{world.turn}] " + text);
    }

    public override string ToString()
    {
        return $"[{name}]";
    }

}

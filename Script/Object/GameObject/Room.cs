using Godot;
using Godot.Collections;

public enum RoomTag
{
    Normal,
    Elite,
    Boss,
    Rest,
    Shop,
    Treasure,
    Event,
    Hidden
}

public partial class Room : Node2D
{
    #region 工厂方法

    private static PackedScene _roomPrefab = null;

    public static PackedScene RoomPrefab
    {
        get
        {
            if (_roomPrefab == null)
            {
                _roomPrefab = AssetManager.Instance.GetPrefab("Asset.Object.room", null);
            }
            return _roomPrefab;
        }
    }

    /// <summary>
    /// 创建房间实例
    /// </summary>
    /// <param name="position">房间位置（世界坐标）</param>
    /// <param name="data">房间数据</param>
    /// <param name="parent">父节点（默认添加到当前场景）</param>
    /// <param name="level">当前层数</param>
    /// <returns>创建的Room实例</returns>
    public static Room Create(Vector2 position, RoomData data, Node parent = null, int level = 1)
    {
        if (data == null)
        {
            data = DefaultData;
        }

        if (RoomPrefab == null)
        {
            GD.PrintErr("无法创建房间：Room预制体未找到");
            return null;
        }

        Room room = RoomPrefab.Instantiate<Room>();
        if (room == null)
        {
            GD.PrintErr("无法创建房间：实例化失败");
            return null;
        }

        room.Position = position;
        room.level = level;
        room.Initialize(data);

        if (parent == null)
        {
            parent = Engine.GetMainLoop() is SceneTree sceneTree ? sceneTree.CurrentScene : null;
        }
        parent.CallDeferred("add_child", room);

        return room;
    }

    #endregion

    #region 默认数据

    /// <summary>
    /// 默认房间数据缓存
    /// </summary>
    private static RoomData _defaultData = null;

    /// <summary>
    /// 获取默认房间数据（从Asset/Default/Data/room_data.tres加载）
    /// </summary>
    public static RoomData DefaultData
    {
        get
        {
            if (_defaultData == null)
            {
                _defaultData = AssetManager.Instance.GetDefault<RoomData>("Data/room_data.tres");
                if (_defaultData == null)
                {
                    GD.PrintErr("未找到默认房间数据！");
                    _defaultData = new RoomData();
                }
            }
            return _defaultData;
        }
    }

    #endregion

    #region 房间属性
    /// <summary>
    /// 子节点
    /// </summary>
    [Export] public Node2D enemyNode;
    [Export] public Node2D doorNode;

    /// <summary>
    /// 房间数据源
    /// </summary>
    [Export] public RoomData data;

    [Export] public Array<RoomTag> tags = new();

    /// <summary>
    /// 当前层数
    /// </summary>
    [Export] public int level = 1;

    [Export] RoomTileMap tileMap = null;


    #endregion

    #region 初始化

    /// <summary>
    /// 使用RoomData初始化房间
    /// </summary>
    /// <param name="data">房间数据</param>
    public void Initialize(RoomData data)
    {
        if (data == null)
        {
            GD.PrintErr("房间初始化失败：RoomData为空");
            return;
        }

        this.data = data;
        Name = data.roomId;

        // 实例化TileMap
        if (data.tileMap != null)
        {
            //移除演示的TileMap
            foreach(Node child in GetChildren())
            {
                if(child is TileMapLayer)
                {
                    child.QueueFree();
                }
            }
            tileMap = data.tileMap.Instantiate<RoomTileMap>();
            AddChild(tileMap);
            MoveChild(tileMap, 0);
            // 初始化tilemap
            tileMap.Initialize(this);
        }
        else
        {
            GD.Print("房间生成：未检测到地形资源");
        }

        tags = data.roomTags.Duplicate();

    }

    /// <summary>
    /// 房间进入时
    /// </summary>
    public void OnEnter()
    {
        //默认进入时刷怪
        tileMap.SpawnAllEnemy();
    }

    #endregion
}
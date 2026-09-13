using System.Linq;
using Godot;
using Godot.Collections;

public enum RoomTag
{
    /// <summary>
    /// 起始房间
    /// </summary>
    Start,
    /// <summary>
    /// 普通房间
    /// </summary>
    Normal,
    /// <summary>
    /// BOSS房间
    /// </summary>
    Boss,
    /// <summary>
    /// 商店房间
    /// </summary>
    Shop,
    /// <summary>
    /// 资源房间
    /// </summary>
    Treasure,
    /// <summary>
    /// 事件房间
    /// </summary>
    Event,
    /// <summary>
    /// 隐藏房间
    /// </summary>
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

    public Effect onCreate;
    public Effect onEnter;
    public Effect onExit;
    public Effect onUpdate;

    /// <summary>
    /// 房间是否已被清理过
    /// </summary>
    public bool isCleared = false;

    public Array<Door> nextDoors = new();
    public Door returnDoor;
    public Array<Door> otherDoors = new();
    /// <summary>
    /// 上一个房间，可通过返回门返回
    /// </summary>
    public Room preRoom = null;

    int A = -1;


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

        GD.Print($"房间生成：{data}");

        this.data = data;
        Name = string.IsNullOrEmpty(data.roomId) ? "Room" : data.roomId;
        
        onCreate = data.onCreate?.Duplicate() as Effect;
        onEnter = data.onEnter?.Duplicate() as Effect;
        onExit = data.onExit?.Duplicate() as Effect;
        onUpdate = data.onUpdate?.Duplicate() as Effect;

        // 初始化门

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
    /// 给下一个NextDoor设置房间，在地图生成时调用以链接房间
    /// </summary>
    /// <param name="room"></param>
    /// <param name="targetReturn">是否传送到返回门处</param>
    /// <returns>设置成功返回true，没有更多NextDoor了返回false</returns>
    public bool SetNextRoom(Room room, bool targetReturn = true)
    {
        foreach(Door door in nextDoors)
        {
            if(door.targetRoom == null)
            {
                door.targetRoom = room;
                if(targetReturn && room.returnDoor != null)
                {
                    door.targetDoor = room.returnDoor;
                }
                return true;
            }
        }
        return false;
    }

    #endregion

    # region 房间流程

    public override void _Process(double delta)
    {
        base._Process(delta);
        onUpdate?.Run(this);
        switch(A){
            case 0:
                //流程初始化
                if(isCleared)
                {
                    A = 3;
                }
                else
                {
                    A = 1;
                }
                break;
            case 1:
                //房间状态修改
                CloseAllDoor();
                A = 2;
                break;
            case 2:
                //生成所有波次的敌怪
                if(tileMap.CheckEnemiesAllDead())
                {
                    if (! tileMap.SpawnEnemyNext())
                    {
                        A = 3;
                    }
                }
                break;
            case 3:
                //房间清理完毕，开门
                OpenAllDoor();
                isCleared = true;
                A = -1;
                break;
        }
    }

    # endregion

    #region 暴露方法

    public void Exit(Entity entity)
    {
        entity.room = null;
    }

    public void Enter(Entity entity)
    {
        //设置返回门目标为实体的上一个房间
        preRoom = entity.room;
        if(returnDoor != null && preRoom != null)
        {
            returnDoor.targetRoom = preRoom;
        }
        //实体将进入当前房间
        entity.room = this;
        OnEnter();
    }

    public void OpenAllDoor()
    {
        foreach (Door door in doorNode.GetChildren().Cast<Door>())
        {
            door.Open();
        }
    }

    public void CloseAllDoor()
    {
        foreach (Door door in doorNode.GetChildren().Cast<Door>())
        {
            door.Close();
        }
    }

    #endregion

    #region 事件方法

    /// <summary>
    /// 房间进入时
    /// </summary>
    protected void OnEnter()
    {
        //玩家进入后开始房间流程
        onEnter?.Run(this);
        A = 0;
        //仅作测试，开启所有门
        OpenAllDoor();
    }

    #endregion
}
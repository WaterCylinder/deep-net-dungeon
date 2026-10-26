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

    [Export] public RoomTileMap tileMap = null;
    public Array<Enemy> enemiesNow => tileMap.enemiesNow;

    [Export] public string roomTitle = "None Name Room";
    [Export] public Args args = new();

    public Effect onCreate;
    public Effect onFirstEnter;
    public Effect onGenerateEnemy;
    public Effect onGenerateItem;
    public Effect onEnter;
    public Effect onExit;
    public Effect onUpdate;
    public Effect onClear;

    /// <summary>
    /// 玩家是否已经进入过该房间
    /// </summary>
    public bool alreadyEntered = false;
    /// <summary>
    /// 玩家是否已经清理过该房间
    /// </summary>
    public bool alreadyCleared = false;

    /// <summary>
    /// 房间是否已被清理过
    /// </summary>
    public bool isCleared = false;
    public bool useDefaultRoomProcess = true;

    public Array<Door> nextDoors = new();
    /// <summary>
    /// 返回门（兼容旧数据：多个入口门时取第一个）。
    /// 新逻辑请用returnDoors：房间可以有多个入口，每扇入口门各自记录自己的targetDoor
    /// </summary>
    public Door returnDoor;
    /// <summary>
    /// 所有入口门（DoorType.Back）。
    /// 房间允许有多个入口，从哪扇门进来就由那扇门自己决定回程落点，
    /// 不再共用Room.returnDoor
    /// </summary>
    public Array<Door> returnDoors = new();
    public Array<Door> otherDoors = new();
    /// <summary>
    /// 玩家进入该房间前的上一个房间
    /// </summary>
    public Room preRoom = null;

    public int A = -1;


    #endregion

    Game game => GameManager.Instance.game;

    #region tag

    public bool CheckTag(RoomTag tag)
    {
        return tags.Contains(tag);
    }

    public void AddTag(RoomTag tag)
    {
        if (!tags.Contains(tag))
        {
            tags.Add(tag);
        }
    }

    public void RemoveTag(RoomTag tag)
    {
        if (tags.Contains(tag))
        {
            tags.Remove(tag);
        }
    }

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
        roomTitle = data.roomId;

        useDefaultRoomProcess = data.useDefaultRoomProcess;
        
        onCreate = data.onCreate?.Duplicate() as Effect;
        onFirstEnter = data.onFirstEnter?.Duplicate() as Effect;
        onGenerateEnemy = data.onGenerateEnemy?.Duplicate() as Effect;
        onGenerateItem = data.onGenerateItem?.Duplicate() as Effect;
        onEnter = data.onEnter?.Duplicate() as Effect;
        onExit = data.onExit?.Duplicate() as Effect;
        onUpdate = data.onUpdate?.Duplicate() as Effect;
        onClear = data.onClear?.Duplicate() as Effect;

        args = new();
        args.Init(data.args);

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
    /// <param name="room">目标房间</param>
    /// <returns>设置成功返回true，没有更多NextDoor了返回false</returns>
    public bool SetNextRoom(Room room)
    {
        foreach(Door door in nextDoors)
        {
            if(door.targetRoom == null)
            {
                door.targetRoom = room;

                //反向链接：让目标房间的返回门指向本门。
                //一个房间可能被多个房间连入（例如主线房间和资源房间都通向同一个普通房间），
                //但返回门只有一个，所以这里只保留首次绑定、后续连入不再覆盖，
                //返回门因此固定指向该房间在地图上的主线父房间（Map.LinkRooms 先链接主线、后链接支路）。
                string returnBindInfo = "返回门=无";
                if(room != null && room.returnDoor != null)
                {
                    if(room.returnDoor.targetDoor == null)
                    {
                        room.returnDoor.targetDoor = door;
                        returnBindInfo = $"返回门绑定到{door.Name}";
                    }
                    else
                    {
                        returnBindInfo = $"返回门保持{room.returnDoor.targetDoor.Name}";
                    }
                    door.targetDoor = room.returnDoor;
                }

                GD.Print($"[链接] {Name}.{door.Name} -> {room?.Name}【targetDoor={(door.targetDoor != null ? door.targetDoor.Name : "null")}，{returnBindInfo}】");
                return true;
            }
        }
        GD.PushWarning($"[链接] {Name} 没有空闲的Next门可分配给 {room?.Name}");
        return false;
    }

    #endregion

    # region 房间流程

    public override void _Ready()
    {
        onCreate?.Run(this);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        onUpdate?.Run(this);
        //默认房间流程
        if (useDefaultRoomProcess)
        {
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
                    //房间初次进入时生成物品
                    tileMap.SpawnItem();
                    A = 2;
                    break;
                case 2:
                    //生成所有波次的敌怪
                    //没有地形（未配置RoomData.tileMap）时不可能有敌人生成器，直接视为已清理
                    if(tileMap == null)
                    {
                        A = 3;
                        break;
                    }
                    if(tileMap.CheckGeneratorNowFinished() && tileMap.CheckEnemiesAllDead())
                    {
                        if (! tileMap.SpawnEnemyNext())
                        {
                            A = 3;
                        }
                    }
                    break;
                case 3:
                    //房间清理完毕，开门
                    OnClear();
                    A = -1;
                    break;
            }
        }
        
    }

    public void OnClear()
    {
        OpenAllDoor();
        isCleared = true;
        if (!alreadyCleared)
        {
            alreadyCleared = true;
            onClear?.Run(this);
            game.NotifyRoomCleared(this);
        }
    }

    # endregion

    #region 暴露方法

    public void Exit(Entity entity)
    {
        entity.room = null;
    }

    /// <summary>
    /// 是否为普通房间
    /// </summary>
    public bool IsNormalRoom => CheckTag(RoomTag.Normal);

    public void Enter(Entity entity)
    {
        Enter(entity, entity?.room);
    }

    /// <summary>
    /// 实体进入房间
    /// </summary>
    /// <param name="entity">进入的实体</param>
    /// <param name="fromRoom">实体进入前所在的房间</param>
    public void Enter(Entity entity, Room fromRoom)
    {
        if(!Game.IsAble(entity))
        {
            return;
        }

        //返回门的目标只由地图生成时的SetNextRoom决定，指向本房间在地图上的上一个房间，
        //与玩家实际来路无关，因此这里不能改写返回门的targetRoom与targetDoor
        preRoom = fromRoom;
        //实体将进入当前房间
        entity.room = this;
        //诊断：输出本房间所有门的绑定状态
        GD.Print($"[进入] {Name} 返回门={(returnDoor != null ? $"{returnDoor.Name}->{returnDoor.targetDoor?.room?.Name ?? returnDoor.targetRoom?.Name ?? "null"}" : "无")} 普通门{nextDoors.Count}个 普通房间={IsNormalRoom} 已清理={isCleared}");
        foreach(Door door in nextDoors)
        {
            if(door == null)
            {
                GD.Print("    <null>");
                continue;
            }
            GD.Print($"    {door.Name}: targetRoom={door.targetRoom?.Name ?? "null"} targetDoor={(door.targetDoor != null ? door.targetDoor.Name : "null")}");
        }
        OnEnter();
    }

    /// <summary>
    /// 打开房间里所有"已经绑定目标"的门。
    /// 没有绑定目标的门会被保持关闭（不可见），避免玩家走进去却没有任何反应
    /// </summary>
    public void OpenAllDoor()
    {
        if(doorNode == null)
        {
            return;
        }
        foreach(Node child in doorNode.GetChildren())
        {
            if(child is Door door)
            {
                if(door.IsBound || door.doorType == DoorType.NextLevel)
                {
                    door.Open();
                }
                else
                {
                    door.Close();
                }
            }
        }
    }

    public void CloseAllDoor()
    {
        if(doorNode == null)
        {
            return;
        }
        foreach(Node child in doorNode.GetChildren())
        {
            if(child is Door door)
            {
                door.Close();
            }
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
        if (!alreadyEntered)
        {
            onFirstEnter?.Run(this);
        }
        alreadyEntered = true;

        //更新游戏系统当前房间
        Game game = GameManager.Instance.game;
        if(game != null)
        {
            game.room = this;
        }
    }

    #endregion

    #region 工具方法

    public void Log(string text)
    {
        GD.Print($"[{Name}] {text}");
    }

    #endregion
}
using Godot;
using Godot.Collections;
using System;

/// <summary>
/// Map类用于生成游戏地图，地图生成规则如下：
/// 1. Map类的数据由MapData加载而来，MapData包含当前要生成的房间的data以及地图尺寸等信息。加载data数据时进行拷贝，参考其他根据data加载的类
/// 2. 本项目的地图为"主线直线 + 支路"地图，房间之间在画面上没有明显链接，玩家在房间之间移动依赖门之间的传送。
/// 主线为一条直线：起始房间 → 普通房间1 → 普通房间2 → ... → 最后一个普通房间 → Boss房间。
/// 普通房间固定有两个普通门和一个返回门；两个普通门中一个用于主线，一个用于支路（没有支路时第二个门同样通向主线下一个房间）。
/// 每个地图的普通房间数量就是地图尺寸。
/// 每个地图固定有一个资源房间和一个商店房间，boss房间为空则不生成boss房间，不为空则固定有一个boss房间。
/// 支路规则（临时策略）：靠近起始房间的第一个普通房间有一条支路通向资源房间；靠近Boss房间的最后一个普通房间有一条支路通向商店房间。
/// 资源房间和商店房间各有一个普通门通向主线上的下一个普通房间（若该普通房间的下一个房间是Boss房间则通向Boss房间）。
/// 3. 首先生成房间并缓存，这样房间会自动初始化其中的门，那之后使用SetNextRoom设置门的目标来链接所有房间。
/// 4. 房间的生成使用RandomSystem的随机数
/// </summary>

[GlobalClass]
public partial class Map : Node2D
{
    #region 数据

    /// <summary>
    /// 地图数据源（已拷贝）
    /// </summary>
    [Export] public MapData data;

    /// <summary>
    /// 随机数生成器名称（用于地图生成）
    /// </summary>
    [Export] public string randomGeneratorName;

    /// <summary>
    /// 所有已生成的房间（按生成顺序）
    /// </summary>
    [Export] public Array<Room> rooms = new();

    /// <summary>
    /// 起始房间
    /// </summary>
    public Room startRoom;

    /// <summary>
    /// Boss房间（如果没有boss则为null）
    /// </summary>
    public Room bossRoom;

    /// <summary>
    /// 商店房间
    /// </summary>
    public Room shopRoom;

    /// <summary>
    /// 资源房间
    /// </summary>
    public Room treasureRoom;

    /// <summary>
    /// 地图尺寸（普通房间数量）
    /// </summary>
    public int mapSize;

    /// <summary>
    /// 所有普通房间（不包含起始、boss、商店、资源房间）
    /// </summary>
    public Array<Room> normalRooms;

    #endregion

    #region 常量

    /// <summary>
    /// 房间之间的水平间距
    /// </summary>
    private const float ROOM_SPACING_X = 1980f;

    /// <summary>
    /// 房间之间的垂直偏移幅度（用于树状布局）
    /// </summary>
    private const float ROOM_SPACING_Y = 1980f;

    #endregion

    #region 初始化

    /// <summary>
    /// 使用MapData初始化地图
    /// </summary>
    /// <param name="data">地图数据</param>
    /// <param name="randomSystem">随机数系统</param>
    public void Initialize(MapData data, RandomSystem randomSystem)
    {
        if (data == null)
        {
            GD.PrintErr("地图初始化：MapData参数为空，使用编辑器定义的data");
            data = this.data;
        }

        this.data = data;
        // 拷贝数据
        mapSize = data.mapSize;
        foreach (RoomTag tag in data.roomDatas.Keys)
        {
            data.roomDatas[tag] = [.. data.roomDatas[tag]];
        }
        data.specialRoomDatas = [.. data.specialRoomDatas];

        // 创建随机数生成器
        randomGeneratorName = randomSystem.SetRandomGenerator(generatorName: "MapGenerator");

        // 生成地图
        GenerateMap();
    }

    #endregion

    #region 地图生成

    /// <summary>
    /// 执行地图生成流程
    /// </summary>
    private void GenerateMap()
    {
        rooms.Clear();

        // 1. 从数据中随机选取各类型房间的RoomData
        RoomData startData = PickRoomData(RoomTag.Start);
        RoomData bossData = PickRoomData(RoomTag.Boss);
        RoomData shopData = PickRoomData(RoomTag.Shop);
        RoomData treasureData = PickRoomData(RoomTag.Treasure);

        int normalCount = data.mapSize;

        // 2. 生成起始房间
        startRoom = CreateRoomInstance(Vector2.Zero, startData);
        rooms.Add(startRoom);

        // 3. 生成普通房间（主线直线布局）
        Array<Room> normalRooms = new();
        for (int i = 0; i < normalCount; i++)
        {
            RoomData normalData = PickRoomData(RoomTag.Normal);
            Vector2 position = CalculateNormalRoomPosition(i, normalCount);
            Room room = CreateRoomInstance(position, normalData);
            normalRooms.Add(room);
            rooms.Add(room);
        }
        this.normalRooms = normalRooms;

        // 4. 生成资源房间（靠近起始房间的第一个普通房间的支路）
        if (treasureData != null)
        {
            treasureRoom = CreateRoomInstance(CalculateSpecialRoomPosition(normalCount, 0), treasureData);
            rooms.Add(treasureRoom);
        }

        // 5. 生成商店房间（靠近Boss房间的最后一个普通房间的支路，至少需要2个普通房间）
        if (shopData != null && normalCount >= 2)
        {
            shopRoom = CreateRoomInstance(CalculateSpecialRoomPosition(normalCount, 1), shopData);
            rooms.Add(shopRoom);
        }

        // 6. 生成Boss房间
        if (bossData != null)
        {
            bossRoom = CreateRoomInstance(CalculateBossRoomPosition(normalCount), bossData);
            rooms.Add(bossRoom);
        }

        // 7. 链接房间：用SetNextRoom建立连接
        LinkRooms(normalRooms);

        GD.Print($"[Map] 地图生成完成：{rooms.Count}个房间（{normalCount}个普通房间）");
    }

    /// <summary>
    /// 链接所有房间的门。
    /// 地图结构：
    /// - 主线：起始房间 → 普通房间1 → 普通房间2 → ... → 最后一个普通房间 → Boss房间（一条直线）。
    /// - 支路（临时策略）：
    ///   靠近起始房间的第一个普通房间 → 资源房间 → 主线下一个普通房间（或Boss）。
    ///   靠近Boss房间的最后一个普通房间 → 商店房间 → Boss。
    /// - 普通房间固定有两个普通门：第一个门用于主线，第二个门用于支路；没有支路时第二个门也通向主线下一个房间。
    /// - 资源房间和商店房间各有一个普通门通向主线上的下一个普通房间（若下一个是Boss则通向Boss）。
    /// 注意：Room.SetNextRoom总是设置第一个未设置targetRoom的门，
    /// 因此必须先设置主线（占用第一个门），再为有支路的普通房间设置第二个门。
    /// </summary>
    private void LinkRooms(Array<Room> normalRooms)
    {
        int normalCount = normalRooms.Count;
        if (normalCount <= 0)
        {
            // 没有普通房间时，起始房间直接通向Boss
            if (startRoom != null && bossRoom != null)
            {
                startRoom.SetNextRoom(bossRoom);
            }
            return;
        }

        // 1. 主线：起始房间 → 第一个普通房间
        startRoom.SetNextRoom(normalRooms[0]);

        // 2. 主线：普通房间依次链接到下一个普通房间，最后一个普通房间链接到Boss。
        //    这里只为每个普通房间设置第一个门（主线门），第二个门留给支路。
        for (int i = 0; i < normalCount; i++)
        {
            Room next = GetNextMainRoom(normalRooms, i);
            if (next != null)
            {
                normalRooms[i].SetNextRoom(next);
            }
        }

        // 3. 支路：靠近起始房间的第一个普通房间 → 资源房间（占用第一个普通房间的第二个门）。
        //    资源房间的普通门 → 其父节点主线下一个房间（或Boss）。
        if (treasureRoom != null)
        {
            normalRooms[0].SetNextRoom(treasureRoom);

            Room treasureNext = GetNextMainRoom(normalRooms, 0);
            if (treasureNext != null)
            {
                treasureRoom.SetNextRoom(treasureNext);
            }
        }

        // 4. 支路：靠近Boss房间的最后一个普通房间 → 商店房间（占用最后一个普通房间的第二个门）。
        //    商店房间的普通门 → 其父节点主线下一个房间（通常是Boss）。
        if (shopRoom != null && normalCount >= 2)
        {
            int shopParentIndex = normalCount - 1;
            normalRooms[shopParentIndex].SetNextRoom(shopRoom);

            Room shopNext = GetNextMainRoom(normalRooms, shopParentIndex);
            if (shopNext != null)
            {
                shopRoom.SetNextRoom(shopNext);
            }
        }

        // 5. 没有支路的普通房间：第二个门同样通向主线下一个房间。
        for (int i = 0; i < normalCount; i++)
        {
            bool hasBranch = (i == 0 && treasureRoom != null) ||
                             (i == normalCount - 1 && shopRoom != null && normalCount >= 2);
            if (!hasBranch)
            {
                Room next = GetNextMainRoom(normalRooms, i);
                if (next != null)
                {
                    normalRooms[i].SetNextRoom(next);
                }
            }
        }
    }

    /// <summary>
    /// 获取主线上下一个房间；最后一个普通房间的下一个是Boss房间
    /// </summary>
    private Room GetNextMainRoom(Array<Room> normalRooms, int index)
    {
        if (index < normalRooms.Count - 1)
        {
            return normalRooms[index + 1];
        }
        return bossRoom;
    }

    #endregion

    #region 房间创建与布局

    /// <summary>
    /// 创建房间实例
    /// </summary>
    private Room CreateRoomInstance(Vector2 position, RoomData data)
    {
        if (data == null)
        {
            data = Room.DefaultData;
        }

        // 通过Game的SpawnRoom方法创建房间
        Game game = GameManager.Instance?.game;
        if (game != null)
        {
            return game.SpawnRoom(position, data);
        }

        // 回退：直接创建
        return Room.Create(position, data);
    }

    /// <summary>
    /// 计算普通房间的位置（主线直线排列）
    /// </summary>
    private Vector2 CalculateNormalRoomPosition(int index, int totalCount)
    {
        // 主线是一条直线：沿X轴依次排列，Y轴居中
        float x = (index + 1) * ROOM_SPACING_X;
        float y = 0;
        return new Vector2(x, y);
    }

    /// <summary>
    /// 计算特殊房间的位置
    /// offset 0 = 资源房间：挂在靠近起始房间侧的第一个普通房间上方
    /// offset 1 = 商店房间：挂在靠近Boss房间侧的最后一个普通房间下方
    /// </summary>
    private Vector2 CalculateSpecialRoomPosition(int normalCount, int offset)
    {
        if (offset == 0)
        {
            // 资源房间：靠近起始房间一侧的第一个普通房间上方（x = 1 * ROOM_SPACING_X）
            float x = ROOM_SPACING_X;
            float y = -ROOM_SPACING_Y * 1.2f;
            return new Vector2(x, y);
        }
        else
        {
            // 商店房间：靠近Boss房间一侧的最后一个普通房间下方（x = normalCount * ROOM_SPACING_X）
            float x = normalCount * ROOM_SPACING_X;
            float y = ROOM_SPACING_Y * 1.2f;
            return new Vector2(x, y);
        }
    }

    /// <summary>
    /// 计算Boss房间的位置
    /// </summary>
    private Vector2 CalculateBossRoomPosition(int normalCount)
    {
        float x = (normalCount + 1) * ROOM_SPACING_X;
        float y = 0;
        return new Vector2(x, y);
    }

    #endregion

    #region 数据选取

    /// <summary>
    /// 从MapData中随机选取指定标签的RoomData
    /// </summary>
    private RoomData PickRoomData(RoomTag tag)
    {
        if (data == null || !data.roomDatas.ContainsKey(tag))
        {
            return null;
        }

        Array<RoomData> candidates = data.roomDatas[tag];
        if (candidates == null || candidates.Count == 0)
        {
            return null;
        }

        // 使用随机数系统选取
        RandomSystem randomSystem = GetRandomSystem();
        if (randomSystem != null)
        {
            return randomSystem.Pick(candidates, randomGeneratorName);
        }

        // 回退：直接随机
        return candidates[(int)(GD.Randi() % (ulong)candidates.Count)];
    }

    #endregion

    #region 工具方法

    /// <summary>
    /// 获取随机数系统
    /// </summary>
    private RandomSystem GetRandomSystem()
    {
        // 从GameManager或全局获取RandomSystem实例
        // 这里假设RandomSystem以某种方式被全局管理
        // 如果不可用则返回null
        return null; // 暂未实现全局RandomSystem管理
    }

    /// <summary>
    /// 获取.NET Random对象
    /// </summary>
    private Random GetRandomGenerator()
    {
        // 尝试从RandomSystem获取，若不可用则使用默认随机
        return new Random();
    }

    /// <summary>
    /// Fisher-Yates洗牌算法
    /// </summary>
    private void Shuffle<[MustBeVariant] T>(Array<T> array, Random random)
    {
        int n = array.Count;
        while (n > 1)
        {
            n--;
            int k = random.Next(n + 1);
            (array[k], array[n]) = (array[n], array[k]);
        }
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 根据索引获取房间
    /// </summary>
    public Room GetRoom(int index)
    {
        if (index < 0 || index >= rooms.Count)
        {
            return null;
        }
        return rooms[index];
    }

    /// <summary>
    /// 获取所有房间
    /// </summary>
    public Array<Room> GetAllRooms()
    {
        return rooms.Duplicate();
    }

    /// <summary>
    /// 获取房间数量
    /// </summary>
    public int RoomCount => rooms.Count;

    #endregion
}
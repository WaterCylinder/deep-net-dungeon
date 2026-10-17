using Godot;
using Godot.Collections;
using System;

/// <summary>
/// Map类用于生成游戏地图。
/// 1. 数据来自MapData（房间数据、生成模式与各种规模参数）。
/// 2. 房间之间在画面上没有明显链接，玩家在房间之间移动依赖门之间的传送。
/// 3. 生成模式由MapData.mapMode决定：
///    - Fixed（默认，固定地图）：起始房间 → N个普通房间 → Boss房间（主线单向连接）。
///      最后一个普通房间的第二个普通门通向商店房间；资源房间随机出现在"除最后一个普通房间之外"的
///      某个普通房间的第二个普通门；资源房间自己的门通向它父房间主线上的下一个房间（平行路径），
///      商店房间的门通向Boss。
///    - Tree（树状地图）：主干每层1个房间、单向连接到下一层，末端接唯一Boss；
///      主干节点按概率长出支路，支路按概率继续延伸，结束的支路汇回主干的下一个节点。
/// 4. 不管哪种模式，都会确保每个房间的空闲普通门被连掉，避免出现没有目标的"死门"。
/// 5. 首先生成房间并缓存（房间会自动初始化其中的门），然后用SetNextRoom设置门的目标来链接所有房间。
/// 6. 生成使用RandomSystem的随机数（同一个种子得到同一张地图）。
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
    /// 地图尺寸（生成后的房间总数，这里统计的是普通房间数量）
    /// </summary>
    public int mapSize;

    /// <summary>
    /// 所有普通房间（不包含起始、boss、商店、资源房间）
    /// </summary>
    public Array<Room> normalRooms;

    /// <summary>
    /// 按层保存的房间，layers[0]是起始房间所在的层。
    /// 供小地图、关卡流程等按层取房间使用
    /// </summary>
    public Array<Array<Room>> layers = new();

    /// <summary>
    /// 本次生成使用的随机数系统（由Initialize传入，用于房间数据挑选与分叉判定）
    /// </summary>
    private RandomSystem randomSystem;

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
            GD.PushWarning("地图初始化：MapData参数为空，使用编辑器定义的data");
            data = this.data;
        }

        this.data = data;
        // 拷贝数据
        mapSize = 0;
        foreach (RoomTag tag in data.roomDatas.Keys)
        {
            data.roomDatas[tag] = [.. data.roomDatas[tag]];
        }
        data.specialRoomDatas = [.. data.specialRoomDatas];

        // 记录随机数系统（生成过程中要用它挑选房间数据与判定分叉）
        this.randomSystem = randomSystem;
        // 创建随机数生成器
        randomGeneratorName = randomSystem?.SetRandomGenerator(generatorName: "MapGenerator");

        // 生成地图
        GenerateMap();
    }

    #endregion

    #region 地图生成

    /// <summary>
    /// 执行地图生成流程，按MapData.mapMode选择生成模式
    /// </summary>
    private void GenerateMap()
    {
        rooms.Clear();
        layers.Clear();
        startRoom = null;
        bossRoom = null;
        shopRoom = null;
        treasureRoom = null;

        switch (data.mapMode)
        {
            case MapMode.Tree:
                GenerateTreeMap();
                break;

            default:
                GenerateFixedMap();
                break;
        }

        FinishGeneration();
    }

    /// <summary>
    /// 生成后的公共收尾：统计普通房间并打印结果
    /// </summary>
    private void FinishGeneration()
    {
        Array<Room> normalList = new();
        foreach (Room room in rooms)
        {
            if (room != null && room.CheckTag(RoomTag.Normal))
            {
                normalList.Add(room);
            }
        }

        normalRooms = normalList;
        mapSize = normalList.Count;

        // 检查是否还有没绑定目标的普通门。
        // 这种门不会被打开（Room.OpenAllDoor只打开已绑定的门），正常不应该出现，
        // 出现通常意味着房间预制体的门数量和地图规则不匹配，这里直接报出来方便排查
        int unboundDoorCount = 0;
        foreach (Room room in rooms)
        {
            if (room == null || room.nextDoors == null)
            {
                continue;
            }

            foreach (Door door in room.nextDoors)
            {
                if (door != null && !door.IsBound)
                {
                    unboundDoorCount++;
                    GD.PushWarning($"[Map] {room.Name}.{door.Name} 没有绑定目标，这扇门不会被打开");
                }
            }
        }

        GD.Print($"[Map] 地图生成完成（{data.mapMode}）：共{rooms.Count}个房间、{layers.Count}层、普通房间{mapSize}个" +
                 $"（资源房间={(treasureRoom != null ? "有" : "无")}，商店房间={(shopRoom != null ? "有" : "无")}，Boss={(bossRoom != null ? "有" : "无")}" +
                 $"，未绑定的门{unboundDoorCount}扇）");
    }

    /// <summary>
    /// 固定地图：起始房间 → N个普通房间 → Boss房间（主线单向连接）。
    /// - 最后一个普通房间的第二个普通门 → 商店房间，商店房间的门 → Boss。
    /// - 资源房间随机出现在"除最后一个普通房间之外"的某个普通房间的第二个普通门；
    ///   资源房间自己的门 → 它父房间主线上的下一个房间（作为平行路径/捷径）。
    /// - 没有被挑中放支路的普通房间，第二个门也指向主线的下一个房间，避免留下没有目标的死门。
    /// 主线长度由MapData.normalRoomCount决定（默认5）
    /// </summary>
    private void GenerateFixedMap()
    {
        int normalCount = Mathf.Max(1, data.normalRoomCount);

        RoomData startData = PickRoomData(RoomTag.Start);
        RoomData bossData = PickRoomData(RoomTag.Boss);
        RoomData shopData = PickRoomData(RoomTag.Shop);
        RoomData treasureData = PickRoomData(RoomTag.Treasure);

        // 1. 主线房间：起始房间 + N个普通房间 + Boss
        Room start = CreateRoomInstance(CalculateFixedRoomPosition(0), startData, "Room_L0_0");
        if (start == null)
        {
            GD.PrintErr("[Map] 起始房间创建失败，地图生成中止");
            return;
        }

        startRoom = start;
        rooms.Add(start);

        Array<Room> mainLine = new();   // 主线上的普通房间，按顺序排列

        for (int i = 1; i <= normalCount; i++)
        {
            Room room = CreateRoomInstance(CalculateFixedRoomPosition(i), PickRoomData(RoomTag.Normal), $"Room_L1_{i}");
            if (room == null)
            {
                continue;
            }

            rooms.Add(room);
            mainLine.Add(room);
        }

        if (mainLine.Count <= 0)
        {
            GD.PushWarning("[Map] 没能生成任何普通房间，固定地图退化为起始房间直连Boss");
        }

        if (bossData != null)
        {
            bossRoom = CreateRoomInstance(CalculateFixedRoomPosition(mainLine.Count + 1), bossData, "Room_boss");
            if (bossRoom != null)
            {
                rooms.Add(bossRoom);
            }
        }
        else
        {
            GD.PushWarning("[Map] 未配置Boss房间数据，主线末端将没有后续房间");
        }

        // 2. 先连主线，保证每个房间的第一个普通门用于主线（支路随后占用第二个门）
        Room previous = start;
        for (int i = 0; i < mainLine.Count; i++)
        {
            previous.SetNextRoom(mainLine[i]);
            previous = mainLine[i];
        }

        if (bossRoom != null)
        {
            previous.SetNextRoom(bossRoom);
        }

        // 3. 商店房间：挂在最后一个普通房间的第二条门
        Room lastNormal = mainLine.Count > 0 ? mainLine[mainLine.Count - 1] : null;

        if (shopData != null && lastNormal != null && bossRoom != null)
        {
            shopRoom = CreateRoomInstance(
                CalculateFixedBranchPosition(mainLine.Count, false), shopData, "Room_shop");

            if (shopRoom != null)
            {
                rooms.Add(shopRoom);
                lastNormal.SetNextRoom(shopRoom);
                shopRoom.SetNextRoom(bossRoom);
            }
        }

        // 4. 资源房间：在前面的普通房间（排除最后一个）中随机挑一个挂支路
        if (treasureData != null)
        {
            if (mainLine.Count >= 2)
            {
                int parentIndex = RandomInt(0, mainLine.Count - 2);   // 0 .. N-2，排除最后一个普通房间
                Room parent = mainLine[parentIndex];
                Room treasureNext = parentIndex + 1 < mainLine.Count ? mainLine[parentIndex + 1] : bossRoom;

                treasureRoom = CreateRoomInstance(
                    CalculateFixedBranchPosition(parentIndex + 1, true), treasureData, "Room_treasure");

                if (treasureRoom != null)
                {
                    rooms.Add(treasureRoom);
                    parent.SetNextRoom(treasureRoom);

                    if (treasureNext != null)
                    {
                        treasureRoom.SetNextRoom(treasureNext);
                    }
                }
            }
            else
            {
                GD.PushWarning("[Map] 普通房间数量不足2个，没有可挂资源房间的位置，本次不生成资源房间");
            }
        }

        // 5. 收尾：没有被挑中放支路的普通房间，第二个门也指向主线下一个房间，避免死门
        for (int i = 0; i < mainLine.Count; i++)
        {
            Room next = i + 1 < mainLine.Count ? mainLine[i + 1] : bossRoom;
            if (next != null)
            {
                LinkAllFreeDoors(mainLine[i], next);
            }
        }

        LinkAllFreeDoors(start, mainLine.Count > 0 ? mainLine[0] : bossRoom);

        // 6. layers 记录主线推进顺序（固定模式下支路房间不计入）
        layers.Add(new Array<Room>() { start });
        foreach (Room room in mainLine)
        {
            layers.Add(new Array<Room>() { room });
        }
        if (bossRoom != null)
        {
            layers.Add(new Array<Room>() { bossRoom });
        }
    }

    /// <summary>
    /// 树状地图：主干 + 随机支路。
    /// - 主干：每层恰好1个房间，从起始房间单向连接到下一层，末端接唯一的Boss房间，
    ///   所以主干就是整张地图里最长的那条枝干（Boss位于最长枝干的最后）。
    /// - 支路：主干节点以branchChance的概率额外长出一条支路；支路节点再以同一概率继续延伸，
    ///   不延伸的支路到此结束，并把空闲的门汇回主干的下一个节点（平行路径，不会留下没有目标的死门）。
    /// - branchChance = 0 时退化成纯线性地图（每层1个房间，一路单向连到Boss）。
    /// </summary>
    private void GenerateTreeMap()
    {
        RoomData bossData = PickRoomData(RoomTag.Boss);
        int depth = Mathf.Max(1, data.treeDepth);

        // 层0：起始房间（主干起点）
        Room spine = CreateRoomInstance(Vector2.Zero, PickRoomData(RoomTag.Start), "Room_L0_0");
        if (spine == null)
        {
            GD.PrintErr("[Map] 起始房间创建失败，地图生成中止");
            return;
        }

        startRoom = spine;
        rooms.Add(spine);

        Array<Room> currentLayer = new() { spine };
        layers.Add(currentLayer);

        // 上一层结束的支路节点，等待汇回下一层的主干节点
        System.Collections.Generic.List<Room> pendingLeaves = new();

        int treasureTargetLayer = data.treasureLayer;
        int shopTargetLayer = data.shopLayer > 0 ? data.shopLayer : depth;
        bool treasurePlaced = false;
        bool shopPlaced = false;

        for (int layer = 1; layer <= depth; layer++)
        {
            // 1. 规划本层槽位：槽位0固定是主干节点，其余都是支路节点
            System.Collections.Generic.List<SlotPlan> plans = new()
            {
                new SlotPlan { parent = spine, data = PickRoomData(RoomTag.Normal) }
            };

            // 1.1 主干节点的支路：按概率分叉；最后一层若特殊房间还没放完则强制分叉一次
            bool isLastLayer = layer == depth;
            bool mustPlaceSpecial = isLastLayer
                && ((!treasurePlaced && treasureTargetLayer > 0) || !shopPlaced);

            // 主干节点要留1个门给主干孩子，所以分叉需要2个空闲门
            if (plans.Count < data.maxLayerRooms && CanBranch(spine, 2)
                && (mustPlaceSpecial || RandomFloat(0f, 1f) < data.branchChance))
            {
                plans.Add(new SlotPlan { parent = spine });
            }

            // 1.2 上一层支路节点的延续：按概率继续，不继续的成为叶子等待汇回主干
            foreach (Room room in currentLayer)
            {
                if (room == spine)
                {
                    continue;   // 主干节点已在上面处理
                }

                if (plans.Count >= data.maxLayerRooms || !CanBranch(room, 1)
                    || RandomFloat(0f, 1f) >= data.branchChance)
                {
                    pendingLeaves.Add(room);
                    continue;
                }

                plans.Add(new SlotPlan { parent = room });
            }

            // 1.3 给支路槽位分配房间数据（资源/商店房间只作为支路出现）
            for (int i = 1; i < plans.Count; i++)
            {
                plans[i].data = PickBranchRoomData(layer, treasureTargetLayer, shopTargetLayer,
                    ref treasurePlaced, ref shopPlaced, out RoomTag tag);
                plans[i].tag = tag;
            }

            // 2. 创建本层房间并逐个连到自己的父房间
            Array<Room> nextLayer = new();
            Room newSpine = null;

            for (int i = 0; i < plans.Count; i++)
            {
                SlotPlan plan = plans[i];
                Room child = CreateRoomInstance(
                    CalculateLayerRoomPosition(layer, i),
                    plan.data,
                    $"Room_L{layer}_{i}");

                if (child == null)
                {
                    continue;
                }

                nextLayer.Add(child);
                rooms.Add(child);

                // 槽位0是主干孩子，最先连接它，保证主干节点的返回门指向主干父节点
                plan.parent.SetNextRoom(child);

                if (i == 0)
                {
                    newSpine = child;
                }

                if (plan.tag == RoomTag.Treasure)
                {
                    treasureRoom = child;
                }
                else if (plan.tag == RoomTag.Shop)
                {
                    shopRoom = child;
                }
            }

            if (nextLayer.Count <= 0 || newSpine == null)
            {
                break;  // 本层没有造出主干孩子，主干无法接力，地图到此结束
            }

            // 3. 主干接力：主干节点若没分叉，剩余的门也指向主干孩子，避免留下死门
            LinkAllFreeDoors(spine, newSpine);

            // 4. 上一层结束的支路汇回本层主干（作为平行路径）
            foreach (Room leaf in pendingLeaves)
            {
                LinkAllFreeDoors(leaf, newSpine);
            }
            pendingLeaves.Clear();

            spine = newSpine;
            layers.Add(nextLayer);
            currentLayer = nextLayer;
        }

        // 5. 最后一层的支路节点汇回同层主干，避免出现没有目标的死门
        foreach (Room room in currentLayer)
        {
            if (room != spine)
            {
                LinkAllFreeDoors(room, spine);
            }
        }

        // 6. 主干末端接唯一Boss：Boss只有这一个父节点，位于最长枝干的最后
        if (bossData != null)
        {
            bossRoom = CreateRoomInstance(CalculateBossPosition(depth), bossData, "Room_boss");

            if (bossRoom != null)
            {
                rooms.Add(bossRoom);
                LinkAllFreeDoors(spine, bossRoom);
            }
        }
        else
        {
            GD.PushWarning("[Map] 未配置Boss房间数据，主干末端将没有后续房间");
        }
    }

    /// <summary>
    /// 本层一个房间槽位的规划：由哪个房间产生、用哪份房间数据
    /// </summary>
    private class SlotPlan
    {
        /// <summary>
        /// 产生这个槽位的父房间
        /// </summary>
        public Room parent;

        /// <summary>
        /// 该槽位使用的房间数据
        /// </summary>
        public RoomData data;

        /// <summary>
        /// 该槽位的房间类型（用于记录资源/商店房间引用）
        /// </summary>
        public RoomTag tag = RoomTag.Normal;
    }

    /// <summary>
    /// 判断房间是否有足够多的空闲普通门用来长出孩子
    /// </summary>
    /// <param name="room">待判断的房间</param>
    /// <param name="neededDoors">需要占用的门数（主干节点分叉需要2个：1个给主干孩子、1个给支路）</param>
    private static bool CanBranch(Room room, int neededDoors)
    {
        return CountFreeDoors(room) >= neededDoors;
    }

    /// <summary>
    /// 统计房间还有几个空闲的普通门
    /// </summary>
    private static int CountFreeDoors(Room room)
    {
        if (room == null || room.nextDoors == null)
        {
            return 0;
        }

        int count = 0;
        foreach (Door door in room.nextDoors)
        {
            if (door != null && door.targetRoom == null)
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// 把房间所有空闲的普通门都连向同一个目标房间（没有空闲门时不做任何事）。
    /// 用于"不分叉的房间"和"最后一层连向Boss"，避免出现没有目标的死门
    /// </summary>
    private static void LinkAllFreeDoors(Room room, Room target)
    {
        if (room == null || target == null)
        {
            return;
        }

        int freeCount = CountFreeDoors(room);
        for (int i = 0; i < freeCount; i++)
        {
            room.SetNextRoom(target);
        }
    }

    /// <summary>
    /// 按槽位挑选房间数据：指定槽位放资源/商店房间，其余放普通房间
    /// </summary>
    /// <param name="layer">当前层号</param>
    /// <param name="treasureLayer">资源房间出现的层（≤0表示不生成）</param>
    /// <param name="shopLayer">商店房间出现的层</param>
    /// <param name="treasurePlaced">资源房间是否已放置</param>
    /// <param name="shopPlaced">商店房间是否已放置</param>
    /// <param name="tag">实际取到的房间类型</param>
    private RoomData PickBranchRoomData(int layer, int treasureLayer, int shopLayer,
        ref bool treasurePlaced, ref bool shopPlaced, out RoomTag tag)
    {
        tag = RoomTag.Normal;

        if (!treasurePlaced && treasureLayer > 0 && layer >= treasureLayer)
        {
            RoomData treasure = PickRoomData(RoomTag.Treasure);
            if (treasure != null)
            {
                tag = RoomTag.Treasure;
                treasurePlaced = true;
                return treasure;
            }
        }

        if (!shopPlaced && layer >= shopLayer)
        {
            RoomData shop = PickRoomData(RoomTag.Shop);
            if (shop != null)
            {
                tag = RoomTag.Shop;
                shopPlaced = true;
                return shop;
            }
        }

        return PickRoomData(RoomTag.Normal);
    }

    /// <summary>
    /// 用随机数系统生成[min,max)之间的随机浮点数
    /// </summary>
    private float RandomFloat(float min, float max)
    {
        RandomSystem system = GetRandomSystem();
        if (system != null)
        {
            return system.Float(min, max, randomGeneratorName);
        }

        return (float)GD.RandRange(min, max);
    }

    #endregion

    #region 房间创建与布局

    /// <summary>
    /// 创建房间实例
    /// </summary>
    private Room CreateRoomInstance(Vector2 position, RoomData data, string roomName = "")
    {
        if (data == null)
        {
            data = Room.DefaultData;
        }

        Room room = null;

        // 通过Game的SpawnRoom方法创建房间
        Game game = GameManager.Instance?.game;
        if (game != null)
        {
            room = game.SpawnRoom(position, data);
        }
        else
        {
            // 无game对象：直接创建
            room = Room.Create(position, data);
        }

        if(room == null)
        {
            GD.PrintErr("[Map] 房间创建失败");
            return null;
        }

        if(roomName != "")
        {
            room.Name = roomName;
        }
        else
        {
            room.Name = $"Room_{rooms.Count}";
        }

        return room;
    }

    /// <summary>
    /// 计算某一层中某个房间的位置：同一层沿X轴居中排列，层数决定Y。
    /// 房间之间靠门的传送移动，世界坐标只用于摆放与编辑器里观察树的形状
    /// </summary>
    /// <param name="layer">层号（0是起始房间所在层）</param>
    /// <param name="indexInLayer">该层内的序号（0固定是主干节点）</param>
    private Vector2 CalculateLayerRoomPosition(int layer, int indexInLayer)
    {
        // 主干节点（序号0）固定在x=0排成一条竖直主干，支路依次向右排开；
        // 房间之间靠门的传送移动，世界坐标只用于摆放与在编辑器里观察地图形状
        float x = indexInLayer * ROOM_SPACING_X;
        float y = layer * ROOM_SPACING_Y;
        return new Vector2(x, y);
    }

    /// <summary>
    /// 计算Boss房间的位置（主干末端再往下一层）
    /// </summary>
    /// <param name="depth">主干层数</param>
    private Vector2 CalculateBossPosition(int depth)
    {
        return new Vector2(0f, (depth + 1) * ROOM_SPACING_Y);
    }

    /// <summary>
    /// 固定模式下主线房间的位置：沿X轴依次排开（0是起始房间，末尾是Boss）
    /// </summary>
    /// <param name="mainIndex">主线上的序号</param>
    private Vector2 CalculateFixedRoomPosition(int mainIndex)
    {
        return new Vector2(mainIndex * ROOM_SPACING_X, 0f);
    }

    /// <summary>
    /// 固定模式下支路房间的位置：资源房间挂在父房间上方，商店房间挂在最后一个普通房间下方
    /// </summary>
    /// <param name="mainIndex">父房间在主线上的序号</param>
    /// <param name="above">true = 放在主线之上（资源房间），false = 放在主线之下（商店房间）</param>
    private Vector2 CalculateFixedBranchPosition(int mainIndex, bool above)
    {
        float y = above ? -ROOM_SPACING_Y : ROOM_SPACING_Y;
        return new Vector2(mainIndex * ROOM_SPACING_X, y);
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
    /// 获取本次生成使用的随机数系统（由Initialize传入）。
    /// 之前这里恒返回null，导致房间数据挑选退化成GD.Randi()、地图无法复现
    /// </summary>
    private RandomSystem GetRandomSystem()
    {
        return randomSystem;
    }

    /// <summary>
    /// 用随机数系统生成[min,max]之间的随机整数（包含两端）
    /// </summary>
    private int RandomInt(int min, int max)
    {
        RandomSystem system = GetRandomSystem();
        if (system != null)
        {
            return system.Int(min, max, randomGeneratorName);
        }

        return (int)GD.RandRange(min, max);
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
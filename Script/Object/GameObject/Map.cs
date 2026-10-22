using Godot;
using Godot.Collections;
using System;

/// <summary>
/// Map类用于生成游戏地图。
/// 1. 数据来自MapData（房间数据、生成模式与各种规模参数）。
/// 2. 房间之间在画面上没有明显链接，玩家在房间之间移动依赖门之间的传送。
/// 3. 生成模式由MapData.mapMode决定：
///    - Line（默认，直线地图）：起始房间 → [若干普通房间 + 1个资源房间 + 1个商店房间 混排] → Boss房间。
///      普通房间数量在Map.minNormalRoomCount~Map.maxNormalRoomCount之间随机；
///      资源房间越靠近起始房间越容易抽到，商店房间越靠近Boss房间越容易抽到。
///      所有房间单向串联成一条直线，没有支路。
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

    #region 直线地图生成参数

    /// <summary>
    /// 直线地图：普通房间数量的最小值（含）。
    /// 与maxNormalRoomCount一起决定本次直线的普通房间数量（在这个区间里随机）；
    /// 两个都填0（或负数）时退回MapData.normalRoomCount作为固定数量
    /// </summary>
    [ExportCategory("直线地图生成")]
    [Export] public int minNormalRoomCount = 3;

    /// <summary>
    /// 直线地图：普通房间数量的最大值（含）。
    /// 小于minNormalRoomCount时会自动互换，写反了也不会报错
    /// </summary>
    [Export] public int maxNormalRoomCount = 7;

    /// <summary>
    /// 资源房间的位置倾向（0~1）：
    /// 0 = 在路径上完全随机，数值越大越集中在靠近起始房间的位置（1 = 只出现在离起始房间最近的那个槽位）
    /// </summary>
    [Export(PropertyHint.Range, "0,1,0.05")] public float treasureNearStartBias = 0.5f;

    /// <summary>
    /// 商店房间的位置倾向（0~1）：
    /// 0 = 在路径上完全随机，数值越大越集中在靠近Boss房间的位置（1 = 只出现在离Boss最近的那个槽位）
    /// </summary>
    [Export(PropertyHint.Range, "0,1,0.05")] public float shopNearBossBias = 0.5f;

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

            case MapMode.Line:
            default:
                GenerateLineMap();
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
    /// 直线地图：起始房间 → [若干普通房间 + 1个资源房间 + 1个商店房间 混排] → Boss房间。
    /// - 普通房间数量在Map的minNormalRoomCount~maxNormalRoomCount之间随机
    /// - 资源房间与商店房间各只有一个，随机夹杂在路径中间，且不会落在同一个槽位上
    /// - 抽取槽位带权重：资源房间越靠近起始房间概率越高，商店房间越靠近Boss房间概率越高
    ///   （强度由treasureNearStartBias / shopNearBossBias控制）
    /// - 所有房间单向串联成一条直线，没有任何支路
    /// </summary>
    private void GenerateLineMap()
    {
        RoomData startData = PickRoomData(RoomTag.Start);
        RoomData bossData = PickRoomData(RoomTag.Boss);
        RoomData shopData = PickRoomData(RoomTag.Shop);
        RoomData treasureData = PickRoomData(RoomTag.Treasure);

        if (bossData == null)
        {
            GD.PushWarning("[Map] 未配置Boss房间数据，直线末端将没有Boss房间");
        }
        if (treasureData == null)
        {
            GD.PushWarning("[Map] 未配置资源房间数据，本次不生成资源房间");
        }
        if (shopData == null)
        {
            GD.PushWarning("[Map] 未配置商店房间数据，本次不生成商店房间");
        }

        // 1. 普通房间数量，以及路径槽位数量（路径指起始房间与Boss房间之间的部分）
        int normalCount = RollNormalRoomCount();
        int specialCount = (treasureData != null ? 1 : 0) + (shopData != null ? 1 : 0);
        int slotCount = normalCount + specialCount;

        // 2. 抽取特殊房间的槽位：先抽资源房间，商店房间在剩下的槽位里抽
        int treasureSlot = treasureData != null
            ? PickSlotByDistancePreference(slotCount, true, treasureNearStartBias)
            : -1;
        int shopSlot = shopData != null
            ? PickSlotByDistancePreference(slotCount, false, shopNearBossBias, treasureSlot)
            : -1;

        if (shopData != null && shopSlot < 0)
        {
            GD.PushWarning("[Map] 路径上没有多余的槽位放商店房间，本次不生成商店房间");
        }

        GD.Print($"[Map] 直线地图：普通房间{normalCount}个、路径槽位{slotCount}个、" +
                 $"资源房间槽位={(treasureSlot >= 0 ? treasureSlot.ToString() : "无")}、" +
                 $"商店房间槽位={(shopSlot >= 0 ? shopSlot.ToString() : "无")}");

        // 3. 起始房间
        Room start = CreateRoomInstance(CalculateLineRoomPosition(0), startData, "Room_Start");
        if (start == null)
        {
            GD.PrintErr("[Map] 起始房间创建失败，地图生成中止");
            return;
        }

        startRoom = start;
        rooms.Add(start);

        // 4. 按槽位顺序创建路径上的房间
        Array<Room> path = new();
        for (int slot = 0; slot < slotCount; slot++)
        {
            RoomData roomData;
            string roomName;

            if (slot == treasureSlot)
            {
                roomData = treasureData;
                roomName = "Room_Treasure";
            }
            else if (slot == shopSlot)
            {
                roomData = shopData;
                roomName = "Room_Shop";
            }
            else
            {
                roomData = PickRoomData(RoomTag.Normal);
                roomName = $"Room_Normal_{slot:00}";
            }

            Room room = CreateRoomInstance(CalculateLineRoomPosition(slot + 1), roomData, roomName);
            if (room == null)
            {
                continue;
            }

            path.Add(room);
            rooms.Add(room);

            if (slot == treasureSlot)
            {
                treasureRoom = room;
            }
            else if (slot == shopSlot)
            {
                shopRoom = room;
            }
        }

        // 5. Boss房间：直线地图里Boss是路径的最后一段
        if (bossData != null)
        {
            bossRoom = CreateRoomInstance(CalculateLineRoomPosition(slotCount + 1), bossData, "Room_Boss");
            if (bossRoom != null)
            {
                rooms.Add(bossRoom);
            }
        }

        // 6. 依次串联成一条直线
        Room previous = start;
        foreach (Room room in path)
        {
            LinkLineRoom(previous, room);
            previous = room;
        }

        if (bossRoom != null)
        {
            LinkLineRoom(previous, bossRoom);
        }
        else
        {
            GD.PushWarning("[Map] 直线末端没有Boss房间，地图到此结束");
        }

        // 7. layers按行进顺序记录（直线地图每层只有一个房间）
        layers.Add(new Array<Room>() { start });
        foreach (Room room in path)
        {
            layers.Add(new Array<Room>() { room });
        }
        if (bossRoom != null)
        {
            layers.Add(new Array<Room>() { bossRoom });
        }
    }

    /// <summary>
    /// 掷出本次直线的普通房间数量。
    /// 范围取自Map的minNormalRoomCount~maxNormalRoomCount；
    /// 两者都≤0时退回MapData.normalRoomCount作为固定数量（兼容没有配置范围的旧数据）
    /// </summary>
    private int RollNormalRoomCount()
    {
        int min = Mathf.Max(0, minNormalRoomCount);
        int max = Mathf.Max(0, maxNormalRoomCount);

        if (max <= 0)
        {
            int fallback = data != null ? data.normalRoomCount : 5;
            fallback = Mathf.Max(1, fallback);
            GD.Print($"[Map] 未设置普通房间数量范围，改用MapData.normalRoomCount={fallback}");
            return fallback;
        }

        // 写反了也能用：自动交换上下限
        if (max < min)
        {
            (min, max) = (max, min);
        }

        return RandomInt(min, max);
    }

    /// <summary>
    /// 按"离某一端的距离"加权抽取一个路径槽位。
    /// 权重 = pow(1 - bias, distance)，distance是该槽位与指定端之间隔着的槽位数：
    ///   - bias = 0：所有槽位权重都是1，等价于完全随机
    ///   - bias 越大：越靠近指定端的槽位权重越高（bias = 1时只会抽到最靠近该端的槽位）
    /// </summary>
    /// <param name="slotCount">路径槽位总数（不含起始房间与Boss房间）</param>
    /// <param name="fromStart">true = 越靠近起始房间越容易抽到；false = 越靠近Boss房间越容易抽到</param>
    /// <param name="bias">位置倾向强度（0~1）</param>
    /// <param name="excludeSlot">需要排除的槽位（已被另一个特殊房间占用），传-1表示不排除</param>
    /// <returns>抽到的槽位索引；没有可用槽位时返回-1</returns>
    private int PickSlotByDistancePreference(int slotCount, bool fromStart, float bias, int excludeSlot = -1)
    {
        if (slotCount <= 0)
        {
            return -1;
        }

        float ratio = 1f - Mathf.Clamp(bias, 0f, 1f);

        float[] weights = new float[slotCount];
        float total = 0f;
        for (int slot = 0; slot < slotCount; slot++)
        {
            if (slot == excludeSlot)
            {
                continue;
            }

            int distance = fromStart ? slot : slotCount - 1 - slot;
            weights[slot] = Mathf.Pow(ratio, distance);
            total += weights[slot];
        }

        // 极端配置（bias=1且最靠近该端的槽位已被占用）下所有权重都是0：
        // 退化成在剩余槽位里均匀抽取，保证特殊房间不会因此消失
        if (total <= 0f)
        {
            System.Collections.Generic.List<int> candidates = new();
            for (int slot = 0; slot < slotCount; slot++)
            {
                if (slot != excludeSlot)
                {
                    candidates.Add(slot);
                }
            }

            if (candidates.Count <= 0)
            {
                return -1;
            }

            return candidates[RandomInt(0, candidates.Count - 1)];
        }

        float roll = RandomFloat(0f, total);
        float accumulated = 0f;
        for (int slot = 0; slot < slotCount; slot++)
        {
            if (weights[slot] <= 0f)
            {
                continue;
            }

            accumulated += weights[slot];
            if (roll <= accumulated)
            {
                return slot;
            }
        }

        // 浮点误差兜底：返回权重最大的槽位
        int bestSlot = -1;
        float bestWeight = -1f;
        for (int slot = 0; slot < slotCount; slot++)
        {
            if (weights[slot] > bestWeight)
            {
                bestWeight = weights[slot];
                bestSlot = slot;
            }
        }

        return bestSlot;
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
    /// 把房间连向下一个房间：先占用一扇门保证串联，再把剩余的空闲门也指向同一个目标，避免出现死门。
    /// 直线地图里每个房间都只有"下一个"这一个去向，所以多余的门也一起连过去
    /// </summary>
    private static void LinkLineRoom(Room previous, Room next)
    {
        if (previous == null || next == null)
        {
            return;
        }

        previous.SetNextRoom(next);
        LinkAllFreeDoors(previous, next);
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
    /// 直线模式下房间的位置：从起始房间开始沿X轴依次排开（0是起始房间，末尾是Boss房间）。
    /// 房间之间靠门的传送移动，世界坐标只用于摆放与在编辑器里观察地图形状
    /// </summary>
    /// <param name="mainIndex">房间在直线上的序号</param>
    private Vector2 CalculateLineRoomPosition(int mainIndex)
    {
        return new Vector2(mainIndex * ROOM_SPACING_X, 0f);
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
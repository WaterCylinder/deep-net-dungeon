using Godot;
using Godot.Collections;
using System;

/// <summary>
/// Map类用于生成游戏地图。
/// 1. 数据来自MapData（房间数据、生成模式与各种规模参数）。
/// 2. 房间之间在画面上没有明显链接，玩家在房间之间移动依赖门之间的传送。
/// 3. 生成模式由MapData.mapMode决定：
///    - Line（默认，直线地图）：起始房间 → [若干普通房间 + 1个资源房间 + 1个商店房间 混排] → Boss房间。
///      普通房间数量在MapData.minNormalRoomCount~MapData.maxNormalRoomCount之间随机；
///      资源房间越靠近起始房间越容易抽到，商店房间越靠近Boss房间越容易抽到。
///      所有房间单向串联成一条直线，没有支路。
///    - Tree（纺锤形树状地图）：起始房间 → 逐层变宽的中间层 → 逐层收窄 → Boss房间。
///      中间房间数量由MapData的普通/资源/商店房间数量决定，每个房间最多2入2出；
///      资源房间越靠近起始房间越容易抽到，商店房间越靠近Boss房间越容易抽到。
/// 4. 不管哪种模式，都会尽量把房间的空闲普通门连掉，避免出现没有目标的"死门"；
///    纺锤模式允许房间有多条入口/出口，房间之间用门对门显式配对（Door.PairWith）。
/// 5. 首先生成房间并缓存（房间会自动初始化其中的门），然后设置门的目标来链接所有房间。
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
    /// 房间之间的垂直偏移幅度（用于纺锤形布局的层内排列）
    /// </summary>
    private const float ROOM_SPACING_Y = 1980f;

    /// <summary>
    /// 单个房间最多允许的入口数/出口数。
    /// 纺锤形生成时按这个上限控制每个房间连出的边数，房间本身可以摆更多门，多出来的门保持关闭
    /// </summary>
    private const int MAX_DOORS_PER_ROOM = 2;

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

        //清除所有已存在的房间
        foreach(Room r in rooms)
        {
            r.QueueFree();
        }
        //清除所有掉落的道具
        foreach(var node in GameManager.Instance.game.dropItemNode.GetChildren())
        {
            if(node is Item item)
            {
                item.QueueFree();
            }
        }
        //清除所有敌怪
        foreach(var node in GameManager.Instance.game.enemyNode.GetChildren())
        {
            if(node is Enemy enemy)
            {
                enemy.QueueFree();
            }
        }

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
                GenerateSpindleMap();
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

        // 检查门的绑定情况：
        // - 未被使用的门是正常现象（纺锤模式下房间可以有2扇门但只用到1扇），只统计不逐扇告警
        // - 真正需要告警的是"一个出口门都没绑定的房间"，那说明它没有去向，玩家会被困住
        int unboundDoorCount = 0;
        int deadEndRoomCount = 0;
        foreach (Room room in rooms)
        {
            if (room == null || room.nextDoors == null)
            {
                continue;
            }

            int boundCount = 0;
            foreach (Door door in room.nextDoors)
            {
                if (door != null)
                {
                    if (door.IsBound)
                    {
                        boundCount++;
                    }
                    else
                    {
                        unboundDoorCount++;
                    }
                }
            }

            if (boundCount <= 0)
            {
                deadEndRoomCount++;
                GD.PushWarning($"[Map] {room.Name} 没有任何已绑定的出口门，玩家进入后将无法离开");
            }
        }

        GD.Print($"[Map] 地图生成完成（{data.mapMode}）：共{rooms.Count}个房间、{layers.Count}层、普通房间{mapSize}个" +
                 $"（资源房间={(treasureRoom != null ? "有" : "无")}，商店房间={(shopRoom != null ? "有" : "无")}，Boss={(bossRoom != null ? "有" : "无")}" +
                 $"，未使用的门{unboundDoorCount}扇，无出口房间{deadEndRoomCount}个）");
    }

    /// <summary>
    /// 直线地图：起始房间 → [若干普通房间 + 1个资源房间 + 1个商店房间 混排] → Boss房间。
    /// - 普通房间数量在MapData的minNormalRoomCount~maxNormalRoomCount之间随机
    /// - 资源房间与商店房间各只有一个，随机夹杂在路径中间，且不会落在同一个槽位上
    /// - 抽取槽位带权重：资源房间越靠近起始房间概率越高，商店房间越靠近Boss房间概率越高
    ///   （强度由MapData.treasureNearStartBias / shopNearBossBias控制）
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
            ? PickSlotByDistancePreference(slotCount, true, data.treasureNearStartBias)
            : -1;
        int shopSlot = shopData != null
            ? PickSlotByDistancePreference(slotCount, false, data.shopNearBossBias, treasureSlot)
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
    /// 范围取自MapData的minNormalRoomCount~maxNormalRoomCount（数据缺失时按0~0处理，即没有普通房间）
    /// </summary>
    private int RollNormalRoomCount()
    {
        if (data == null)
        {
            GD.PrintErr("[Map] MapData为空，无法确定普通房间数量");
            return 0;
        }

        int min = Mathf.Max(0, data.minNormalRoomCount);
        int max = Mathf.Max(0, data.maxNormalRoomCount);

        // 写反了也能用：自动交换上下限
        if (max < min)
        {
            (min, max) = (max, min);
        }

        return RandomInt(min, max);
    }

    /// <summary>
    /// 按"离某一端的距离"加权抽取一个路径槽位（直线模式的便捷入口）。
    /// 距离按槽位序号算：起始端 = 槽位序号，Boss端 = 反向序号
    /// </summary>
    /// <param name="slotCount">路径槽位总数（不含起始房间与Boss房间）</param>
    /// <param name="fromStart">true = 越靠近起始房间越容易抽到；false = 越靠近Boss房间越容易抽到</param>
    /// <param name="bias">位置倾向强度（0~1）</param>
    /// <param name="excludeSlot">需要排除的槽位（已被另一个特殊房间占用），传-1表示不排除</param>
    /// <returns>抽到的槽位索引；没有可用槽位时返回-1</returns>
    private int PickSlotByDistancePreference(int slotCount, bool fromStart, float bias, int excludeSlot = -1)
    {
        System.Collections.Generic.HashSet<int> excluded = excludeSlot >= 0
            ? new System.Collections.Generic.HashSet<int> { excludeSlot }
            : null;

        return PickWeightedSlotByDistance(
            slotCount,
            fromStart
                ? (System.Func<int, int>)(slot => slot)
                : (slot => slotCount - 1 - slot),
            bias,
            excluded);
    }

    /// <summary>
    /// 按距离权重抽取一个槽位，权重 = pow(1 - bias, distance)：
    ///   - bias = 0：所有槽位权重都是1，等价于完全随机
    ///   - bias 越大：距离参考端越近的槽位权重越高（bias = 1时只会抽到距离0的槽位）
    ///
    /// 距离由调用方给出：直线模式是"槽位到端点的槽位数"，纺锤模式是"槽位到端点的层数"，
    /// 两种模式因此共用同一套权重公式
    /// </summary>
    /// <param name="slotCount">候选槽位总数</param>
    /// <param name="distanceOf">槽位 → 到参考端的距离（0表示最靠近参考端）</param>
    /// <param name="bias">位置倾向强度（0~1）</param>
    /// <param name="excluded">需要排除的槽位集合（可为null）</param>
    /// <returns>抽到的槽位索引；没有可用槽位时返回-1</returns>
    private int PickWeightedSlotByDistance(int slotCount, System.Func<int, int> distanceOf,
        float bias, System.Collections.Generic.HashSet<int> excluded = null)
    {
        if (slotCount <= 0 || distanceOf == null)
        {
            return -1;
        }

        float ratio = 1f - Mathf.Clamp(bias, 0f, 1f);

        float[] weights = new float[slotCount];
        float total = 0f;
        for (int slot = 0; slot < slotCount; slot++)
        {
            if (excluded != null && excluded.Contains(slot))
            {
                continue;
            }

            weights[slot] = Mathf.Pow(ratio, Mathf.Max(0, distanceOf(slot)));
            total += weights[slot];
        }

        // 极端配置（bias=1且最靠近该端的槽位已被占用）下所有权重都是0：
        // 退化成在剩余槽位里均匀抽取，保证特殊房间不会因此消失
        if (total <= 0f)
        {
            System.Collections.Generic.List<int> candidates = new();
            for (int slot = 0; slot < slotCount; slot++)
            {
                if (excluded != null && excluded.Contains(slot))
                {
                    continue;
                }
                candidates.Add(slot);
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
    /// 按距离权重抽取count个互不重复的槽位，抽中的槽位会同时加进usedSlots（供后续抽取排除）。
    /// 纺锤模式用它分别抽取资源房间与商店房间所在的槽位
    /// </summary>
    private System.Collections.Generic.List<int> PickWeightedSlots(int slotCount, int count,
        System.Func<int, int> distanceOf, float bias, System.Collections.Generic.HashSet<int> usedSlots)
    {
        System.Collections.Generic.List<int> picked = new();
        if (slotCount <= 0 || count <= 0)
        {
            return picked;
        }

        for (int n = 0; n < count; n++)
        {
            int slot = PickWeightedSlotByDistance(slotCount, distanceOf, bias, usedSlots);
            if (slot < 0)
            {
                break;
            }
            picked.Add(slot);
            usedSlots?.Add(slot);
        }

        return picked;
    }

    /// <summary>
    /// 纺锤形树状地图：起始房间 → 逐层变宽（每层+1）→ 到峰值 → 逐层收窄（每层-1）→ Boss房间。
    ///
    /// 规则：
    /// - 中间房间总数 = 普通房间数 + 资源房间数 + 商店房间数（数量由MapData配置）
    /// - 形状是"逐层±1"的阶梯：第一层宽度 = 起始房间可用的出口数（房间最多2个出口，所以是1或2），
    ///   之后每层+1扩散，到峰值后每层-1收拢，最后一层为1直接接Boss；
    ///   峰值受MapData.maxLayerRooms限制，房间总数不是标准阶梯时峰值层会被加宽
    /// - 每个房间最多2个出口（DoorType.Next）和2个入口（DoorType.Back）；
    ///   房间之间用"门对门"显式配对（Door.PairWith），并且按门的上下位置依次配对，
    ///   所以一个房间可以有多条入口/出口，且"走下面的门去下面的房间"
    /// - 资源房间越靠近起始房间概率越高，商店房间越靠近Boss房间概率越高
    ///   （权重 = pow(1 - bias, 层距)，与直线模式共用同一套公式）
    /// </summary>
    private void GenerateSpindleMap()
    {
        RoomData startData = PickRoomData(RoomTag.Start);
        RoomData bossData = PickRoomData(RoomTag.Boss);

        int normalCount = Mathf.Max(0, data.normalRoomCount);
        int treasureCount = Mathf.Max(0, data.treasureRoomCount);
        int shopCount = Mathf.Max(0, data.shopRoomCount);

        if (normalCount > 0 && !HasRoomData(RoomTag.Normal))
        {
            GD.PushWarning("[Map] 普通房间数据池为空，本次不生成普通房间");
            normalCount = 0;
        }
        if (treasureCount > 0 && !HasRoomData(RoomTag.Treasure))
        {
            GD.PushWarning("[Map] 资源房间数据池为空，本次不生成资源房间");
            treasureCount = 0;
        }
        if (shopCount > 0 && !HasRoomData(RoomTag.Shop))
        {
            GD.PushWarning("[Map] 商店房间数据池为空，本次不生成商店房间");
            shopCount = 0;
        }
        if (bossData == null)
        {
            GD.PushWarning("[Map] 未配置Boss房间数据，纺锤末端将没有Boss房间");
        }

        int innerCount = normalCount + treasureCount + shopCount;

        // 1. 起始房间（先建出来：它的可用出口数决定纺锤第一层能有几个房间）
        Room start = CreateRoomInstance(CalculateSpindlePosition(-1, 0, 1), startData, "Room_Start");
        if (start == null)
        {
            GD.PrintErr("[Map] 起始房间创建失败，地图生成中止");
            return;
        }

        startRoom = start;
        rooms.Add(start);
        layers.Add(new Array<Room>() { start });

        // 2. 纺锤层宽轮廓：从起始房间的出口数开始，每层+1扩散到峰值，再每层-1收拢
        int startExits = CountFreeDoors(start);
        int firstWidth = Mathf.Clamp(startExits, 1, MAX_DOORS_PER_ROOM);
        System.Collections.Generic.List<int> widths = BuildSpindleWidths(innerCount, data.maxLayerRooms, firstWidth);
        int layerCount = widths.Count;

        GD.Print($"[Map] 纺锤形地图：普通{normalCount} 资源{treasureCount} 商店{shopCount}，" +
                 $"起始房间出口{startExits}个→第一层{firstWidth}间，共{layerCount}层，" +
                 $"层宽=[{string.Join(",", widths)}]");

        // 3. 槽位 → 层号，用于按"层距"给特殊房间加权；同时记录每层第一个槽位的序号
        int[] slotLayer = new int[innerCount];
        int[] layerStart = new int[layerCount];
        {
            int slot = 0;
            for (int layer = 0; layer < layerCount; layer++)
            {
                layerStart[layer] = slot;
                for (int k = 0; k < widths[layer]; k++)
                {
                    slotLayer[slot++] = layer;
                }
            }
        }

        // 4. 抽取资源/商店房间所在的槽位：资源偏起始房间、商店偏Boss房间，两者互不重叠
        System.Collections.Generic.HashSet<int> usedSlots = new();
        System.Collections.Generic.List<int> treasureSlots = PickWeightedSlots(
            innerCount, treasureCount, slot => slotLayer[slot], data.treasureNearStartBias, usedSlots);
        System.Collections.Generic.List<int> shopSlots = PickWeightedSlots(
            innerCount, shopCount, slot => layerCount - 1 - slotLayer[slot], data.shopNearBossBias, usedSlots);

        // 5. 逐层创建中间房间
        Array<Array<Room>> innerLayers = new();
        for (int layer = 0; layer < layerCount; layer++)
        {
            Array<Room> layerRooms = new();
            for (int k = 0; k < widths[layer]; k++)
            {
                int slot = layerStart[layer] + k;

                RoomData roomData;
                string roomName;
                if (treasureSlots.Contains(slot))
                {
                    roomData = PickRoomData(RoomTag.Treasure);
                    roomName = $"Room_Treasure_{layer}_{k}";
                }
                else if (shopSlots.Contains(slot))
                {
                    roomData = PickRoomData(RoomTag.Shop);
                    roomName = $"Room_Shop_{layer}_{k}";
                }
                else
                {
                    roomData = PickRoomData(RoomTag.Normal);
                    roomName = $"Room_Normal_{layer}_{k}";
                }

                Room room = CreateRoomInstance(CalculateSpindlePosition(layer, k, widths[layer]), roomData, roomName);
                if (room == null)
                {
                    continue;
                }

                layerRooms.Add(room);
                rooms.Add(room);

                //MapUI按房间角色取色：有多个资源/商店房间时，其余房间靠RoomTag着色
                if (treasureRoom == null && treasureSlots.Contains(slot))
                {
                    treasureRoom = room;
                }
                else if (shopRoom == null && shopSlots.Contains(slot))
                {
                    shopRoom = room;
                }
            }

            innerLayers.Add(layerRooms);
            layers.Add(layerRooms);
        }

        // 6. Boss房间
        if (bossData != null)
        {
            bossRoom = CreateRoomInstance(CalculateSpindlePosition(layerCount, 0, 1), bossData, "Room_Boss");
            if (bossRoom != null)
            {
                rooms.Add(bossRoom);
                layers.Add(new Array<Room>() { bossRoom });
            }
        }

        // 7. 逐层连接：起始 → 第0层 → … → 最后一层 → Boss
        Array<Room> previous = new() { start };
        foreach (Array<Room> layerRooms in innerLayers)
        {
            ConnectLayers(previous, layerRooms);
            previous = layerRooms;
        }

        if (bossRoom != null)
        {
            ConnectLayers(previous, new Array<Room>() { bossRoom });
        }
    }

    /// <summary>
    /// 生成纺锤形的层宽轮廓：从起始房间能连出的房间数开始，**每层+1** 扩散到峰值，
    /// 再 **每层-1** 收拢到1，最后接Boss。即：
    ///   first, first+1, first+2, …, 峰值, …, 3, 2, 1
    /// first 取起始房间的空闲出口数（房间最多2个出口，所以 first 只会是 1 或 2）。
    ///
    /// 房间总数量由MapData配置，若比阶梯形状多出一些，多出来的房间会加厚峰值层（中段平段），
    /// 不会打乱"逐层±1"的扩散/收拢节奏；峰值受maxLayerRooms限制。
    /// </summary>
    /// <param name="totalRooms">中间房间总数</param>
    /// <param name="maxWidth">最宽一层的房间数（峰值上限）</param>
    /// <param name="firstWidth">第一层中间房间的数量（=起始房间能为几个房间开门）</param>
    private System.Collections.Generic.List<int> BuildSpindleWidths(int totalRooms, int maxWidth, int firstWidth)
    {
        System.Collections.Generic.List<int> widths = new();
        if (totalRooms <= 0)
        {
            return widths;
        }

        int cap = Mathf.Max(1, maxWidth);
        int first = Mathf.Clamp(firstWidth, 1, cap);

        // 阶梯形（first起每层+1到峰值，再每层-1到1）占用的房间数
        // = (first+…+peak) + (peak-1 + … + 1)
        int StaircaseSum(int peak)
        {
            int up = (first + peak) * (peak - first + 1) / 2;
            int down = peak * (peak - 1) / 2;
            return up + down;
        }

        // 选峰值：在"阶梯能放下"的前提下取最大的峰值（不超过maxLayerRooms）
        int bestPeak = -1;
        for (int peak = Mathf.Min(cap, 64); peak >= first; peak--)
        {
            if (StaircaseSum(peak) <= totalRooms)
            {
                bestPeak = peak;
                break;
            }
        }

        // 连最小的阶梯都放不下（房间数太少）：退化成每层1间的一条线，保证房间总数不丢、且一定连通
        if (bestPeak < 0)
        {
            for (int i = 0; i < totalRooms; i++)
            {
                widths.Add(1);
            }
            return widths;
        }

        // 基础阶梯：上升段 first … peak，收拢段 peak-1 … 1
        for (int w = first; w <= bestPeak; w++)
        {
            widths.Add(w);
        }
        int peakIndex = widths.Count - 1;
        for (int w = bestPeak - 1; w >= 1; w--)
        {
            widths.Add(w);
        }

        // 多出来的房间：插入"平段层"来吸收——宽度不超过峰值，且插在同宽的层后面。
        // 这样层宽序列仍然先逐层变宽、再逐层收窄（只是多了几段平段），
        // 收拢段也不会出现"3个房间直接连1个房间"这种门数不够的跳变
        int remainder = totalRooms - StaircaseSum(bestPeak);
        int guard = 0;
        while (remainder > 0 && guard++ < 4096)
        {
            int width = Mathf.Min(remainder, bestPeak);
            int insertAt = FindPlateauInsertIndex(widths, width, peakIndex);
            widths.Insert(insertAt, width);
            remainder -= width;
        }

        if (totalRooms != StaircaseSum(bestPeak))
        {
            GD.Print($"[Map] 纺锤形：房间数{totalRooms}不是标准阶梯形状（峰值{bestPeak}的标准形状需要{StaircaseSum(bestPeak)}间，" +
                     $"峰值{bestPeak + 1}需要{StaircaseSum(bestPeak + 1)}间），已用平段层补足");
        }

        // 校验收拢/扩散节奏是否还在"每间房最多2入2出"的能力范围内：
        // 相邻两层a→b时，需要 a ≤ 2b（下层每间最多2个入口）且 b ≤ 2a（上层每间最多2个出口）
        for (int i = 0; i + 1 < widths.Count; i++)
        {
            int a = widths[i];
            int b = widths[i + 1];
            if (a > 2 * b || b > 2 * a)
            {
                GD.PushWarning($"[Map] 纺锤形层宽不合法：第{i}层{a}间 → 第{i + 1}层{b}间，" +
                               $"门的数量不够（每房间最多2个入口/出口），部分房间会不可达");
            }
        }

        return widths;
    }

    /// <summary>
    /// 找到插入"平段层"的位置：从峰值层往后找第一层宽度等于width的层，插在它后面。
    /// 插在同宽的层后面不会破坏"相邻层宽度差不超过一倍"的门数约束，
    /// 也能保持"先逐层变宽、再逐层收窄"的纺锤形状
    /// </summary>
    private static int FindPlateauInsertIndex(System.Collections.Generic.List<int> widths, int width, int peakIndex)
    {
        for (int i = Mathf.Max(0, peakIndex); i < widths.Count; i++)
        {
            if (widths[i] == width)
            {
                return i + 1;
            }
        }

        // 找不到同宽的层（正常不会发生）：接在峰值层后面
        return Mathf.Clamp(peakIndex + 1, 0, widths.Count);
    }

    /// <summary>
    /// 把相邻两层连接起来（最多2入2出的编织结构）：
    /// 1. 先决定"谁连谁"：下层每个房间至少1个入口、上层每个房间至少1个出口，剩余容量再补第二条边
    /// 2. 再分配具体的门：房间的门按Y从上到下排序，按目标/来源房间的上下顺序依次配对，
    ///    保证"走下面的门去下面的房间"，不会出现上下错位
    /// </summary>
    private void ConnectLayers(Array<Room> fromLayer, Array<Room> toLayer)
    {
        if (fromLayer == null || fromLayer.Count == 0 || toLayer == null || toLayer.Count == 0)
        {
            return;
        }

        int fromCount = fromLayer.Count;
        int toCount = toLayer.Count;

        int[] outLeft = new int[fromCount];
        int[] inLeft = new int[toCount];

        for (int i = 0; i < fromCount; i++)
        {
            outLeft[i] = Mathf.Min(MAX_DOORS_PER_ROOM, CountFreeDoors(fromLayer[i]));
        }
        for (int j = 0; j < toCount; j++)
        {
            inLeft[j] = Mathf.Min(MAX_DOORS_PER_ROOM, CountFreeReturnDoors(toLayer[j]));
        }

        // 本次要建立的连接（房间下标对），先定好拓扑，最后再统一分配具体的门
        System.Collections.Generic.List<(int from, int to)> links = new();
        bool[] hasOut = new bool[fromCount];
        bool[] hasIn = new bool[toCount];

        bool HasLink(int from, int to)
        {
            foreach ((int f, int t) in links)
            {
                if (f == from && t == to)
                {
                    return true;
                }
            }
            return false;
        }

        void AddLink(int from, int to)
        {
            links.Add((from, to));
            outLeft[from]--;
            inLeft[to]--;
            hasOut[from] = true;
            hasIn[to] = true;
        }

        // 1. 单调映射：逐个下层房间，按比例找出它应该连的1~2个上层房间（区间左右边界）。
        //    这样连线像一条从左下到右上的阶梯：上面的房间连上面的房间、下面的房间连下面的房间；
        //    上下两层等宽时就是干净的1对1直连，不会出现"6、7交叉连到8、9"这种穿插
        for (int j = 0; j < toCount; j++)
        {
            int lowParent = Mathf.Clamp(j * fromCount / toCount, 0, fromCount - 1);
            int highParent = Mathf.Clamp(((j + 1) * fromCount + toCount - 1) / toCount - 1, 0, fromCount - 1);
            if (highParent < lowParent)
            {
                highParent = lowParent;
            }

            // 一个下层房间最多接2个入口
            highParent = Mathf.Min(highParent, lowParent + MAX_DOORS_PER_ROOM - 1);

            for (int i = lowParent; i <= highParent; i++)
            {
                if (inLeft[j] <= 0)
                {
                    break;
                }

                if (outLeft[i] <= 0 || HasLink(i, j))
                {
                    continue;
                }

                AddLink(i, j);
            }
        }

        // 2. 兜底：仍有下层房间没入口（通常是预制体门不够），从就近的上层房间补一条
        for (int j = 0; j < toCount; j++)
        {
            if (hasIn[j])
            {
                continue;
            }

            int parent = FindClosestWithCapacity(outLeft, MapIndex(j, toCount, fromCount));
            if (parent < 0)
            {
                GD.PushWarning($"[Map] 上层没有空闲出口可以连接到 {toLayer[j]?.Name}，该房间可能不可达");
                continue;
            }

            AddLink(parent, j);
        }

        // 3. 兜底：仍有上层房间没出口（死胡同）
        for (int i = 0; i < fromCount; i++)
        {
            if (hasOut[i] || outLeft[i] <= 0)
            {
                continue;
            }

            int child = FindClosestWithCapacity(inLeft, MapIndex(i, fromCount, toCount));
            if (child < 0 || HasLink(i, child))
            {
                continue;
            }

            AddLink(i, child);
        }

        // 4. 统一分配具体的门：按上下位置一一配对
        PairDoorsByPosition(fromLayer, toLayer, links);
    }

    /// <summary>
    /// 给已经决定的连接分配具体的门，保证上下位置对得上：
    /// 两趟分别给上下两层分配门，保证每条连接的两端上下"镜像"：
    /// - 上层：每个房间的连接按"目标行号升序"处理，出口门按Y从上到下依次使用
    ///   （上面的出口去上面的房间，下面的出口去下面的房间）
    /// - 下层：每个房间的连接按"来源行号降序"处理，入口门按Y从下到上依次使用
    ///   （入口在房间另一侧，所以靠上的来源接到靠下的入口）
    /// 于是"走靠下面的出口"正好落到"对面房间靠上面的入口"，反过来也成立
    /// </summary>
    private static void PairDoorsByPosition(Array<Room> fromLayer, Array<Room> toLayer,
        System.Collections.Generic.List<(int from, int to)> links)
    {
        if (links == null || links.Count == 0)
        {
            return;
        }

        int fromCount = fromLayer.Count;
        int toCount = toLayer.Count;

        // 每个房间还空着门的候选表（取走一扇就从表里删掉）
        System.Collections.Generic.List<Door>[] outDoors = new System.Collections.Generic.List<Door>[fromCount];
        System.Collections.Generic.List<Door>[] inDoors = new System.Collections.Generic.List<Door>[toCount];
        for (int i = 0; i < fromCount; i++)
        {
            outDoors[i] = FreeDoors(fromLayer[i], false);
        }
        for (int j = 0; j < toCount; j++)
        {
            inDoors[j] = FreeDoors(toLayer[j], true);
        }

        // 每条连接两端各选"朝向对方"的那扇门：
        // 对面的房间在下方就用下侧的门，在上方就用上侧的门。
        // 这样出口和入口正好互为镜像——走靠下的出口，落到对面就是靠上的入口
        int unpaired = 0;
        foreach ((int from, int to) in links)
        {
            // 两个房间在同一高度时没有"朝向"可分，默认按下方处理
            float dir = Mathf.Sign(toLayer[to].Position.Y - fromLayer[from].Position.Y);
            if (Mathf.IsZeroApprox(dir))
            {
                dir = 1f;
            }

            Door outDoor = TakeDoorFacing(outDoors[from], dir);
            Door inDoor = TakeDoorFacing(inDoors[to], -dir);

            if (outDoor == null || inDoor == null)
            {
                unpaired++;
                continue;
            }

            outDoor.PairWith(inDoor);
        }

        if (unpaired > 0)
        {
            GD.PushWarning($"[Map] 有{unpaired}条层间连接没有可用门配对（房间的门不够），这些房间之间不会连通");
        }
    }

    /// <summary>
    /// 从候选门里取"最朝向dir方向"的那扇：dir>0取靠下的门，dir&lt;0取靠上的门，取走后从表里删掉
    /// </summary>
    /// <param name="doors">候选门（会被修改）</param>
    /// <param name="dir">朝向（+1 = 下方，-1 = 上方）</param>
    private static Door TakeDoorFacing(System.Collections.Generic.List<Door> doors, float dir)
    {
        if (doors == null || doors.Count == 0)
        {
            return null;
        }

        int best = 0;
        float bestScore = float.MinValue;
        for (int i = 0; i < doors.Count; i++)
        {
            float score = dir * doors[i].Position.Y;
            if (score > bestScore)
            {
                bestScore = score;
                best = i;
            }
        }

        Door door = doors[best];
        doors.RemoveAt(best);
        return door;
    }

    /// <summary>
    /// 取房间还空着（没有配对）的门
    /// </summary>
    /// <param name="room">房间</param>
    /// <param name="entrance">true = 取入口门（DoorType.Back），false = 取出口门（DoorType.Next）</param>
    private static System.Collections.Generic.List<Door> FreeDoors(Room room, bool entrance)
    {
        System.Collections.Generic.List<Door> result = new();
        if (room == null)
        {
            return result;
        }

        Array<Door> doors = entrance ? room.returnDoors : room.nextDoors;
        if (doors == null)
        {
            return result;
        }

        foreach (Door door in doors)
        {
            if (IsDoorFree(door))
            {
                result.Add(door);
            }
        }

        return result;
    }

    /// <summary>
    /// 把一个序号按比例映射到另一层的序号（层内就近对齐用）
    /// </summary>
    private static int MapIndex(int index, int fromCount, int toCount)
    {
        if (fromCount <= 1 || toCount <= 1)
        {
            return 0;
        }
        return Mathf.Clamp(Mathf.RoundToInt((float)index * (toCount - 1) / (fromCount - 1)), 0, toCount - 1);
    }

    /// <summary>
    /// 从ideal开始向两侧找第一个还有容量的房间
    /// </summary>
    private static int FindClosestWithCapacity(int[] capacity, int ideal)
    {
        if (capacity == null || capacity.Length == 0)
        {
            return -1;
        }

        ideal = Mathf.Clamp(ideal, 0, capacity.Length - 1);
        if (capacity[ideal] > 0)
        {
            return ideal;
        }

        for (int d = 1; d < capacity.Length; d++)
        {
            int right = ideal + d;
            if (right < capacity.Length && capacity[right] > 0)
            {
                return right;
            }

            int left = ideal - d;
            if (left >= 0 && capacity[left] > 0)
            {
                return left;
            }
        }

        return -1;
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
    /// 统计房间还有几个空闲的入口门（DoorType.Back）
    /// </summary>
    private static int CountFreeReturnDoors(Room room)
    {
        if (room == null || room.returnDoors == null)
        {
            return 0;
        }

        int count = 0;
        foreach (Door door in room.returnDoors)
        {
            if (IsDoorFree(door))
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// 门是否还没有配对
    /// </summary>
    private static bool IsDoorFree(Door door)
    {
        return door != null && door.targetRoom == null && door.targetDoor == null;
    }

    /// <summary>
    /// MapData里该类型是否有可用的房间数据
    /// </summary>
    private bool HasRoomData(RoomTag tag)
    {
        if (data == null || !data.roomDatas.ContainsKey(tag))
        {
            return false;
        }

        Array<RoomData> pool = data.roomDatas[tag];
        return pool != null && pool.Count > 0;
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
    /// 纺锤形布局的房间位置：层号决定X（自左而右），层内序号决定Y（以该层中线为轴对称展开）。
    /// 配合逐层变宽/收窄的层宽，编辑器里看就是中间宽两头窄的纺锤形。
    /// 房间之间靠门的传送移动，世界坐标只用于摆放与观察地图形状
    /// </summary>
    /// <param name="layerIndex">层号（-1表示起始房间，layerCount表示Boss房间）</param>
    /// <param name="indexInLayer">层内序号（0起）</param>
    /// <param name="layerWidth">该层的房间数，用于把房间沿Y轴居中排列</param>
    private Vector2 CalculateSpindlePosition(int layerIndex, int indexInLayer, int layerWidth)
    {
        float x = (layerIndex + 1) * ROOM_SPACING_X;
        float y = (indexInLayer - (layerWidth - 1) * 0.5f) * ROOM_SPACING_Y;
        return new Vector2(x, y);
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
            GD.PushWarning($"[Map] MapData里没有{tag}房间的数据池");
            return null;
        }

        Array<RoomData> candidates = data.roomDatas[tag];
        if (candidates == null || candidates.Count == 0)
        {
            GD.PushWarning($"[Map] {tag}房间的数据池是空的");
            return null;
        }

        // 使用随机数系统选取
        RandomSystem randomSystem = GetRandomSystem();
        RoomData picked = randomSystem != null
            ? randomSystem.Pick(candidates, randomGeneratorName)
            : candidates[(int)(GD.Randi() % (ulong)candidates.Count)];

        // 诊断日志：能直接看出池子里有几个候选、这次抽到了哪一个
        // 如果每次都是同一个房间，先看这里的"候选N个"是否正确
        GD.Print($"[Map] 抽取{tag}房间：候选{candidates.Count}个 → " +
                 $"{(picked != null ? picked.roomId : "null")}（{picked?.ResourcePath ?? "无"}）");

        return picked;
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
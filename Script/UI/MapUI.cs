using Godot;
using System.Collections.Generic;

/// <summary>
/// 小地图 UI —— 以树状结构绘制当前地图的所有房间。
///
/// 表现规则：
///   - 每个房间绘制为一个色块，颜色由房间角色决定
///   - 房间之间的连线表示门的连通关系（无向，同一对房间只画一条）
///   - ShowAll = true：所有房间按真实类型着色（全图透视）
///   - ShowAll = false：未进入过（alreadyEntered == false）的房间以灰色色块显示
///
/// 布局规则（自左而右）：
///   直接读Map.layers：层号→列（起始房间在最左，Boss在最右），层内序号→行（以该层中线为0上下展开）。
///   直线的每层只有1个房间、纺锤形的中段一层有多间房，都能如实画出来；
///   层内多间房的左右/大小关系不受门的位置影响，房间有一条还是两条入口都不影响布局。
/// 使用方式：
///   把本脚本挂到任意 Control 节点上，自行设置该节点的位置与大小。
///   绘制内容会自动适配节点矩形：空间不足时间距与色块等比缩小，空间充足时保持设定尺寸并居中。
/// </summary>
public partial class MapUI : Control
{
    #region 绘制参数

    /// <summary>
    /// 是否显示完整地图。
    /// true：所有房间按真实类型着色
    /// false：未进入过的房间以灰色色块显示
    /// </summary>
    [Export] public bool ShowAll = true;

    /// <summary>
    /// 关闭 ShowAll 时是否连未进入过的房间也一并隐藏（只显示已探索部分）。
    /// 仅在 ShowAll = false 时生效。
    /// 注意：布局始终按完整地图计算，隐藏房间只影响绘制，避免探索过程中地图跳动。
    /// </summary>
    [Export] public bool hideUnvisited = false;

    /// <summary>
    /// 可选：直接指定要绘制的 Map。留空则自动使用当前游戏（GameManager.Instance.game）的 map
    /// </summary>
    [Export] public Map map;

    /// <summary>
    /// 是否自动跟随游戏状态刷新。关闭后可手动调用 RebuildLayout()
    /// </summary>
    [Export] public bool autoRefresh = true;

    /// <summary>
    /// 房间色块边长（像素，未缩放时）
    /// </summary>
    [Export] public float blockSize = 22f;

    /// <summary>
    /// 主线方向（横向）相邻房间的色块中心距，即主线自左而右的推进节奏
    /// </summary>
    [Export] public float spacingMain = 44f;

    /// <summary>
    /// 支路方向（纵向）相邻房间的色块中心距，主要影响支路向上下展开的宽度
    /// </summary>
    [Export] public float spacingBranch = 36f;

    /// <summary>
    /// 内容与节点边缘的间距（用于计算可用空间）
    /// </summary>
    [Export] public float padding = 10f;

    /// <summary>
    /// 是否高亮玩家当前所在房间
    /// </summary>
    [Export] public bool highlightCurrentRoom = true;

    /// <summary>
    /// 连线宽度
    /// </summary>
    [Export] public float lineWidth = 2f;

    /// <summary>
    /// 色块描边宽度
    /// </summary>
    [Export] public float borderWidth = 1f;

    /// <summary>
    /// 当前房间高亮描边宽度
    /// </summary>
    [Export] public float currentBorderWidth = 2f;

    #endregion

    #region 颜色参数

    [Export] public Color startColor = new(0.36f, 0.83f, 0.60f);
    [Export] public Color normalColor = new(0.42f, 0.55f, 0.78f);
    [Export] public Color bossColor = new(0.86f, 0.30f, 0.33f);
    [Export] public Color shopColor = new(0.91f, 0.75f, 0.31f);
    [Export] public Color treasureColor = new(0.67f, 0.47f, 0.91f);
    [Export] public Color eventColor = new(0.40f, 0.79f, 0.86f);
    [Export] public Color hiddenColor = new(0.50f, 0.36f, 0.58f);

    /// <summary>
    /// 未进入过的房间色块颜色（ShowAll = false 时使用）
    /// </summary>
    [Export] public Color unvisitedColor = new(0.35f, 0.35f, 0.38f, 0.85f);

    [Export] public Color blockBorderColor = new(0.08f, 0.09f, 0.12f, 0.90f);
    [Export] public Color unvisitedBorderColor = new(0.25f, 0.25f, 0.28f, 0.90f);

    /// <summary>
    /// 通向已知房间的连线颜色
    /// </summary>
    [Export] public Color edgeColor = new(0.75f, 0.80f, 0.90f, 0.70f);

    /// <summary>
    /// 通向未进入房间的连线颜色
    /// </summary>
    [Export] public Color unknownEdgeColor = new(0.45f, 0.45f, 0.50f, 0.50f);

    /// <summary>
    /// 当前房间的高亮描边颜色
    /// </summary>
    [Export] public Color currentRoomColor = new(1f, 1f, 1f, 0.95f);

    /// <summary>
    /// 空地图提示文字的颜色与字号
    /// </summary>
    [Export] public Color hintColor = new(0.80f, 0.80f, 0.80f);
    [Export] public int hintFontSize = 14;

    #endregion

    #region 布局数据

    /// <summary>
    /// 布局节点：一个房间在网格中的坐标。
    /// depth 为层号（0 = 起始房间所在列，向右递增），直接取自Map.layers；
    /// lane 为层内行号（以本层中线为0，向上为负、向下为正，用浮点保证奇数/偶数房间都能居中）
    /// </summary>
    private class MapNode
    {
        public readonly Room room;
        public readonly int depth;
        public readonly float lane;

        public MapNode(Room room, int depth, float lane)
        {
            this.room = room;
            this.depth = depth;
            this.lane = lane;
        }
    }

    /// <summary>
    /// 一条连通关系（无向）
    /// </summary>
    private readonly struct MapEdge
    {
        public readonly MapNode a;
        public readonly MapNode b;

        public MapEdge(MapNode a, MapNode b)
        {
            this.a = a;
            this.b = b;
        }
    }

    private readonly List<MapNode> _nodes = new();
    private readonly List<MapEdge> _edges = new();
    private readonly Dictionary<Room, MapNode> _nodeOf = new();
    private readonly Dictionary<Room, bool> _visitedCache = new();

    private int _minDepth;
    private int _maxDepth;
    private float _minLane;
    private float _maxLane;

    // 刷新检测用的缓存
    private Map _cachedMap;
    private int _cachedRoomCount = -1;
    private Room _cachedCurrentRoom;
    private bool _cachedShowAll;
    private bool _cachedHideUnvisited;

    // 绘制几何（每次 _Draw 时按当前节点尺寸重算）
    private float _drawBlockSize;
    /// <summary>横向步距（主线推进方向）</summary>
    private float _drawMainStep;
    /// <summary>纵向步距（支路展开方向）</summary>
    private float _drawBranchStep;
    private Vector2 _drawOffset;

    #endregion

    #region 生命周期

    public override void _Ready()
    {
        // 节点尺寸变化时重绘（几何在 _Draw 内按 Size 计算）
        Resized += QueueRedraw;

        // 初始先画一次
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (!autoRefresh)
        {
            return;
        }

        Map currentMap = GetMap();

        // 地图实例或房间数量变化：重新布局
        if (currentMap != _cachedMap
            || (currentMap != null && currentMap.rooms.Count != _cachedRoomCount))
        {
            RebuildLayout();
            return;
        }

        // 暴露参数运行时被修改：重绘
        if (ShowAll != _cachedShowAll || hideUnvisited != _cachedHideUnvisited)
        {
            _cachedShowAll = ShowAll;
            _cachedHideUnvisited = hideUnvisited;
            QueueRedraw();
            return;
        }

        // 房间进入状态或当前房间变化：只重绘，不用重新布局
        if (CheckStateChanged())
        {
            QueueRedraw();
        }
    }

    #endregion

    #region 数据来源

    /// <summary>
    /// 获取要绘制的 Map
    /// </summary>
    private Map GetMap()
    {
        if (Game.IsAble(map))
        {
            return map;
        }

        Game game = GameManager.Instance?.game;
        if (game == null)
        {
            return null;
        }

        return Game.IsAble(game.map) ? game.map : null;
    }

    /// <summary>
    /// 获取当前玩家所在房间
    /// </summary>
    private Room GetCurrentRoom()
    {
        Game game = GameManager.Instance?.game;
        if (game == null || !Game.IsAble(game.room))
        {
            return null;
        }
        return game.room;
    }

    /// <summary>
    /// 房间是否已被玩家进入过
    /// </summary>
    private static bool IsVisited(Room room)
    {
        return Game.IsAble(room) && room.alreadyEntered;
    }

    /// <summary>
    /// 获取房间所有"下一房间"的门与目标房间。
    /// 同一房间可能被多个门指向（例如无支路的普通房间两个门都通向主线下一个房间），此处按目标房间去重。
    /// 顺序保持 doorsNode 的节点顺序，因为 Map.SetNextRoom 正是按这个顺序分配主线门与支路门的
    /// </summary>
    private static List<(Door door, Room target)> GetNextDoors(Room room)
    {
        List<(Door door, Room target)> result = new();
        if (!Game.IsAble(room) || room.nextDoors == null)
        {
            return result;
        }

        foreach (Door door in room.nextDoors)
        {
            if (!Game.IsAble(door))
            {
                continue;
            }

            Room target = door.targetRoom;
            if (!Game.IsAble(target) || target == room)
            {
                continue;
            }

            bool duplicated = false;
            for (int i = 0; i < result.Count; i++)
            {
                if (result[i].target == target)
                {
                    duplicated = true;
                    break;
                }
            }

            if (!duplicated)
            {
                result.Add((door, target));
            }
        }

        return result;
    }

    #endregion

    #region 布局生成

    /// <summary>
    /// 重新生成布局。地图变化或需要强制刷新时调用
    /// </summary>
    public void RebuildLayout()
    {
        _nodes.Clear();
        _edges.Clear();
        _nodeOf.Clear();
        _visitedCache.Clear();

        Map currentMap = GetMap();
        _cachedMap = currentMap;
        _cachedRoomCount = currentMap?.rooms.Count ?? 0;
        _cachedShowAll = ShowAll;
        _cachedHideUnvisited = hideUnvisited;
        _cachedCurrentRoom = GetCurrentRoom();

        if (currentMap == null || currentMap.rooms.Count == 0)
        {
            QueueRedraw();
            return;
        }

        // 1. 直接按Map.layers铺格子：层号→列（自左而右），层内序号→行（以该层中线为0上下展开）。
        //    生成器已经把房间按"直线/纺锤"分好层，这里不再靠门的位置去猜拓扑，
        //    因此纺锤形的中间宽两头窄、以及一个房间有多条入口的情况都能如实画出来
        if (currentMap.layers != null)
        {
            for (int layerIndex = 0; layerIndex < currentMap.layers.Count; layerIndex++)
            {
                Godot.Collections.Array<Room> layer = currentMap.layers[layerIndex];
                if (layer == null)
                {
                    continue;
                }

                // 先挑出这一层里的有效房间，用于把整层沿Y轴居中
                List<Room> validRooms = new();
                foreach (Room room in layer)
                {
                    if (Game.IsAble(room) && !_nodeOf.ContainsKey(room))
                    {
                        validRooms.Add(room);
                    }
                }

                for (int i = 0; i < validRooms.Count; i++)
                {
                    float lane = i - (validRooms.Count - 1) * 0.5f;
                    MapNode node = new(validRooms[i], layerIndex, lane);
                    _nodes.Add(node);
                    _nodeOf[validRooms[i]] = node;
                }
            }
        }

        UpdateBounds();

        // 2. 兜底：没有登记在layers里的房间（例如手工摆放或旧数据）放在最右侧的额外一列
        float orphanLane = 0f;
        foreach (Room room in currentMap.rooms)
        {
            if (!Game.IsAble(room) || _nodeOf.ContainsKey(room))
            {
                continue;
            }

            MapNode orphan = new(room, _maxDepth + 1, orphanLane++);
            _nodes.Add(orphan);
            _nodeOf[room] = orphan;
        }

        // 4. 建立连通关系（无向去重）
        HashSet<(MapNode, MapNode)> seenEdges = new();
        foreach (MapNode node in _nodes)
        {
            foreach ((Door _, Room target) in GetNextDoors(node.room))
            {
                if (!_nodeOf.TryGetValue(target, out MapNode other) || other == node)
                {
                    continue;
                }

                // 统一成（层浅, 层深）的顺序，保证同一对房间只画一条连线
                MapNode a = node;
                MapNode b = other;
                if (node.depth > other.depth || (node.depth == other.depth && node.lane > other.lane))
                {
                    a = other;
                    b = node;
                }

                if (!seenEdges.Add((a, b)))
                {
                    continue;
                }

                _edges.Add(new MapEdge(a, b));
            }
        }

        // 5. 记录初始进入状态，供刷新检测比对
        foreach (MapNode node in _nodes)
        {
            _visitedCache[node.room] = node.room.alreadyEntered;
        }

        UpdateBounds();
        QueueRedraw();
    }

    /// <summary>
    /// 重新计算网格范围
    /// </summary>
    private void UpdateBounds()
    {
        if (_nodes.Count == 0)
        {
            _minDepth = _maxDepth = 0;
            _minLane = _maxLane = 0f;
            return;
        }

        _minDepth = _maxDepth = _nodes[0].depth;
        _minLane = _maxLane = _nodes[0].lane;

        foreach (MapNode node in _nodes)
        {
            _minDepth = Mathf.Min(_minDepth, node.depth);
            _maxDepth = Mathf.Max(_maxDepth, node.depth);
            _minLane = Mathf.Min(_minLane, node.lane);
            _maxLane = Mathf.Max(_maxLane, node.lane);
        }
    }

    /// <summary>
    /// 检查房间进入状态与当前房间是否变化
    /// </summary>
    private bool CheckStateChanged()
    {
        bool changed = false;

        foreach (MapNode node in _nodes)
        {
            bool visited = node.room.alreadyEntered;
            if (_visitedCache.TryGetValue(node.room, out bool cached))
            {
                if (cached != visited)
                {
                    _visitedCache[node.room] = visited;
                    changed = true;
                }
            }
            else
            {
                _visitedCache[node.room] = visited;
                changed = true;
            }
        }

        Room currentRoom = GetCurrentRoom();
        if (currentRoom != _cachedCurrentRoom)
        {
            _cachedCurrentRoom = currentRoom;
            changed = true;
        }

        return changed;
    }

    #endregion

    #region 绘制几何

    /// <summary>
    /// 按当前节点尺寸计算绘制几何，空间不足时等比缩小
    /// </summary>
    /// <returns>节点尺寸非法时返回false</returns>
    private bool UpdateGeometry()
    {
        if (Size.X <= 0 || Size.Y <= 0)
        {
            return false;
        }

        // 横向是主线推进方向（depth），纵向是层内展开方向（lane）
        int depths = _maxDepth - _minDepth + 1;
        float lanes = _maxLane - _minLane + 1;

        float contentWidth = (depths - 1) * spacingMain + blockSize;
        float contentHeight = (lanes - 1) * spacingBranch + blockSize;

        float scale = 1f;
        Vector2 available = Size - new Vector2(padding * 2, padding * 2);
        if (available.X > 0 && available.Y > 0 && contentWidth > 0 && contentHeight > 0)
        {
            scale = Mathf.Min(1f, Mathf.Min(available.X / contentWidth, available.Y / contentHeight));
        }

        _drawBlockSize = blockSize * scale;
        _drawMainStep = spacingMain * scale;
        _drawBranchStep = spacingBranch * scale;

        Vector2 content = new(contentWidth * scale, contentHeight * scale);
        _drawOffset = (Size - content) * 0.5f;

        return true;
    }

    /// <summary>
    /// 获取房间色块的绘制矩形（横向由层数决定，纵向由所在行决定）
    /// </summary>
    private Rect2 GetBlockRect(MapNode node)
    {
        Vector2 position = _drawOffset + new Vector2(
            (node.depth - _minDepth) * _drawMainStep,
            (node.lane - _minLane) * _drawBranchStep);

        return new Rect2(position, new Vector2(_drawBlockSize, _drawBlockSize));
    }

    /// <summary>
    /// 获取房间色块的中心点
    /// </summary>
    private Vector2 GetBlockCenter(MapNode node)
    {
        return GetBlockRect(node).GetCenter();
    }

    /// <summary>
    /// 按房间角色取色块颜色。
    /// 优先使用 Map 的角色引用（startRoom / bossRoom / shopRoom / treasureRoom），
    /// 其次按房间标签（RoomTag）判断，最后回退为普通房间色。
    /// 之所以优先用 Map 引用，是因为房间数据里的 roomTags 可能未配置（例如起始房间），
    /// 而 Map 的角色引用在地图生成时一定被赋值。
    /// </summary>
    private Color GetRoomColor(Room room)
    {
        Map currentMap = GetMap();
        if (currentMap != null)
        {
            if (room == currentMap.bossRoom) return bossColor;
            if (room == currentMap.shopRoom) return shopColor;
            if (room == currentMap.treasureRoom) return treasureColor;
            if (room == currentMap.startRoom) return startColor;
        }

        if (room.CheckTag(RoomTag.Boss)) return bossColor;
        if (room.CheckTag(RoomTag.Shop)) return shopColor;
        if (room.CheckTag(RoomTag.Treasure)) return treasureColor;
        if (room.CheckTag(RoomTag.Start)) return startColor;
        if (room.CheckTag(RoomTag.Event)) return eventColor;
        if (room.CheckTag(RoomTag.Hidden)) return hiddenColor;
        return normalColor;
    }

    #endregion

    #region 绘制

    public override void _Draw()
    {
        if (_nodes.Count == 0 || !UpdateGeometry())
        {
            DrawHint("无地图数据");
            return;
        }

        Room currentRoom = GetCurrentRoom();

        // 1. 先画连线，让后面的色块盖住线头
        foreach (MapEdge edge in _edges)
        {
            bool knownA = ShowAll || IsVisited(edge.a.room);
            bool knownB = ShowAll || IsVisited(edge.b.room);

            if (hideUnvisited && !ShowAll && !knownA && !knownB)
            {
                continue;
            }

            Color color = (knownA || knownB) ? edgeColor : unknownEdgeColor;
            DrawLine(GetBlockCenter(edge.a), GetBlockCenter(edge.b), color, lineWidth, true);
        }

        // 2. 画房间色块
        foreach (MapNode node in _nodes)
        {
            bool visited = IsVisited(node.room);
            bool reveal = ShowAll || visited;

            if (!reveal && hideUnvisited)
            {
                continue;
            }

            Rect2 rect = GetBlockRect(node);
            DrawRect(rect, reveal ? GetRoomColor(node.room) : unvisitedColor);
            DrawRect(rect, reveal ? blockBorderColor : unvisitedBorderColor, false, borderWidth);
        }

        // 3. 高亮玩家当前所在房间（玩家所在房间必然已被进入，不会被 hideUnvisited 隐藏）
        if (highlightCurrentRoom
            && Game.IsAble(currentRoom)
            && _nodeOf.TryGetValue(currentRoom, out MapNode current))
        {
            Rect2 rect = GetBlockRect(current).Grow(currentBorderWidth);
            DrawRect(rect, currentRoomColor, false, currentBorderWidth);
        }
    }

    /// <summary>
    /// 绘制居中的提示文字
    /// </summary>
    private void DrawHint(string text)
    {
        if (Size.X <= 0 || Size.Y <= 0)
        {
            return;
        }

        Font font = GetThemeDefaultFont();
        if (font == null)
        {
            return;
        }

        Vector2 textSize = font.GetStringSize(text, HorizontalAlignment.Left, -1, hintFontSize);
        Vector2 position = new((Size.X - textSize.X) * 0.5f, (Size.Y + textSize.Y) * 0.5f);
        DrawString(font, position, text, HorizontalAlignment.Left, -1, hintFontSize, hintColor);
    }

    #endregion
}

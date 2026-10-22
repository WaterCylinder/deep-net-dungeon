using Godot;
using Godot.Collections;

/// <summary>
/// 地图生成模式
/// </summary>
public enum MapMode
{
    /// <summary>
    /// 直线地图：起始房间 → [若干普通房间 + 1个资源房间 + 1个商店房间 混排] → Boss房间，
    /// 所有房间单向串联成一条直线，没有任何支路。
    /// 普通房间数量、资源/商店房间的位置倾向在Map脚本的"直线地图生成"栏目里配置
    /// </summary>
    Line,

    /// <summary>
    /// 树状地图：主干每层1个房间、末端接唯一Boss；主干节点按概率长出支路，支路按概率继续延伸
    /// </summary>
    Tree
}

[GlobalClass]
public partial class MapData : Resource
{
    /// <summary>
    /// 地图生成模式
    /// </summary>
    [Export] public MapMode mapMode = MapMode.Line;

    /// <summary>
    /// 直线模式：普通房间数量。
    /// 现在普通房间数量由Map脚本的minNormalRoomCount~maxNormalRoomCount随机决定，
    /// 这个值只在Map上没配置范围（两者都≤0）时作为兜底数量使用
    /// </summary>
    [Export] public int normalRoomCount = 5;

    /// <summary>
    /// 树状模式：主干的层数（层0是起始房间；主干每层1个房间，单向连接到下一层）。
    /// Boss房间接在主干的最末端，因此主干就是整张地图里最长的那条枝干
    /// </summary>
    [Export] public int treeDepth = 3;

    /// <summary>
    /// 树状模式：分叉概率（0~1）：
    /// - 主干节点：以该概率额外长出一条支路（0 = 纯线性地图；1 = 每个主干节点都分叉）
    /// - 支路节点：以该概率继续向下一层延伸，否则在此结束并汇回主干
    /// </summary>
    [Export(PropertyHint.Range, "0,1,0.05")] public float branchChance = 0.5f;

    /// <summary>
    /// 树状模式：单层房间数软上限，达到上限后本层不再新增支路，防止分叉逐层累积导致房间爆炸
    /// </summary>
    [Export] public int maxLayerRooms = 8;

    /// <summary>
    /// 树状模式：资源房间出现的层（从1开始计数，表示"该层及之后的第一条支路"放资源房间）；≤0 表示不生成。
    /// 直线模式下不使用该参数（直线模式下资源房间按"越靠近起始房间概率越高"随机插在路径上）
    /// </summary>
    [Export] public int treasureLayer = 1;

    /// <summary>
    /// 树状模式：商店房间出现的层（从1开始计数）；≤0 表示放在最后一层。
    /// 直线模式下不使用该参数（直线模式下商店房间按"越靠近Boss房间概率越高"随机插在路径上）
    /// </summary>
    [Export] public int shopLayer = 0;

    [Export] public Dictionary<RoomTag, Array<RoomData>> roomDatas = new()
    {
        {RoomTag.Start, new()},
        {RoomTag.Normal, new()},
        {RoomTag.Shop, new()},
        {RoomTag.Treasure, new()},
        {RoomTag.Boss, new()}
    };
    [Export] public Array<RoomData> specialRoomDatas = new();
}

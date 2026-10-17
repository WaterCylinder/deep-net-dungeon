using Godot;
using Godot.Collections;

/// <summary>
/// 地图生成模式
/// </summary>
public enum MapMode
{
    /// <summary>
    /// 固定地图：起始房间 → N个普通房间 → Boss房间（主线单向连接）。
    /// 商店房间挂在最后一个普通房间的支路上；资源房间随机出现在前面某个普通房间的支路上
    /// </summary>
    Fixed,

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
    [Export] public MapMode mapMode = MapMode.Fixed;

    /// <summary>
    /// 固定模式：主线上的普通房间数量（默认5）。
    /// 主线是 起始房间 → N个普通房间 → Boss房间
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
    /// 固定模式下不使用该参数（固定模式下资源房间随机挂在前面的普通房间上）
    /// </summary>
    [Export] public int treasureLayer = 1;

    /// <summary>
    /// 树状模式：商店房间出现的层（从1开始计数）；≤0 表示放在最后一层。
    /// 固定模式下不使用该参数（固定模式下商店房间固定在最后一个普通房间的支路）
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

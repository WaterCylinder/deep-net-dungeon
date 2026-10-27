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
    /// 普通房间数量、资源/商店房间的位置倾向由本资源上的"直线地图"参数决定
    /// </summary>
    Line,

    /// <summary>
    /// 树状地图：主干每层1个房间、末端接唯一Boss；主干节点按概率长出支路，支路按概率继续延伸
    /// </summary>
    Tree
}

/// <summary>
/// 地图数据（Resource）
///
/// 一张地图的生成规则全部定义在这里：生成模式、规模参数、以及各类房间的数据池。
/// Map脚本只负责按这里的数据执行生成，本身不再保存任何生成参数。
/// </summary>
[GlobalClass]
public partial class MapData : Resource
{
    /// <summary>
    /// 地图生成模式
    /// </summary>
    [Export] public MapMode mapMode = MapMode.Line;

    #region 直线地图

    /// <summary>
    /// 直线模式：普通房间数量的最小值（含）。
    /// 与maxNormalRoomCount一起决定本次直线路径上有几个普通房间（在这个区间里随机）
    /// </summary>
    [ExportCategory("直线地图")]
    [Export] public int minNormalRoomCount = 3;

    /// <summary>
    /// 直线模式：普通房间数量的最大值（含）。
    /// 小于minNormalRoomCount时会自动互换，写反了也不会报错
    /// </summary>
    [Export] public int maxNormalRoomCount = 7;

    #endregion

    #region 树状地图（纺锤形）

    /// <summary>
    /// 纺锤形：普通房间数量（不含起始房间与Boss房间）
    /// </summary>
    [ExportCategory("树状地图（纺锤形）")]
    [Export] public int normalRoomCount = 6;

    /// <summary>
    /// 纺锤形：资源房间数量（越靠近起始房间概率越高，强度见"房间位置倾向"）
    /// </summary>
    [Export] public int treasureRoomCount = 1;

    /// <summary>
    /// 纺锤形：商店房间数量（越靠近Boss房间概率越高，强度见"房间位置倾向"）
    /// </summary>
    [Export] public int shopRoomCount = 1;

    /// <summary>
    /// 纺锤形：事件房间数量。
    /// 事件房间会尽量生成在"道路中央"——离起始房间和Boss房间都尽量远
    /// （集中程度见"房间位置倾向"里的eventMiddleBias）
    /// </summary>
    [Export] public int eventRoomCount = 1;

    /// <summary>
    /// 纺锤形：最宽一层的房间数（峰值上限）。
    /// 层宽从"起始房间可用出口数"开始，每层+1扩散到峰值，再每层-1收拢到1；
    /// 房间总数不是标准阶梯形状时，多出来的房间会加宽峰值层（此时峰值层可能比这个值宽一点）
    /// </summary>
    [Export] public int maxLayerRooms = 3;

    #endregion

    #region 房间位置倾向

    /// <summary>
    /// 资源房间的位置倾向（0~1）：0 = 完全随机，越大越集中在靠近起始房间的位置
    /// （1 = 只会出现在离起始房间最近的槽位/层）。直线与树状模式共用
    /// </summary>
    [ExportCategory("房间位置倾向")]
    [Export(PropertyHint.Range, "0,1,0.05")] public float treasureNearStartBias = 0.5f;

    /// <summary>
    /// 商店房间的位置倾向（0~1）：0 = 完全随机，越大越集中在靠近Boss房间的位置
    /// （1 = 只会出现在离Boss最近的槽位/层）。直线与树状模式共用
    /// </summary>
    [Export(PropertyHint.Range, "0,1,0.05")] public float shopNearBossBias = 0.5f;

    /// <summary>
    /// 事件房间的位置倾向（0~1）：0 = 完全随机，越大越集中在路径中央
    /// （1 = 只会出现在离起始房间与Boss房间都最远的那一层）
    /// </summary>
    [Export(PropertyHint.Range, "0,1,0.05")] public float eventMiddleBias = 0.5f;

    #endregion

    #region 房间数据池

    /// <summary>
    /// 按房间标签分类的房间数据池，生成时从对应池子里随机抽取
    /// </summary>
    [Export] public Dictionary<RoomTag, Array<RoomData>> roomDatas = new()
    {
        {RoomTag.Start, new()},
        {RoomTag.Normal, new()},
        {RoomTag.Shop, new()},
        {RoomTag.Treasure, new()},
        {RoomTag.Boss, new()},
        {RoomTag.Event, new()},
    };

    /// <summary>
    /// 特殊房间数据（供剧情等特殊流程使用）
    /// </summary>
    [Export] public Array<RoomData> specialRoomDatas = new();

    #endregion
}

using Godot;
using Godot.Collections;

/// <summary>
/// 房间数据（Resource）
/// </summary>
[GlobalClass]
public partial class RoomData : Resource
{
    /// <summary>
    /// 房间ID，房间类型的唯一标识符
    /// </summary>
    [Export] public string roomId = "";
    /// <summary>
    /// 房间标签
    /// </summary>
    [Export] public Array<RoomTag> roomTags = new();
    /// <summary>
    /// 房间瓦片地图
    /// </summary>
    [Export] public PackedScene tileMap;
    [Export] public Effect onCreate;
    [Export] public Effect onEnter;
    [Export] public Effect onExit;
    [Export] public Effect onUpdate;
}

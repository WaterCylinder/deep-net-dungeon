using Godot;
using Godot.Collections;

[GlobalClass]
public partial class MapData : Resource
{
    [Export] public int mapSize = 3;
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

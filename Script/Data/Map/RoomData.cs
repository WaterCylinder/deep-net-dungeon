using Godot;
using Godot.Collections;

/// <summary>
/// 房间数据（Resource）
/// </summary>
[GlobalClass]
public partial class RoomData : Resource
{
    [Export] public string roomId = "";
    [Export] public Array<RoomTag> roomType = new();
    [Export] public Vector2I roomSize = new(1920, 1080);
    [Export] public Array<Resource> enemies = new();
    [Export] public Array<Resource> obstacles = new();
    [Export] public int minWaves = 3;
    [Export] public int maxWaves = 5;
}

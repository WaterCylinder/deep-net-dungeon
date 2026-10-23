using Godot;
using System;

[GlobalClass]
public partial class RoomBehaviorDemo : RoomBehavior
{   
    Game game => GameManager.Instance.game;
    public void StartRoomInit(Room room)
    {
        room.Log("初始房间初始化");
        room.CloseAllDoor();
        //room.OpenAllDoor();
        room.tileMap.SpawnItem();
    }
}

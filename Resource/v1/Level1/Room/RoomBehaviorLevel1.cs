using Godot;
using System;

[GlobalClass]
public partial class RoomBehaviorLevel1 : RoomBehavior
{
    public void StartRoomInit(Room room)
    {
        room.Log("初始房间初始化");
        room.CloseAllDoor();
        //room.OpenAllDoor();
        room.tileMap.SpawnItem();
    }

    public void StartToomSetOpenDoor(Room room, Item item)
    {
        if (Game.IsAble(item))
        {
            room.Log("挂载开门事件");
            item.OnPickSignal += (e) => room.OpenAllDoor();
        }
    }
}

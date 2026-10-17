using Godot;
using System;

[GlobalClass]
public partial class RoomBehaviorDemo : Behavior
{   
    Game game => GameManager.Instance.game;
    public void StartRoomInit(Room room)
    {
        room.Log("初始房间初始化");
        room.CloseAllDoor();
        //room.OpenAllDoor();
        room.tileMap.SpawnItem();
    }

    //设置boss实体，用于显示boss血条
    public void SetBoss(Room room, Enemy enemy)
    {
        if (Game.IsAble(enemy))
        {
            if (enemy.CheckTag(EnemyTag.BOSS))
            {
                room.Log($"设置Boss敌怪为{enemy}");
                game.hud.BossHealthBar.Visible = true;
                game.hud.BossHealthBar.SetTarget(enemy);
                enemy.OnDeathSignal += dmg =>
                {
                    game.hud.BossHealthBar.Visible = false;
                };
            }
        }
    }
}

using Godot;
using System;

/// <summary>
/// 经典模式 - 默认的游戏模式
/// 标准的地牢探索玩法
/// </summary>
public partial class TestMode : GameModeBase
{
    public override string ModeName => "Test";

    public override void OnStart()
    {
        //创建测试房间
        Room room = game.SpawnRoom(new Vector2(0, 0), null);
        room.OnEnter();
    }

    public override void OnPhysicsProcess(double delta)
    {
        base.OnPhysicsProcess(delta);
        //对其摄像机到玩家位置
        CameraFollowPlayer();
    }
}
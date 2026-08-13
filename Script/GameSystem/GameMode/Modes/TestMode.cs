using Godot;
using System;

/// <summary>
/// 经典模式 - 默认的游戏模式
/// 标准的地牢探索玩法
/// </summary>
public partial class TestMode : GameModeBase
{
    public override string ModeName => "Test";

    public int a = 0;

    public override void OnStart()
    {   
        //设置摄像机无限移动范围
        camera.posLimitX = new Vector2(-Mathf.Inf, Mathf.Inf);
        camera.posLimitY = new Vector2(-Mathf.Inf, Mathf.Inf);
        //传送时重置摄像机位置
        player.OnTrans += camera.SetPosition;
    }

    public override void OnProcess(double delta)
    {
        base.OnProcess(delta);
        switch (game.A)
        {
            case 0:
            //初始化
            game.Next();
            break;
            case 100:
            //生成地图
            map.Initialize(null, new RandomSystem(Time.GetDateDictFromSystem()));
            map.startRoom.Enter(player);
            game.Next();
            break;
        }
    }

    public override void OnPhysicsProcess(double delta)
    {
        base.OnPhysicsProcess(delta);
        //对其摄像机到玩家位置
        CameraFollowPlayer();
    }
}
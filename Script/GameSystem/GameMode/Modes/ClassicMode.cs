using Godot;
using System;

/// <summary>
/// 经典模式 - 默认的游戏模式
/// 标准的地牢探索玩法
/// </summary>
public partial class ClassicMode : GameModeBase
{
    public override string ModeName => "Classic";

    public override void OnStart()
    {
        base.OnStart();
        //设置摄像机无限移动范围
        camera.posLimitX = new Vector2(-Mathf.Inf, Mathf.Inf);
        camera.posLimitY = new Vector2(-Mathf.Inf, Mathf.Inf);
        //传送时重置摄像机位置
        player.OnTrans += camera.SetPosition;
        //打开黑幕
        game.hud.SetBlack(0);
    }

    void Init()
    {
    }

    public override void OnProcess(double delta)
    {
        base.OnProcess(delta);
        switch (game.A)
        {
            case 0:
                //初始化生成地图
                Init();
                map.Initialize(null, game.randomSystem);
                game.camera.Zoom = new Vector2(5,5);
                game.camera.targetZoom = 5;
                game.Next();
            break;
            case 100:
                //等待黑幕
                map.startRoom.Enter(player);
                player.opera += 1;
                player.animeNode.Play("prone");
                player.animeNode.Connect(
                    AnimatedSprite2D.SignalName.AnimationFinished,
                    Callable.From(() => game.Next()),
                    (uint)ConnectFlags.OneShot
                );
                game.hud.SetBlack(0.1f);
                game.Next();
            break;
            case 200:
                //等待动画
            break;
            case 300:
                player.opera -= 1;
                game.camera.targetZoom = 1;
                game.hud.BlackOpen(0.1f);
                game.Next();
            break;
            case 400:
                game.hud.ShowTips(0);
                game.Next();
            break;
        }
    }

    public override void OnPlayerDeath()
    {
        base.OnPlayerDeath();
        GD.Print("玩家死亡");
        //默认进入死亡界面
        game.hud.endUi.Visible = true;
        game.hud.endUi.endText.Text = "失败";
    }

    public override void OnPhysicsProcess(double delta)
    {
        base.OnPhysicsProcess(delta);
        //对其摄像机到玩家位置
        CameraFollowPlayer();
    }

    public override void OnRoomCleared(Room room)
    {
        base.OnRoomCleared(room);
        foreach(var point in game.energyNode.GetChildren())
        {
            if(point is EnergyPoint ep)
            {
                if(ep.energyType == EnergyType.Coin)
                {
                    ep.target = player;
                }
            }
        }
    }
}
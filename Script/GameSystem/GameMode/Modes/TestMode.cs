using Godot;
using System;

/// <summary>
/// 经典模式 - 默认的游戏模式
/// 标准的地牢探索玩法
/// </summary>
public partial class TestMode : GameModeBase
{
    Player player => GameManager.Instance.game.player;
    public override string ModeName => "Test";

    public override void OnProcess(double delta)
    {
        base.OnProcess(delta);
        InputManager input = InputManager.Instance;
        // 测试模式：每帧处理逻辑
        if(input.IsActionJustPressed(InputManager.Actions.ActiveSkill))
        {
            Vector2 toward = input.GetMouseWorldPosition() - player.Position;
            Bullet but = Bullet.Create(
                player,
                player.Position,
                toward * 500f,
                null
            );
        }
    }
}
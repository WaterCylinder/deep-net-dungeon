using Godot;
using System;

[GlobalClass]
public partial class EnemyBehaviorDemo : EnemyBehavior
{
    /// <summary>
    /// 朝向玩家移动
    /// </summary>
    /// <param name="enemy"></param>
    public void UpdateCloseToPalyer(Enemy enemy)
    {
        if(!Game.IsAble(player))return;
        //朝着玩家移动
        enemy.MoveTarget(player.GlobalPosition);
    }

    /// <summary>
    /// 和玩家保持一定距离
    /// </summary>
    /// <param name="enemy"></param>
    public void KeepDistanceToPlayer(Enemy enemy)
    {
        if(!Game.IsAble(player))return;

        float distance = Arg<float>("distance", 256);

        Vector2 diff = player.GlobalPosition - enemy.GlobalPosition;
        if(diff.Length() > distance)
        {
            //朝着玩家移动
            enemy.MoveTarget(player.GlobalPosition);
        }
        else
        {
            enemy.MoveToward(diff * -1);
        }
    }

    public void AttackShotPlayer(Enemy enemy)
    {
        if(player == null || !IsInstanceValid(player) || player.IsQueuedForDeletion())return;

        Vector2 toward = player.GlobalPosition - enemy.GlobalPosition;

        float bulletSpeed = Arg("speed", 3);

        Bullet.Create(
            enemy,
            enemy.GlobalPosition,
            toward.Normalized() * bulletSpeed,
            null
        );
    }
}

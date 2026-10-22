using Godot;
using System;

[GlobalClass]
public partial class EnemyBehavior : Behavior
{
    protected Game game => GameManager.Instance.game;
    protected Player player => game.player;
    public void Test(Enemy enemy)
    {
        GD.Print($"触发敌人行为: Enemy={enemy.Name}");
    }

    # region 通用方法

    public void Attack(Enemy enemy)
    {
        enemy.Attack();
    }

    public void FaceToPlayer(Enemy enemy)
    {
        if(player == null || !IsInstanceValid(player) || player.IsQueuedForDeletion())return;

        float diff = player.GlobalPosition.X - enemy.GlobalPosition.X;
        if(diff > 0)
            diff = 1;
        else if(diff < 0)
            diff = -1;
        enemy.ScaleLayout(new Vector2(diff, 1));
    }

    /// <summary>
    /// 朝向移动房间（仅左右）
    /// </summary>
    /// <param name="enemy"></param>
    public void FaceToMove(Enemy enemy)
    {
        float diff = enemy.GetVelocity().X;
        if(diff < 0)
            diff = -1;
        else
            diff = 1;
        enemy.ScaleLayout(new Vector2(diff, 1));
    }

    /// <summary>
    /// 朝向玩家移动
    /// </summary>
    /// <param name="enemy"></param>
    public void UpdateCloseToPalyer(Enemy enemy)
    {
        //朝着玩家移动
        enemy.MoveTarget(player.GlobalPosition);
    }

    /// <summary>
    /// 朝向玩家发射子弹
    /// </summary>
    /// <param name="enemy"></param>
    public void AttackShotPlayer(Enemy enemy)
    {
        Vector2 toward = player.GlobalPosition - enemy.GlobalPosition;

        float bulletSpeed = Arg("speed", 3);
        BulletData bulletData = Arg("bullet_data", Bullet.DefaultData);

        enemy.ChangeBullet(bulletData);

        enemy.Shot(toward.Normalized() * bulletSpeed);
    }

    # endregion

}

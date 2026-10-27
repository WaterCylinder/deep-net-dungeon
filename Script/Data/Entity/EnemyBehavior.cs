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

        enemy.Shot(toward.Normalized() * bulletSpeed);
    }
    
    /// <summary>
    /// 切换弹幕
    /// </summary>
    /// <param name="enemy"></param>
    public void ChangeShotBullet(Enemy enemy)
    {
        BulletData data = enemy.args.Get<BulletData>("bullet_data");
        GD.Print($"{enemy.Name}设置弹幕{data.bulletName}");
        enemy.ChangeBullet(data);
    }

    /// <summary>
    /// 朝向玩家发射四方向弹幕：
    /// 以"敌人 → 玩家"的方向为基准，再分别旋转 90°、180°、270°，共四发呈十字形散开。
    /// 参数：
    /// - speed：弹速，默认 3（和 AttackShotPlayer 一致）
    /// - start_angle：整体起始偏移角（度），默认 0；填 45 就变成"×"形
    /// - bullet_data：可选，指定本次使用的弹幕数据；不填则用敌人当前弹幕
    /// </summary>
    /// <param name="enemy"></param>
    public void AttackShotPlayerFourDirection(Enemy enemy)
    {
        if(!Game.IsAble(enemy) || !Game.IsAble(player))return;

        Vector2 toward = player.GlobalPosition - enemy.GlobalPosition;
        //玩家和敌人完全重叠时Normalized()会得到零向量，四发弹幕会全部卡在原地，这里退化成朝右
        if(toward.LengthSquared() < 0.0001f)
            toward = Vector2.Right;

        float bulletSpeed = Arg("speed", 3);
        float startAngle = Arg("start_angle", 0f);
        BulletData data = Arg<BulletData>("bullet_data", null);

        for(int i = 0; i < 4; i++)
        {
            float angle = Mathf.DegToRad(startAngle + 90f * i);
            enemy.Shot(toward.Normalized().Rotated(angle) * bulletSpeed, default, data);
        }
    }

    # endregion

}

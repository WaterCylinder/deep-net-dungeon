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
        //朝着玩家移动
        enemy.MoveTarget(player.GlobalPosition);
    }

    /// <summary>
    /// 和玩家保持一定距离
    /// </summary>
    /// <param name="enemy"></param>
    public void KeepDistanceToPlayer(Enemy enemy)
    {
        float distance = Arg<float>("distance", 256);

        Vector2 diff = player.GlobalPosition - enemy.GlobalPosition;
        if(diff.Length() > distance + 16)
        {
            //朝着玩家移动
            enemy.MoveTarget(player.GlobalPosition);
        }
        else if(diff.Length() < distance - 16)
        {
            enemy.MoveToward(diff * -1);
        }
        else
        {
            enemy.MoveStop();
        }
    }

    /// <summary>
    /// 朝向玩家发射普通子弹
    /// </summary>
    /// <param name="enemy"></param>
    public void AttackShotPlayer(Enemy enemy)
    {
        Vector2 toward = player.GlobalPosition - enemy.GlobalPosition;

        float bulletSpeed = Arg("speed", 3);
        BulletData bulletData = Arg("bullet_data", default).As<BulletData>();

        Bullet.Create(
            enemy,
            enemy.GlobalPosition,
            toward.Normalized() * bulletSpeed,
            bulletData
        );
    }

    /// <summary>
    /// 朝向玩家发射三向子弹
    /// </summary>
    /// <param name="enemy"></param>
    public void AttackShotPlayerThree(Enemy enemy)
    {
        Vector2 toward = player.GlobalPosition - enemy.GlobalPosition;

        float bulletSpeed = Arg("speed", 3);
        float angle = Arg("angle", 20);
        BulletData bulletData = Arg("bullet_data", default).As<BulletData>();

        Vector2 toward1 = toward.Rotated(Mathf.DegToRad(angle));
        Vector2 toward2 = toward.Rotated(Mathf.DegToRad(-angle));

        Bullet.Create(
            enemy,
            enemy.GlobalPosition,
            toward.Normalized() * bulletSpeed,
            bulletData
        );
        Bullet.Create(
            enemy,
            enemy.GlobalPosition,
            toward1.Normalized() * bulletSpeed,
            bulletData
        );
        Bullet.Create(
            enemy,
            enemy.GlobalPosition,
            toward2.Normalized() * bulletSpeed,
            bulletData
        );
    }

    /// <summary>
    /// 反弹移动
    /// </summary>
    public void UpdateReboundMove(Enemy enemy)
    {
        Vector2 toward = enemy.GetVelocity().Normalized();
        if(enemy.GetToward() == Vector2.Zero)
        {
            toward = new Vector2(game.randomSystem.Float(-1, 1), game.randomSystem.Float(-1, 1));
            if(toward == Vector2.Zero)
            {
                toward = Vector2.Up;
            }
        }
        enemy.args.Add("add_rebound_event", false);
        if (! enemy.args.Get<bool>("add_rebound_event"))
        {
            toward = new Vector2(game.randomSystem.Float(-1, 1), game.randomSystem.Float(-1, 1));
            enemy.MoveToward(toward);
            //给碰撞事件添加反射事件
            enemy.OnContact += (ps, ns) =>
            {
                Vector2 toward = enemy.GetToward();
                if(ns.Count > 0)
                {
                    Vector2 normal = ns[0].Normalized();
                    //法线夹角大于90读，点积小于0
                    if(normal.Dot(toward) < 0)
                    {
                        toward = toward.Bounce(normal);
                        enemy.MoveToward(toward);
                    }
                }
            };
            enemy.args.Set("add_rebound_event", true);
        }
        enemy.Move();
    }

    /// <summary>
    /// 跟随并蓄力冲击
    /// </summary>
    public void UpdateFollowAndChargedImpact(Enemy enemy)
    {
        enemy.args.Add("follow_and_charged_impact_state", "follow");

        if(enemy.args.Get<string>("follow_and_charged_impact_state") == "follow")
        {
            enemy.MoveTarget(player.GlobalPosition);
            enemy.AddTempTimer(
                "follow_and_charged_impact_timer1",
                5,
                () => enemy.args.Set("follow_and_charged_impact_state", "charge")
            );
        }
        else if(enemy.args.Get<string>("follow_and_charged_impact_state") == "charge")
        {
            enemy.MoveStop();
            enemy.AddTempTimer(
                "follow_and_charged_impact_timer2",
                1,
                () => 
                {
                    enemy.args.Set("follow_and_charged_impact_state", "impact");
                    enemy.MoveToward(player.GlobalPosition - enemy.GlobalPosition);
                    enemy.speed.Multi(5, "impact_speed_up", 5);
                }
            );
        }
        else if(enemy.args.Get<string>("follow_and_charged_impact_state") == "impact")
        {
            enemy.Move();
            enemy.AddTempTimer(
                "follow_and_charged_impact_timer3",
                1,
                () =>
                {
                    enemy.args.Set("follow_and_charged_impact_state", "follow");
                    enemy.speed.RemoveMultiplier("impact_speed_up");
                }
            );
        }
    }
}

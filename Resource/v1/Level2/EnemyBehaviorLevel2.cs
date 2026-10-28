using System.Threading.Tasks;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class EnemyBehaviorLevel2 : EnemyBehavior
{
    public void SpiderDeath(Enemy enemy)
    {
        //发射八方向弹幕
        Vector2 toward = Vector2.Right;
        Array<Vector2> towards = new();
        float angle = 360 / 8;
        for(int i = 1; i<=8; i++)
        {
            toward = toward.Rotated(Mathf.DegToRad(angle));
            enemy.Shot(toward.Normalized() * 4);
        }
    }

    public async Task UpdateRiftWanderAsync(Enemy enemy)
    {
        enemy.args.Add("rift_wander_state", "wander");
        if(enemy.args.Get<string>("rift_wander_state") == "wander")
        {
            //随机移动
            Vector2 toward = enemy.GetToward();
            if(toward == Vector2.Zero)
            {
                toward = new Vector2(game.randomSystem.Float(-1, 1), game.randomSystem.Float(-1, 1));
                if(toward == Vector2.Zero)
                {
                    toward = Vector2.Up;
                }
                enemy.MoveToward(toward);
            }
            enemy.AddTempTimer(
                "rift_wander_timer1",
                game.randomSystem.Float(1, 2),
                () => {
                    enemy.args.Set("rift_wander_state", "shot");
                }
            );
        }
        else if(enemy.args.Get<string>("rift_wander_state") == "shot")
        {
            enemy.animNode.Play("attack");
            enemy.MoveStop();
            enemy.args.Set("rift_wander_state", "shot_anim");
            await ToSignal(enemy.animNode, AnimatedSprite2D.SignalName.AnimationFinished);
            if (enemy.args.Get<string>("rift_wander_state") != "shot_anim") return;
            enemy.Attack();
            enemy.args.Set("rift_wander_state", "wander");
            enemy.animNode.Play("idle");
        }
    }

    //裂罅监视者行为
    public async void UpdateRiftWatcher(Enemy enemy)
    {
        enemy.args.Add("rift_watcher_state", "spawn");
        BulletData bigFire = enemy.args.Get<BulletData>("big_fire");
        if(enemy.args.Get<string>("rift_watcher_state") == "spawn")
        {
            enemy.animNode.Play("spawn");
            enemy.args.Set("rift_watcher_state", "shot_anim");
            enemy.MoveStop();
            await ToSignal(enemy.animNode, AnimatedSprite2D.SignalName.AnimationFinished);
            enemy.args.Set("rift_watcher_state", "wander");
        }
        // 随机移动
        else if(enemy.args.Get<string>("rift_watcher_state") == "wander")
        {
            //随机移动
            enemy.animNode.Play("idle");
            Vector2 toward = enemy.GetToward();
            enemy.speed.Multi(1, "SpeedUp", 99);
            if(toward == Vector2.Zero)
            {
                toward = new Vector2(game.randomSystem.Float(-1, 1), game.randomSystem.Float(-1, 1));
                if(toward == Vector2.Zero)
                {
                    toward = Vector2.Up;
                }
                enemy.MoveToward(toward);
            }
            enemy.AddTempTimer(
                "rift_wander_timer1",
                game.randomSystem.Float(1, 2),
                () => {
                    float r = game.randomSystem.Float();
                    if(r < 0.3)
                    {
                        enemy.args.Set("rift_watcher_state", "shot1");
                    }
                    else if(r < 0.7)
                    {
                        enemy.args.Set("rift_watcher_state", "shot2");
                    }
                    else
                    {
                        enemy.args.Set("rift_watcher_state", "shot3");
                    }
                }
            );
        }
        //攻击方式1：发射快慢奇偶弹
        else if (enemy.args.Get<string>("rift_watcher_state") == "shot1")
        {
            enemy.animNode.Play("attack1");
            enemy.args.Set("rift_watcher_state", "shot_anim");
            //等待动画结束
            await ToSignal(enemy.animNode, AnimatedSprite2D.SignalName.AnimationFinished);
            enemy.MoveStop();
            game.CreateParticles("boss_skill", enemy.GlobalPosition);
            //发射六向快速弹幕
            Vector2 toward = Vector2.Right;
            float angle = 360 / 8;
            for(int i = 1; i<=8; i++)
            {
                toward = toward.Rotated(Mathf.DegToRad(angle));
                enemy.Shot(toward.Normalized() * 2);
            }
            enemy.AddTempTimer(
                "rift_watcher_timer_shot",
                0.1f,
                () =>
                {
                    //发射慢速5向弹幕
                    Vector2 toward = Vector2.Right;
                    float angle = 360 / 15;
                    for(int i = 1; i<=15; i++)
                    {
                        toward = toward.Rotated(Mathf.DegToRad(angle));
                        enemy.Shot(toward.Normalized() * 4);
                    }
                }
            );
            enemy.args.Set("rift_watcher_state", "follow");
            enemy.animNode.Play("idle");
        }
        //冲刺,
        else if (enemy.args.Get<string>("rift_watcher_state") == "follow")
        {
            Vector2 toward = player.GlobalPosition - enemy.GlobalPosition;
            enemy.speed.Multi(5, "SpeedUp", 99);
            enemy.MoveToward(toward);
            enemy.AddTempTimer(
                "rift_wander_timer2",
                game.randomSystem.Float(1, 2),
                () => {
                    float r = game.randomSystem.Float();
                    if(r < 0.3)
                    {
                        enemy.args.Set("rift_watcher_state", "shot1");
                    }
                    else if(r < 0.7)
                    {
                        enemy.args.Set("rift_watcher_state", "shot2");
                    }
                    else
                    {
                        enemy.args.Set("rift_watcher_state", "shot3");
                    }
                }
            );
        }
        //攻击方式2：发射三连发大火球
        else if (enemy.args.Get<string>("rift_watcher_state") == "shot2")
        {
            enemy.animNode.Play("attack2");
            enemy.args.Set("rift_watcher_state", "shot_anim");
            //等待动画结束
            await ToSignal(enemy.animNode, AnimatedSprite2D.SignalName.AnimationFinished);
            enemy.MoveStop();
            game.CreateParticles("boss_skill", enemy.GlobalPosition);
            //发射三发大火球
            Vector2 toward = player.GlobalPosition - enemy.GlobalPosition;
            toward = toward.Normalized() * 2;
            enemy.Shot(toward, _data:bigFire);
            enemy.Shot(toward.Rotated(Mathf.DegToRad(20)), _data:bigFire);
            enemy.Shot(toward.Rotated(Mathf.DegToRad(-20)), _data:bigFire);

            //50%概率额外召唤两只裂罅烈焰（rift_fire）
            if(game.randomSystem.Float() < 0.5f)
            {
                EnemyData riftFireData = enemy.args.Get<EnemyData>("rift_fire");
                if(riftFireData != null)
                {
                    for(int i = 0; i < 2; i++)
                    {
                        Enemy riftFire = game.SpawnEnemy(riftFireData, enemy.GlobalPosition + new Vector2(i == 0 ? -160f : 160f, 0), enemy.GetParent());
                        if(riftFire == null)
                        {
                            continue;
                        }

                        riftFire.room = enemy.room;
                        riftFire.OnGenerate();
                        //召唤物不掉资源点
                        riftFire.dropValue = Vector2I.Zero;
                    }
                }
            }

            enemy.args.Set("rift_watcher_state", "wander");
            enemy.animNode.Play("idle");
        }
        //攻击方式3：发射自机狙大火球和十二向弹
        else if (enemy.args.Get<string>("rift_watcher_state") == "shot3")
        {
            enemy.animNode.Play("attack2");
            enemy.args.Set("rift_watcher_state", "shot_anim");
            //等待动画结束
            await ToSignal(enemy.animNode, AnimatedSprite2D.SignalName.AnimationFinished);
            //发射六向快速弹幕
            game.CreateParticles("boss_skill", enemy.GlobalPosition);
            Vector2 toward = player.GlobalPosition - enemy.GlobalPosition;
            toward = toward.Normalized() * 2;
            enemy.Shot(toward, _data:bigFire);
            toward = Vector2.Right;
            float angle = 360 / 12;
            for(int i = 1; i<=12; i++)
            {
                toward = toward.Rotated(Mathf.DegToRad(angle));
                enemy.Shot(toward.Normalized() * 6);
            }
            float r = game.randomSystem.Float();
            if(r < 0.5f)
                enemy.args.Set("rift_watcher_state", "wander");
            else
                enemy.args.Set("rift_watcher_state", "follow");
            enemy.animNode.Play("idle");
        }
    }
}

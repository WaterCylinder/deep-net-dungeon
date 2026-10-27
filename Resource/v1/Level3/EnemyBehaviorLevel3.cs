using Godot;
using System;

[GlobalClass]
public partial class EnemyBehaviorLevel3 : EnemyBehavior
{
    /// <summary>
    /// 电浆蜘蛛：静止 1~2 秒 → 随机移动 1~2 秒 → 静止，循环。
    /// 状态存在 enemy.args["elect_spider_state"]。
    /// 只在计时器回调里翻转状态，每帧只负责"按当前状态表现"——
    /// 这样 AddTempTimer 的同名计时器不会被每帧重置（同名时它直接返回 false）。
    /// </summary>
    public void UpdateElectSpider(Enemy enemy)
    {
        enemy.args.Add("elect_spider_state", "idle");
        string state = enemy.args.Get<string>("elect_spider_state");

        if (state == "move")
        {
            enemy.animNode?.Play("move");

            //静止阶段每帧都在MoveStop，所以刚切到移动时速度是0：这时候抽一次方向走完这个阶段。
            //之后GetToward()有值就不再重抽——如果每帧都抽，200速下每秒重定向60次，观感会变成原地抖动
            Vector2 toward = enemy.GetToward();
            if (toward == Vector2.Zero)
            {
                toward = new(game.randomSystem.Float(-1, 1), game.randomSystem.Float(-1, 1));
                if (toward.LengthSquared() < 0.0001f)
                {
                    toward = Vector2.Up;
                }
                enemy.MoveToward(toward.Normalized());
            }

            enemy.AddTempTimer(
                "elect_spider_phase",
                game.randomSystem.Float(1, 2),
                () =>
                {
                    enemy.MoveStop();
                    enemy.args.Set("elect_spider_state", "idle");
                }
            );
        }
        else
        {
            enemy.MoveStop();
            enemy.animNode?.Play("idle");

            enemy.AddTempTimer(
                "elect_spider_phase",
                game.randomSystem.Float(1, 2),
                () =>
                {
                    enemy.args.Set("elect_spider_state", "move");
                }
            );
        }
    }

    /// <summary>
    /// 电浆蜘蛛死亡：朝玩家发射 1 发电浆弹幕。
    /// 注意这里不能判断 Game.IsAble(enemy)——死亡回调里 enemy.isDead 已经是 true，
    /// 判断了会直接返回、一发都打不出来。
    /// 参数 elect_bullet：电浆弹幕数据；shot_speed：弹速，默认 6。
    /// </summary>
    public void ElectSpiderDeath(Enemy enemy)
    {
        if (enemy == null || !GodotObject.IsInstanceValid(enemy)) return;
        if (!Game.IsAble(player)) return;

        BulletData plasma = enemy.args.Get<BulletData>("elect_bullet");
        if (plasma == null)
        {
            GD.PushWarning("[EnemyBehaviorLevel3.ElectSpiderDeath] 未配置电浆弹幕（elect_bullet），死亡时不会发射");
            return;
        }

        Vector2 toward = player.GlobalPosition - enemy.GlobalPosition;
        if (toward.LengthSquared() < 0.0001f)
        {
            toward = Vector2.Right;
        }

        float shotSpeed = enemy.args.Get<float>("shot_speed", 6f);
        enemy.Shot(toward.Normalized() * shotSpeed, _data: plasma);
    }

    /// <summary>
    /// emoji：远程阶段和玩家保持距离，每帧调 Attack 由攻速决定开火间隔（三连发写在 EmojiAttack 里）；
    /// 护盾被打空后进入近战阶段：播 move、朝玩家冲、护甲挂一个 ×2 乘区，并且不再开火。
    /// 参数 keep_distance：远程阶段想保持的距离，默认 260。
    /// </summary>
    public void UpdateEmoji(Enemy enemy)
    {
        if (!Game.IsAble(enemy) || !Game.IsAble(player)) return;

        enemy.args.Add("emoji_rage", false);

        //护盾空了就切近战。只在第一次满足条件时加乘区，靠 emoji_rage 标记保证不会反复叠加
        if (enemy.shield <= 0 && !enemy.args.Get<bool>("emoji_rage"))
        {
            enemy.args.Set("emoji_rage", true);
            enemy.defense.Multi(2, "EmojiArmor", 99);
            enemy.speed.Multi(2, "EmojiArmor", 99);
        }

        if (enemy.args.Get<bool>("emoji_rage"))
        {
            enemy.animNode?.Play("move");
            enemy.MoveToward(player.GlobalPosition - enemy.GlobalPosition);
            return;
        }

        //远程阶段：比 keep_distance 近就退、远出 20% 就跟上来，在区间内则停下
        float keepDistance = enemy.args.Get<float>("keep_distance", 260f);
        Vector2 towardPlayer = player.GlobalPosition - enemy.GlobalPosition;
        float distance = towardPlayer.Length();
        if (distance < keepDistance)
        {
            enemy.MoveToward(-towardPlayer);
        }
        else if (distance > keepDistance * 1.2f)
        {
            enemy.MoveToward(towardPlayer);
        }
        else
        {
            enemy.MoveStop();
        }

        enemy.animNode?.Play("idle");
        //攻击间隔由 attackSpeed 控制：Entity.Attack 里 attackTimer 满了才会走 OnAttack
        enemy.Attack();
    }

    /// <summary>
    /// emoji 的攻击：朝玩家三连发"默认弹幕"（不传弹幕数据，用敌人自己的 bulletData）。
    /// 第一发立刻打，后两发用 AddTempTimer 错开，和 rift_watcher 的连发写法一致。
    /// 参数 burst_gap：连发间隔，默认 0.12 秒。
    /// </summary>
    public void EmojiAttack(Enemy enemy)
    {
        if (!Game.IsAble(enemy) || !Game.IsAble(player)) return;

        Vector2 toward = player.GlobalPosition - enemy.GlobalPosition;
        if (toward.LengthSquared() < 0.0001f)
        {
            toward = Vector2.Right;
        }
        //三发用同一个方向：连发是一串平行弹，不会每一发都重新瞄准玩家
        toward = toward.Normalized() * 3f;

        float gap = enemy.args.Get<float>("burst_gap", 0.12f);

        enemy.Shot(toward);
        enemy.AddTempTimer("emoji_burst_2", gap, () =>
        {
            if (!Game.IsAble(enemy)) return;
            enemy.Shot(toward);
            enemy.AddTempTimer("emoji_burst_3", gap, () =>
            {
                if (Game.IsAble(enemy))
                {
                    enemy.Shot(toward);
                }
            });
        });
    }

    /// <summary>
    /// 巨蛛 BOSS，循环：
    /// 朝玩家移动 2 秒（进入移动时只调一次 MoveToward 定方向，方向存在实体自己的 moveTowards 里，中途不变）
    /// → 三波电浆弹幕（快速八方向 → 中速五方向 → 慢速十二方向）
    /// → 停住 2 秒
    /// → 朝玩家连发三轮"并排三发"电浆弹幕（三发方向相同、发射位置左右错开）
    /// → 回到移动。
    /// 移动阶段播 move，其余阶段播 idle。参数：elect_bullet 电浆弹幕、move_time / wait_time 阶段时长、
    /// ring_fast_speed / ring_medium_speed / ring_slow_speed 三波弹速、
    /// burst_gap 连发间隔、burst_speed 连发弹速、burst_spread 并排间距（像素）。
    /// </summary>
    public void UpdateBigSpider(Enemy enemy)
    {
        if (!Game.IsAble(enemy) || !Game.IsAble(player)) return;

        enemy.args.Add("big_spider_state", "move");
        string state = enemy.args.Get<string>("big_spider_state");
        BulletData plasma = enemy.args.Get<BulletData>("elect_bullet");
        //移动阶段播 move，其余阶段（环形弹幕 / 停顿 / 并排连发）都是站着的，播 idle
        enemy.animNode?.Play(state == "move" ? "move" : "idle");

        if (state == "move")
        {
            //AddTempTimer 第一次返回 true，说明这一帧刚进入移动阶段：这时调一次 MoveToward 定下方向，
            //方向由实体自己记在 moveTowards 里，之后每帧只调 Move() 沿用这个方向
            if (enemy.AddTempTimer(
                "big_spider_move",
                enemy.args.Get<float>("move_time", 2f),
                () => enemy.args.Set("big_spider_state", "rings")))
            {
                Vector2 toward = player.GlobalPosition - enemy.GlobalPosition;
                if (toward.LengthSquared() < 0.0001f) toward = Vector2.Right;
                enemy.MoveToward(toward);
            }
            else
            {
                enemy.Move();
            }
        }
        else if (state == "rings")
        {
            enemy.MoveStop();
            enemy.args.Set("big_spider_state", "wait");
            //静止时间缩短到 0.5~1 秒（可用 wait_time_min / wait_time_max 调）
            enemy.AddTempTimer(
                "big_spider_wait",
                game.randomSystem.Float(
                    enemy.args.Get<float>("wait_time_min", 0.5f),
                    enemy.args.Get<float>("wait_time_max", 1f)),
                () => enemy.args.Set("big_spider_state", "burst"));

            if (plasma == null)
            {
                GD.PushWarning("[EnemyBehaviorLevel3.UpdateBigSpider] 没有配置电浆弹幕（elect_bullet），跳过环形弹幕");
            }
            else
            {
                float fastSpeed = enemy.args.Get<float>("ring_fast_speed", 5f);
                float mediumSpeed = enemy.args.Get<float>("ring_medium_speed", 3f);
                float slowSpeed = enemy.args.Get<float>("ring_slow_speed", 1.5f);

                //第一波：快速八方向
                Vector2 toward = Vector2.Right;
                for (int i = 0; i < 8; i++)
                {
                    enemy.Shot(toward.Rotated(Mathf.DegToRad(45f * i)) * fastSpeed, default, plasma);
                }

                //第二波：中速五方向
                enemy.AddTempTimer("big_spider_ring2", 0.15f, () =>
                {
                    if (!Game.IsAble(enemy)) return;
                    Vector2 dir = Vector2.Right;
                    for (int i = 0; i < 5; i++)
                    {
                        enemy.Shot(dir.Rotated(Mathf.DegToRad(72f * i)) * mediumSpeed, default, plasma);
                    }
                });

                //第三波：慢速十二方向
                enemy.AddTempTimer("big_spider_ring3", 0.3f, () =>
                {
                    if (!Game.IsAble(enemy)) return;
                    Vector2 dir = Vector2.Right;
                    for (int i = 0; i < 12; i++)
                    {
                        enemy.Shot(dir.Rotated(Mathf.DegToRad(30f * i)) * slowSpeed, default, plasma);
                    }
                });
            }
        }
        else if (state == "wait")
        {
            enemy.MoveStop();
        }
        else if (state == "burst")
        {
            enemy.MoveStop();
            //立刻切到 burst_hold，避免每帧重排一轮连发；三发打完由最后一轮切回 move
            enemy.args.Set("big_spider_state", "burst_hold");

            if (plasma == null)
            {
                GD.PushWarning("[EnemyBehaviorLevel3.UpdateBigSpider] 没有配置电浆弹幕（elect_bullet），跳过并排连发");
                enemy.args.Set("big_spider_state", "move");
            }
            else
            {
                float gap = enemy.args.Get<float>("burst_gap", 0.15f);
                float shotSpeed = enemy.args.Get<float>("burst_speed", 4f);
                float spread = enemy.args.Get<float>("burst_spread", 70f);

                for (int i = 0; i < 3; i++)
                {
                    int volley = i;
                    enemy.AddTempTimer($"big_spider_burst_{volley}", gap * (volley + 1), () =>
                    {
                        if (!Game.IsAble(enemy)) return;

                        Vector2 toward = player.GlobalPosition - enemy.GlobalPosition;
                        if (toward.LengthSquared() < 0.0001f) toward = Vector2.Right;
                        toward = toward.Normalized();

                        //并排：三发方向完全一样，只是发射位置沿垂直方向左右错开
                        Vector2 side = toward.Rotated(Mathf.Pi * 0.5f);
                        Vector2 velocity = toward * shotSpeed;
                        enemy.Shot(velocity, enemy.GlobalPosition, plasma);
                        enemy.Shot(velocity, enemy.GlobalPosition + side * spread, plasma);
                        enemy.Shot(velocity, enemy.GlobalPosition - side * spread, plasma);

                        if (volley == 2)
                        {
                            enemy.args.Set("big_spider_state", "move");
                        }
                    });
                }
            }
        }
        else // burst_hold：等连发的三个计时器打完，这期间原地不动
        {
            enemy.MoveStop();
        }
    }

    /// <summary>
    /// 每隔 summon_interval 秒召唤 summon_count 只电浆蜘蛛（每帧跑一次的行为，配合 BindBehavior 和 UpdateBigSpider 一起挂在 onUpdate 上）。
    /// 参数：
    /// - elect_spider_data：要召唤的蜘蛛数据（EnemyData）
    /// - summon_interval：召唤间隔，默认 5 秒
    /// - summon_count：每次召唤的只数，默认 3 只
    /// - summon_max：场上同时存活的上限，默认 30 只
    /// 已召唤的蜘蛛存在 args["summoned_spiders"] 里，每次召唤前先清掉已经死亡/被回收的，再判断数量。
    /// </summary>
    public void SummonElectSpider(Enemy enemy)
    {
        if (!Game.IsAble(enemy)) return;

        enemy.args.Add("summon_ready", false);
        enemy.args.Add("summoned_spiders", new Godot.Collections.Array());

        //同名计时器还在时 AddTempTimer 会直接返回 false：所以每帧调也只会在空闲时重新开始计时
        enemy.AddTempTimer(
            "big_spider_summon",
            enemy.args.Get<float>("summon_interval", 5f),
            () => enemy.args.Set("summon_ready", true));

        if (!enemy.args.Get<bool>("summon_ready")) return;
        enemy.args.Set("summon_ready", false);

        EnemyData spiderData = enemy.args.Get<EnemyData>("elect_spider_data");
        if (spiderData == null)
        {
            GD.PushWarning("[EnemyBehaviorLevel3.SummonElectSpider] 没有配置电浆蜘蛛数据（elect_spider_data），本次不召唤");
            return;
        }

        //先剔除已经死掉/被回收的蜘蛛，再判断是否达到上限
        Godot.Collections.Array spawned = enemy.args.Get<Godot.Collections.Array>("summoned_spiders");
        for (int i = spawned.Count - 1; i >= 0; i--)
        {
            if (!Game.IsAble(spawned[i].As<Enemy>()))
            {
                spawned.RemoveAt(i);
            }
        }

        int summonMax = enemy.args.Get<int>("summon_max", 30);
        if (spawned.Count >= summonMax) return;

        //每次召唤 summon_count 只（默认3只），接近上限时少召
        int summonCount = enemy.args.Get<int>("summon_count", 3);
        for (int i = 0; i < summonCount && spawned.Count < summonMax; i++)
        {
            //在boss周围随机方向、固定距离处召唤（父节点和boss相同，房间销毁时会一起回收）
            Vector2 offset = new(game.randomSystem.Float(-1, 1), game.randomSystem.Float(-1, 1));
            if (offset.LengthSquared() < 0.0001f) offset = Vector2.Up;

            Enemy spider = game.SpawnEnemy(spiderData, enemy.GlobalPosition + offset.Normalized() * 160f, enemy.GetParent());
            if (spider == null) continue;

            spider.OnGenerate();
            //召唤物攻击力对齐boss的攻击力
            spider.attack.Add(enemy.attack.Get() - spider.attack.Get(), "Summoner", 60);
            //召唤物不掉资源点：蜘蛛数据是共用的，这里只改这一只实例，不然boss战会变成刷钱机
            spider.dropValue = Vector2I.Zero;
            spawned.Add(spider);
        }
    }
}

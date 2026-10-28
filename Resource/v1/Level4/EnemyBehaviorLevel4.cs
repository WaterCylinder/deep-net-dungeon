using Godot;
using System;

[GlobalClass]
public partial class EnemyBehaviorLevel4 : EnemyBehavior
{
    /// <summary>
    /// panic：一直朝玩家移动；护盾低于"最大生命的一半"时，每满 1 秒恢复 10% 最大生命的护盾。
    /// 每次被击中都会把这一秒的等待时间清零，所以连续挨打就回不上盾。
    /// </summary>
    public void UpdatePanic(Enemy enemy)
    {
        if (!Game.IsAble(enemy) || !Game.IsAble(player)) return;

        //第一次走到这里时挂一次受击回调：只要挨打就把回盾的等待时间清零
        if (!enemy.args.Check("panic_inited"))
        {
            enemy.args.Set("panic_inited", true);
            enemy.args.Set("panic_regen_wait", 0f);
            enemy.OnHurtSignal += (_, _) => enemy.args.Set("panic_regen_wait", 0f);
        }

        //跟随玩家（走导航，绕开墙）
        enemy.animNode?.Play("idle");
        UpdateCloseToPalyer(enemy);

        //回盾：累计满1秒结算一次，只有护盾低于最大生命一半时才恢复
        float wait = enemy.args.Get<float>("panic_regen_wait") + (float)enemy.GetProcessDeltaTime();
        if (wait < 1f)
        {
            enemy.args.Set("panic_regen_wait", wait);
            return;
        }

        enemy.args.Set("panic_regen_wait", 0f);
        if (enemy.shield >= enemy.maxHealth.Get() * 0.5f) return;

        float regen = enemy.maxHealth.Get() * 0.1f;
        enemy.shield = Mathf.Min(enemy.maxHealth.Get(), enemy.shield + regen);
    }

    // ======================= WSAWA（见吾所知） =======================

    /// <summary>
    /// WSAWA 主实体。
    /// 一阶段：生成双手 → 最大生命改为"自身+双手"之和 → 上下各放一个穿透弹幕body
    ///         → 停在房间正上方(0,-384)，等双手全部死亡。
    /// 二阶段：回到房间中央 → 移除"不可被选中"标签 → 两个body挪到正下方近处
    ///         → 三种弹幕模式循环（模式一是奇偶弹，一发一换），每种打完停顿2~3秒。
    /// 两个body没有行为，位置由这里每帧按偏移维持。
    /// 参数：left_hand_data / right_hand_data / body1 / body2 /
    ///       plasma_bullet / fire_bullet / big_fire_bullet
    /// </summary>
    public void UpdateWSAWA(Enemy enemy)
    {
        if (!Game.IsAble(enemy) || !Game.IsAble(player)) return;

        //两个body弹幕始终维持在boss的相对位置
        Bullet body1 = enemy.args.Get<Bullet>("wsawa_body1");
        Bullet body2 = enemy.args.Get<Bullet>("wsawa_body2");
        Vector2 body1Offset = enemy.args.Get("wsawa_body1_offset", new Vector2(0, -150));
        Vector2 body2Offset = enemy.args.Get("wsawa_body2_offset", new Vector2(0, 150));
        if (Game.IsAble(body1)) body1.GlobalPosition = enemy.GlobalPosition + body1Offset;
        if (Game.IsAble(body2)) body2.GlobalPosition = enemy.GlobalPosition + body2Offset;

        enemy.args.Add("wsawa_state", "init");
        string state = enemy.args.Get<string>("wsawa_state");

        float delta = (float)enemy.GetProcessDeltaTime();
        enemy.args.Set("wsawa_timer", enemy.args.Get<float>("wsawa_timer") + delta);

        // ---------------- 初始化 ----------------
        if (state == "init")
        {
            //有房间就用房间原点当中心；没有房间（例如调试时用GenerateImmediately直接生成）就用玩家当前位置
            Vector2 center = enemy.room != null ? enemy.room.GlobalPosition : player.GlobalPosition;
            enemy.args.Set("wsawa_center", center);

            EnemyData[] handDatas =
            {
                enemy.args.Get<EnemyData>("left_hand_data"),
                enemy.args.Get<EnemyData>("right_hand_data")
            };
            Vector2[] handOffsets = { new(640, 0), new(-640, 0) };

            Godot.Collections.Array hands = new();
            float handHealth = 0f;
            for (int i = 0; i < handDatas.Length; i++)
            {
                if (handDatas[i] == null) continue;

                handHealth += handDatas[i].maxHealth;

                Enemy hand = game.SpawnEnemy(handDatas[i], center + handOffsets[i], enemy.GetParent());
                if (hand == null) continue;

                hand.room = enemy.room;
                hand.OnGenerate();
                hand.args.Set("wsawa_center", center);
                hand.dropValue = Vector2I.Zero;
                hands.Add(hand);
            }
            enemy.args.Set("wsawa_hands", hands);

            //最大生命值 = 自身 + 两只手
            float totalHealth = enemy.maxHealth.Get() + handHealth;
            enemy.maxHealth.AddBase(totalHealth - enemy.maxHealth.Get());

            //一阶段血条要显示"本体 + 双手"的当前值，本体自身这部分先存下来（一阶段本体不掉血）
            enemy.args.Set("wsawa_self_health", enemy.currentHealth);
            enemy.args.Set("wsawa_self_shield", enemy.shield);

            enemy.Heal(totalHealth);

            //上下两个穿透弹幕body，父节点挂在房间里（位置由上面的每帧跟随维持）
            BulletData body1Data = enemy.args.Get<BulletData>("body1");
            BulletData body2Data = enemy.args.Get<BulletData>("body2");

            enemy.args.Set("wsawa_body1_offset", new Vector2(0, -150));
            enemy.args.Set("wsawa_body2_offset", new Vector2(0, 150));

            if (body1Data != null)
            {
                enemy.args.Set("wsawa_body1", Bullet.Create(enemy, enemy.GlobalPosition + new Vector2(0, -150), Vector2.Zero, body1Data, enemy.GetParent()));
            }
            if (body2Data != null)
            {
                enemy.args.Set("wsawa_body2", Bullet.Create(enemy, enemy.GlobalPosition + new Vector2(0, 150), Vector2.Zero, body2Data, enemy.GetParent()));
            }

            enemy.args.Set("wsawa_state", "wait_hands");
            enemy.args.Set("wsawa_timer", 0f);
            return;
        }

        // ---------------- 一阶段：停在正上方，等待双手全死 ----------------
        if (state == "wait_hands")
        {
            //一阶段血条显示"本体 + 双手"的当前值：本体自身不掉血，双手掉了多少血条就掉多少
            float displayHealth = enemy.args.Get<float>("wsawa_self_health");
            float displayShield = enemy.args.Get<float>("wsawa_self_shield");

            bool allDead = true;
            Godot.Collections.Array hands = enemy.args.Get<Godot.Collections.Array>("wsawa_hands");
            for (int i = 0; i < hands.Count; i++)
            {
                Enemy hand = hands[i].As<Enemy>();
                if (!Game.IsAble(hand))
                {
                    continue;
                }

                allDead = false;
                displayHealth += hand.currentHealth;
                displayShield += hand.shield;
            }

            enemy.currentHealth = displayHealth;
            enemy.shield = displayShield;

            Vector2 anchor = enemy.args.Get("wsawa_center", enemy.GlobalPosition) + new Vector2(0, -384);
            if ((enemy.GlobalPosition - anchor).Length() > 8f)
            {
                enemy.MoveTarget(anchor);
            }
            else
            {
                enemy.MoveStop();
            }

            if (allDead)
            {
                enemy.args.Set("wsawa_state", "enter_center");
                enemy.args.Set("wsawa_timer", 0f);
            }
            return;
        }

        // ---------------- 二阶段入场 ----------------
        if (state == "enter_center")
        {
            Vector2 center = enemy.args.Get("wsawa_center", enemy.GlobalPosition);
            if ((enemy.GlobalPosition - center).Length() > 8f)
            {
                enemy.MoveTarget(center);
                return;
            }
            enemy.MoveStop();
            enemy.RemoveTag(EnemyTag.TargetDisable);

            //两个body挪到boss正下方比较近的位置（改偏移，上面的跟随会自动生效）
            enemy.args.Set("wsawa_body1_offset", new Vector2(0, 120));
            enemy.args.Set("wsawa_body2_offset", new Vector2(0, 190));

            enemy.args.Set("wsawa_pattern", 0);
            enemy.args.Set("wsawa_state", "pause");
            enemy.args.Set("wsawa_timer", 0f);
            enemy.args.Set("wsawa_shot_timer", 0f);
            enemy.args.Set("wsawa_ring_timer", 0f);
            enemy.args.Set("wsawa_spin", 0f);
            enemy.args.Set("wsawa_shot_index", 0);
            enemy.args.Set("wsawa_pause", game.randomSystem.Float(2, 3));
            return;
        }

        // ---------------- 模式之间的停顿 ----------------
        if (state == "pause")
        {
            enemy.MoveStop();
            if (enemy.args.Get<float>("wsawa_timer") < enemy.args.Get<float>("wsawa_pause")) return;

            enemy.args.Set("wsawa_state", "pattern");
            enemy.args.Set("wsawa_timer", 0f);
            enemy.args.Set("wsawa_shot_timer", 0f);
            enemy.args.Set("wsawa_ring_timer", 0f);
            enemy.args.Set("wsawa_spin", 0f);
            enemy.args.Set("wsawa_shot_index", 0);
            return;
        }

        // ---------------- 三种弹幕模式 ----------------
        enemy.MoveStop();

        int pattern = enemy.args.Get<int>("wsawa_pattern");
        float duration = pattern == 1 ? 10f : 8f;

        //发射间隔
        float interval = pattern switch
        {
            0 => enemy.args.Get("odd_even_interval", 0.3f),
            1 => enemy.args.Get("virus_interval", 0.1f),
            _ => enemy.args.Get("big_fire_interval", 0.4f),
        };

        float shotTimer = enemy.args.Get<float>("wsawa_shot_timer") + delta;
        if (shotTimer >= interval)
        {
            shotTimer -= interval;

            Vector2 aim = player.GlobalPosition - enemy.GlobalPosition;
            if (aim.LengthSquared() < 0.0001f) aim = Vector2.Right;
            aim = aim.Normalized();

            if (pattern == 0)
            {
                //奇偶弹：奇数发3发电浆（正对、±20度），偶数发4发裂罅火（±10、±30度），一发一换
                int shotIndex = enemy.args.Get<int>("wsawa_shot_index");
                enemy.args.Set("wsawa_shot_index", shotIndex + 1);

                if (shotIndex % 2 == 0)
                {
                    BulletData plasma = enemy.args.Get<BulletData>("plasma_bullet");
                    if (plasma != null)
                    {
                        float shotSpeed = 4f;
                        enemy.Shot(aim * shotSpeed, default, plasma);
                        enemy.Shot(aim.Rotated(Mathf.DegToRad(-20f)) * shotSpeed, default, plasma);
                        enemy.Shot(aim.Rotated(Mathf.DegToRad(20f)) * shotSpeed, default, plasma);
                    }
                }
                else
                {
                    BulletData fire = enemy.args.Get<BulletData>("fire_bullet");
                    if (fire != null)
                    {
                        float shotSpeed = 4f;
                        enemy.Shot(aim.Rotated(Mathf.DegToRad(-30f)) * shotSpeed, default, fire);
                        enemy.Shot(aim.Rotated(Mathf.DegToRad(-10f)) * shotSpeed, default, fire);
                        enemy.Shot(aim.Rotated(Mathf.DegToRad(10f)) * shotSpeed, default, fire);
                        enemy.Shot(aim.Rotated(Mathf.DegToRad(30f)) * shotSpeed, default, fire);
                    }
                }
            }
            else if (pattern == 1)
            {
                //每0.2秒一发快速病毒弹
                BulletData virus = enemy.args.Get<BulletData>("virus_bullet");
                enemy.Shot(aim * 6f, default, virus);

                //每2秒18向病毒弹
                float ring = enemy.args.Get<float>("wsawa_ring_timer") + interval;
                if (ring >= 2f)
                {
                    ring = 0f;
                    for (int i = 0; i < 18; i++)
                    {
                        enemy.Shot(Vector2.Right.Rotated(Mathf.DegToRad(20f * i)) * 3f, default, virus);
                    }
                }
                enemy.args.Set("wsawa_ring_timer", ring);
            }
            else
            {
                //两方向大火球（正反相对），每发一次起始方向转10度
                BulletData bigFire = enemy.args.Get<BulletData>("big_fire_bullet");
                if (bigFire != null)
                {
                    float spin = enemy.args.Get<float>("wsawa_spin");
                    for (int i = 0; i < 2; i++)
                    {
                        enemy.Shot(Vector2.Right.Rotated(Mathf.DegToRad(spin + 180f * i)) * 2f, default, bigFire);
                    }
                    enemy.args.Set("wsawa_spin", spin + 10f);
                }
            }
        }
        enemy.args.Set("wsawa_shot_timer", shotTimer);

        if (enemy.args.Get<float>("wsawa_timer") >= duration)
        {
            enemy.args.Set("wsawa_state", "pause");
            enemy.args.Set("wsawa_pattern", (pattern + 1) % 3);
            enemy.args.Set("wsawa_timer", 0f);
            enemy.args.Set("wsawa_shot_timer", 0f);
            enemy.args.Set("wsawa_ring_timer", 0f);
            enemy.args.Set("wsawa_spin", 0f);
            enemy.args.Set("wsawa_pause", game.randomSystem.Float(2, 3));
        }
    }

    /// <summary>
    /// WSAWA 死亡：把两个body弹幕一起清掉（它们是无限时间、穿透一切的，不能留在地上）
    /// </summary>
    public void WSAWADeath(Enemy enemy)
    {
        if (enemy == null || !GodotObject.IsInstanceValid(enemy)) return;

        Bullet body1 = enemy.args.Get<Bullet>("wsawa_body1");
        Bullet body2 = enemy.args.Get<Bullet>("wsawa_body2");
        if (Game.IsAble(body1)) body1.QueueFree();
        if (Game.IsAble(body2)) body2.QueueFree();
    }

    /// <summary>
    /// WSAWA 左手：停在房间中心向右640处，停滞1~2秒后随机两种攻击。
    /// A：朝玩家移动并每0.4秒打4向病毒弹，3~5秒后回原位；
    /// B：先上移、再下移到等距位置、最后回原位，移动途中每0.5秒朝玩家打一发大火球。
    /// </summary>
    public void UpdateWSAWALeftHand(Enemy enemy)
    {
        if (!Game.IsAble(enemy) || !Game.IsAble(player)) return;

        //中心点优先用boss传过来的（wsawa_center），没有就用房间，再没有就用玩家位置
        Vector2 center = enemy.args.Get("wsawa_center", enemy.room != null ? enemy.room.GlobalPosition : player.GlobalPosition);
        Vector2 home = center + new Vector2(640, 0);

        if (!enemy.args.Check("wsawa_hand_state"))
        {
            enemy.args.Set("wsawa_hand_state", "idle");
            enemy.args.Set("wsawa_timer", 0f);
            enemy.args.Set("wsawa_wait", game.randomSystem.Float(1, 2));
        }

        float delta = (float)enemy.GetProcessDeltaTime();
        enemy.args.Set("wsawa_timer", enemy.args.Get<float>("wsawa_timer") + delta);
        float timer = enemy.args.Get<float>("wsawa_timer");
        string state = enemy.args.Get<string>("wsawa_hand_state");

        if (state == "idle")
        {
            if ((enemy.GlobalPosition - home).Length() > 16f)
            {
                enemy.MoveTarget(home);
                return;
            }
            enemy.MoveStop();

            if (timer < enemy.args.Get<float>("wsawa_wait")) return;

            enemy.args.Set("wsawa_timer", 0f);
            enemy.args.Set("wsawa_shot_timer", 0f);
            if (game.randomSystem.Float() < 0.5f)
            {
                enemy.args.Set("wsawa_hand_state", "chase");
                enemy.args.Set("wsawa_limit", game.randomSystem.Float(3, 5));
            }
            else
            {
                enemy.args.Set("wsawa_hand_state", "rise");
                enemy.args.Set("wsawa_limit", 300f);
            }
            return;
        }

        if (state == "chase")
        {
            //追击走导航
            UpdateCloseToPalyer(enemy);

            float shot = enemy.args.Get<float>("wsawa_shot_timer") + delta;
            if (shot >= 0.4f)
            {
                shot = 0f;
                Vector2 aim = player.GlobalPosition - enemy.GlobalPosition;
                if (aim.LengthSquared() < 0.0001f) aim = Vector2.Right;
                aim = aim.Normalized();

                //4向病毒弹
                BulletData virus = enemy.args.Get<BulletData>("virus_bullet");
                for (int i = 0; i < 4; i++)
                {
                    enemy.Shot(aim.Rotated(Mathf.DegToRad(90f * i)) * 3f, default, virus);
                }
            }
            enemy.args.Set("wsawa_shot_timer", shot);

            if (timer >= enemy.args.Get<float>("wsawa_limit"))
            {
                enemy.args.Set("wsawa_hand_state", "back");
                enemy.args.Set("wsawa_timer", 0f);
            }
            return;
        }

        if (state == "rise" || state == "fall")
        {
            float distance = enemy.args.Get<float>("wsawa_limit");
            Vector2 target = state == "rise" ? home + new Vector2(0, -distance) : home + new Vector2(0, distance);

            if ((enemy.GlobalPosition - target).Length() > 16f)
            {
                enemy.MoveTarget(target);
            }
            else
            {
                enemy.args.Set("wsawa_hand_state", state == "rise" ? "fall" : "back");
                enemy.args.Set("wsawa_timer", 0f);
            }

            //移动途中每0.5秒朝玩家打一发大火球
            float shot = enemy.args.Get<float>("wsawa_shot_timer") + delta;
            if (shot >= 0.5f)
            {
                shot = 0f;
                BulletData bigFire = enemy.args.Get<BulletData>("big_fire_bullet");
                if (bigFire != null)
                {
                    Vector2 aim = player.GlobalPosition - enemy.GlobalPosition;
                    if (aim.LengthSquared() < 0.0001f) aim = Vector2.Right;
                    enemy.Shot(aim.Normalized() * 2f, default, bigFire);
                }
            }
            enemy.args.Set("wsawa_shot_timer", shot);
            return;
        }

        // back：回原位，到位后重新待机
        if ((enemy.GlobalPosition - home).Length() > 16f)
        {
            enemy.MoveTarget(home);
            return;
        }
        enemy.MoveStop();
        enemy.args.Set("wsawa_hand_state", "idle");
        enemy.args.Set("wsawa_timer", 0f);
        enemy.args.Set("wsawa_wait", game.randomSystem.Float(1, 2));
    }

    /// <summary>
    /// WSAWA 右手：停在房间中心向左640处，停滞1~3秒后随机两种攻击。
    /// A：减速并与玩家保持距离，每0.5秒打3发电浆弹，持续5秒；
    /// B：向右移动到偏移点，之后每1秒召唤一只电浆蜘蛛，持续4~6秒，然后回原位。
    /// </summary>
    public void UpdateWSAWARightHand(Enemy enemy)
    {
        if (!Game.IsAble(enemy) || !Game.IsAble(player)) return;

        //中心点优先用boss传过来的（wsawa_center），没有就用房间，再没有就用玩家位置
        Vector2 center = enemy.args.Get("wsawa_center", enemy.room != null ? enemy.room.GlobalPosition : player.GlobalPosition);
        Vector2 home = center + new Vector2(-640, 0);

        if (!enemy.args.Check("wsawa_hand_state"))
        {
            enemy.args.Set("wsawa_hand_state", "idle");
            enemy.args.Set("wsawa_timer", 0f);
            enemy.args.Set("wsawa_wait", game.randomSystem.Float(1, 3));
        }

        float delta = (float)enemy.GetProcessDeltaTime();
        enemy.args.Set("wsawa_timer", enemy.args.Get<float>("wsawa_timer") + delta);
        float timer = enemy.args.Get<float>("wsawa_timer");
        string state = enemy.args.Get<string>("wsawa_hand_state");

        if (state == "idle")
        {
            if ((enemy.GlobalPosition - home).Length() > 16f)
            {
                enemy.MoveTarget(home);
                return;
            }
            enemy.MoveStop();

            if (timer < enemy.args.Get<float>("wsawa_wait")) return;

            enemy.args.Set("wsawa_timer", 0f);
            enemy.args.Set("wsawa_shot_timer", 0f);
            if (game.randomSystem.Float() < 0.5f)
            {
                enemy.args.Set("wsawa_hand_state", "keep");
                enemy.speed.Multi(0.5f, "wsawa_slow", 50);
            }
            else
            {
                enemy.args.Set("wsawa_hand_state", "move_right");
                enemy.args.Set("wsawa_limit", game.randomSystem.Float(4, 6));
            }
            return;
        }

        if (state == "keep")
        {
            //减速状态下和玩家保持距离
            Vector2 toPlayer = player.GlobalPosition - enemy.GlobalPosition;
            float distance = toPlayer.Length();
            if (distance < 420f) enemy.MoveToward(-toPlayer);
            else if (distance > 560f) UpdateCloseToPalyer(enemy);   //离太远：走导航追回来
            else enemy.MoveStop();

            float shot = enemy.args.Get<float>("wsawa_shot_timer") + delta;
            if (shot >= 0.5f)
            {
                shot = 0f;
                BulletData plasma = enemy.args.Get<BulletData>("plasma_bullet");
                if (plasma != null)
                {
                    Vector2 aim = toPlayer;
                    if (aim.LengthSquared() < 0.0001f) aim = Vector2.Right;
                    aim = aim.Normalized();
                    enemy.Shot(aim * 3f, default, plasma);
                    enemy.Shot(aim.Rotated(Mathf.DegToRad(-20f)) * 3f, default, plasma);
                    enemy.Shot(aim.Rotated(Mathf.DegToRad(20f)) * 3f, default, plasma);
                }
            }
            enemy.args.Set("wsawa_shot_timer", shot);

            if (timer >= 5f)
            {
                enemy.speed.RemoveMultiplier("wsawa_slow");
                enemy.args.Set("wsawa_hand_state", "back");
                enemy.args.Set("wsawa_timer", 0f);
            }
            return;
        }

        if (state == "move_right")
        {
            Vector2 target = home + new Vector2(300, 0);
            if ((enemy.GlobalPosition - target).Length() > 16f)
            {
                enemy.MoveTarget(target);
                return;
            }
            enemy.MoveStop();
            enemy.args.Set("wsawa_hand_state", "summon");
            enemy.args.Set("wsawa_timer", 0f);
            enemy.args.Set("wsawa_shot_timer", 0f);
            return;
        }

        if (state == "summon")
        {
            enemy.MoveStop();

            float shot = enemy.args.Get<float>("wsawa_shot_timer") + delta;
            if (shot >= 1f)
            {
                shot = 0f;
                EnemyData spiderData = enemy.args.Get<EnemyData>("elect_spider_data");
                if (spiderData != null)
                {
                    Enemy spider = game.SpawnEnemy(spiderData, enemy.GlobalPosition, enemy.GetParent());
                    if (spider != null)
                    {
                        spider.room = enemy.room;
                        spider.OnGenerate();

                        //召唤物攻击力在自身基础上增加右手自己的攻击力
                        spider.attack.AddModify(enemy.attack.Get(), "Summoner");

                        spider.dropValue = Vector2I.Zero;
                    }
                }
            }
            enemy.args.Set("wsawa_shot_timer", shot);

            if (timer >= enemy.args.Get<float>("wsawa_limit"))
            {
                enemy.args.Set("wsawa_hand_state", "back");
                enemy.args.Set("wsawa_timer", 0f);
            }
            return;
        }

        // back：回原位
        if ((enemy.GlobalPosition - home).Length() > 16f)
        {
            enemy.MoveTarget(home);
            return;
        }
        enemy.MoveStop();
        enemy.speed.RemoveMultiplier("wsawa_slow");
        enemy.args.Set("wsawa_hand_state", "idle");
        enemy.args.Set("wsawa_timer", 0f);
        enemy.args.Set("wsawa_wait", game.randomSystem.Float(1, 3));
    }
}

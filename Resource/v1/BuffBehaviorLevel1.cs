using Godot;
using System;

[GlobalClass]
public partial class BuffBehaviorLevel1 : BuffBehavior
{
    public void HealOneWhenGetScore(Entity entity, Buff buff)
    {
        entity.OnPickEnergy += _HealOneWhenGetScore;
    }

    public void RemoveHealOneWhenGetScore(Entity entity, Buff buff)
    {
        entity.OnPickEnergy -= _HealOneWhenGetScore;
    }

    void _HealOneWhenGetScore(Entity entity, EnergyPoint energyPoint)
    {
        if(Game.IsAble(energyPoint) && energyPoint.energyType == EnergyType.Coin)
        {
            entity.Heal(energyPoint.value);
        }
    }

    //紧急避险道具效果，护盾消失时获得护甲
    public void EmergencyManual(Entity entity, Buff buff)
    {
        entity.OnShieldBrokenSignal += _GetDefenseOnShieldBroken;
    }
    public void RemoveEmergencyManual(Entity entity, Buff buff)
    {
        entity.OnShieldBrokenSignal -= _GetDefenseOnShieldBroken;
    }
    void _GetDefenseOnShieldBroken(Entity entity)
    {
        BuffData data = AssetManager.Instance.GetData<BuffData>("Resource/v1/Item/Buff/Data/buff_emergency_defense", null);
        entity.AttachBuff(data);
    }

    //紧急避险道具效果，获得100点护甲
    public void EmergencyManualGetDefense(Entity entity, Buff buff)
    {
        entity.defense.Add(100, "EmergencyManual", 5);
    }
    public void EmergencyManualRemoveDefense(Entity entity, Buff buff)
    {
        entity.defense.RemoveMultiplier("EmergencyManual");
    }

    //病毒免疫效果,
    public void AntivirusSoftware(Entity entity, Buff buff)
    {
        entity.OnAttachBuffSignal += _DeleteVirusBuff;
    }
    public void RemoveAntivirusSoftware(Entity entity, Buff buff)
    {
        entity.OnAttachBuffSignal -= _DeleteVirusBuff;
    }
    //移除病毒效果
    void _DeleteVirusBuff(Entity entity, Buff buff)
    {
        if(buff.id == "virus")
            entity.RemoveBuff(buff);
    }

    //迭代器效果,受到伤害时获得等量护盾
    public void Iterator(Entity entity, Buff buff)
    {
        entity.OnHurtSignal += GetShiledWhenHurt;
    }
    public void RemoveIterator(Entity entity, Buff buff)
    {
        entity.OnHurtSignal -= GetShiledWhenHurt;
    }
    void GetShiledWhenHurt(Entity entity, float damage)
    {
        entity.AddShield(damage);
    }

    //裂罅护甲：按"本房间内带灼烧效果的敌怪数量"提供护甲（每只+10），由Buff的onTimer每0.1秒刷新一次。
    //比原来的检测范围(Trigger)省很多：不再有额外的Area2D，只是遍历房间敌人的子节点
    public void RiftArmorOnTimer(Entity entity, Buff buff)
    {
        if(!Game.IsAble(entity))return;

        int count = 0;
        Room room = entity.room;
        if(room != null && room.enemyNode != null)
        {
            foreach(Node node in room.enemyNode.GetChildren())
            {
                if(node is Enemy enemy && Game.IsAble(enemy) && enemy.HasBuff("fire"))
                {
                    count += 1;
                }
            }
        }

        //房间为空时这里会写入0，顺便把上一个房间残留的护甲清掉
        entity.defense.Add(10 * count, "RiftArmor", 10);
    }

    public void RemoveRiftArmor(Entity entity, Buff buff)
    {
        entity.defense.RemoveMultiplier("RiftArmor");
    }

    //裂罅图腾：每秒让房间内所有敌怪获得 fire_stack 层灼烧（默认4层），由Buff的onTimer每1秒触发一次
    public void RiftTotemOnTimer(Entity entity, Buff buff)
    {
        if(!Game.IsAble(entity))return;

        Room room = entity.room;
        if(room == null || room.enemyNode == null)return;

        int stack = buff.args.Get<int>("fire_stack", 4);

        foreach(Node node in room.enemyNode.GetChildren())
        {
            if(node is Enemy enemy && Game.IsAble(enemy))
            {
                enemy.AttachBuff(Buff.Fire, stack);
            }
        }
    }

    //护盾推进器：有护盾时速度+100，没护盾时归零。
    //Value.Add(value, id, sortCode) 是"给指定乘区设值"而不是累加，所以每0.1秒按当前状态写一次就够，
    //不需要记之前加过多少（要递增才用 AddModify）
    public void ShieldThrusterOnTimer(Entity entity, Buff buff)
    {
        if(!Game.IsAble(entity))return;

        float bonus = buff.args.Get<float>("speed_bonus", 100f);
        entity.speed.Add(entity.shield > 0 ? bonus : 0f, "ShieldThruster", 50);
    }

    public void RemoveShieldThruster(Entity entity, Buff buff)
    {
        entity.speed.RemoveMultiplier("ShieldThruster");
    }

    //病毒模块：拾取时自身获得若干层病毒，同时攻击力、暴击率、暴击伤害各加一段固定值
    //三个数值用同一个乘区id（分别挂在不同的Value上，互不影响），移除时一起摘掉
    public void VirusModuleOnStart(Entity entity, Buff buff)
    {
        if(!Game.IsAble(entity))return;

        entity.AttachBuff(Buff.Virus, buff.args.Get<int>("virus_stack", 5));
        entity.attack.Add(buff.args.Get<float>("attack_bonus", 50f), "VirusModule", 50);
        entity.critRate.Add(buff.args.Get<float>("crit_rate_bonus", 0.2f), "VirusModule", 50);
        entity.critDamage.Add(buff.args.Get<float>("crit_damage_bonus", 0.5f), "VirusModule", 50);
    }

    public void RemoveVirusModule(Entity entity, Buff buff)
    {
        entity.attack.RemoveMultiplier("VirusModule");
        entity.critRate.RemoveMultiplier("VirusModule");
        entity.critDamage.RemoveMultiplier("VirusModule");

        //把自己施加的那几层病毒也扣掉；层数归零时Buff系统会自动移除病毒
        Buff virus = entity.GetBuff("virus");
        if(virus != null)
        {
            entity.ChangeBuffStack(virus, -buff.args.Get<int>("virus_stack", 5));
        }
    }

    #region 燧石

    /// <summary>
    /// 燧石弹幕效果：弹幕击中时给目标附加流血，并随机方向迸射2~3个燧石碎片弹幕
    /// 挂在弹幕实体上，由 buff_flint_bullet 的 onStart 触发
    /// </summary>
    public void FlintBullet(Entity entity, Buff buff)
    {
        if (entity is not Bullet bullet) return;

        //同一弹幕重复挂载时先解绑，避免重复触发
        RemoveFlintBullet(entity, buff);

        FlintBulletHandler handler = new(bullet, buff);
        _flintHandlers[bullet.GetInstanceId()] = handler;
        bullet.OnHit += handler.OnHit;
    }

    /// <summary>
    /// 移除燧石弹幕效果，由 buff_flint_bullet 的 onEnd 触发
    /// 注意：弹幕死亡时 isDead 已为 true，condition 用 true_condition，
    /// 这里只靠 IsInstanceValid 判断，否则 onEnd 会被条件短路而漏掉解绑
    /// </summary>
    public void RemoveFlintBullet(Entity entity, Buff buff)
    {
        if (entity is not Bullet bullet || !GodotObject.IsInstanceValid(bullet)) return;

        ulong id = bullet.GetInstanceId();
        if (_flintHandlers.TryGetValue(id, out FlintBulletHandler handler))
        {
            bullet.OnHit -= handler.OnHit;
            _flintHandlers.Remove(id);
        }
    }

    /// <summary>
    /// 燧石效果：使实体发射的弹幕挂载燧石弹幕效果
    /// 由 buff_flint 的 onStart 触发
    /// </summary>
    public void Flint(Entity entity, Buff buff)
    {
        if (!GodotObject.IsInstanceValid(entity)) return;

        entity.OnShot -= OnFlintShot;
        entity.OnShot += OnFlintShot;
    }

    /// <summary>
    /// 移除燧石效果，由 buff_flint 的 onEnd 触发
    /// </summary>
    public void RemoveFlint(Entity entity, Buff buff)
    {
        if (!GodotObject.IsInstanceValid(entity)) return;

        entity.OnShot -= OnFlintShot;
    }

    /// <summary>
    /// 实体发射弹幕时给弹幕挂上燧石弹幕效果
    /// </summary>
    void OnFlintShot(Entity entity, Bullet bullet)
    {
        if (!Game.IsAble(bullet)) return;
        bullet.AttachBuff(Buff.FlintBullet);
    }

    /// <summary>
    /// 单个弹幕的燧石命中处理者：按弹幕实例保存，保证 OnHit 能正确解绑
    /// </summary>
    sealed class FlintBulletHandler
    {
        readonly Bullet _bullet;
        readonly Buff _buff;

        public FlintBulletHandler(Bullet bullet, Buff buff)
        {
            _bullet = bullet;
            _buff = buff;
        }

        public void OnHit(Entity target)
        {
            if (!Game.IsAble(_bullet)) return;

            //击中时附加流血
            int stack = _buff.args.Get("blood_stack", 1);
            if (Game.IsAble(target) && stack > 0)
            {
                target.AttachBuff(Buff.Blood, stack);
            }

            //随机方向迸射燧石碎片弹幕
            BulletData shardData = _buff.args.Get<BulletData>("shard_bullet");
            if (shardData == null) return;

            int min = _buff.args.Get("shard_min", 2);
            int max = _buff.args.Get("shard_max", 3);
            float speed = _buff.args.Get("shard_speed", 3f);

            RandomSystem random = GameManager.Instance.game.randomSystem;
            int count = random.Int(Mathf.Min(min, max), Mathf.Max(min, max));

            for (int i = 0; i < count; i++)
            {
                Vector2 toward = Vector2.Right.Rotated(random.Float(0, Mathf.Tau));
                Bullet.Create(_bullet.owner, _bullet.GlobalPosition, toward * speed, shardData);
            }
        }
    }

    static readonly System.Collections.Generic.Dictionary<ulong, FlintBulletHandler> _flintHandlers = new();

    #endregion

    #region 跳板机

    /// <summary>
    /// 跳板机：实体发射弹幕时给弹幕挂上"跳板机弹幕"buff。
    /// 由 buff_springboard 的 onStart 触发
    /// </summary>
    public void Springboard(Entity entity, Buff buff)
    {
        if (!GodotObject.IsInstanceValid(entity)) return;

        entity.OnShot -= OnSpringboardShot;
        entity.OnShot += OnSpringboardShot;
    }

    /// <summary>
    /// 移除跳板机，由 buff_springboard 的 onEnd 触发
    /// </summary>
    public void RemoveSpringboard(Entity entity, Buff buff)
    {
        if (!GodotObject.IsInstanceValid(entity)) return;

        entity.OnShot -= OnSpringboardShot;
    }

    /// <summary>
    /// 实体发射弹幕时，给这颗弹幕挂上跳板机弹幕效果
    /// </summary>
    void OnSpringboardShot(Entity entity, Bullet bullet)
    {
        if (!Game.IsAble(bullet)) return;
        bullet.AttachBuff(Buff.SpringboardBullet);
    }

    /// <summary>
    /// 跳板机弹幕：挂在弹幕上，命中敌怪时按目标的病毒层数给发射者加护盾。
    /// 由 buff_springboard_bullet 的 onStart 触发
    /// </summary>
    public void SpringboardBullet(Entity entity, Buff buff)
    {
        if (entity is not Bullet bullet) return;

        //同一弹幕重复挂载时先解绑，避免重复触发
        RemoveSpringboardBullet(entity, buff);

        SpringboardBulletHandler handler = new(bullet);
        _springboardHandlers[bullet.GetInstanceId()] = handler;
        bullet.OnHit += handler.OnHit;
    }

    /// <summary>
    /// 移除跳板机弹幕效果，由 buff_springboard_bullet 的 onEnd 触发
    /// </summary>
    public void RemoveSpringboardBullet(Entity entity, Buff buff)
    {
        if (entity is not Bullet bullet || !GodotObject.IsInstanceValid(bullet)) return;

        ulong id = bullet.GetInstanceId();
        if (_springboardHandlers.TryGetValue(id, out SpringboardBulletHandler handler))
        {
            bullet.OnHit -= handler.OnHit;
            _springboardHandlers.Remove(id);
        }
    }

    /// <summary>
    /// 单颗弹幕的跳板机命中处理者：按弹幕实例保存，保证 OnHit 能正确解绑
    /// </summary>
    sealed class SpringboardBulletHandler
    {
        readonly Bullet _bullet;

        public SpringboardBulletHandler(Bullet bullet)
        {
            _bullet = bullet;
        }

        public void OnHit(Entity target)
        {
            if (!Game.IsAble(_bullet) || !Game.IsAble(target)) return;
            if (target is not Enemy) return;

            Entity owner = _bullet.owner;
            if (!Game.IsAble(owner)) return;

            //冷却存在发射者的跳板机buff上，这样他打出的所有弹幕共用同一个冷却
            Buff springboard = owner.GetBuff("buff_springboard");
            if (springboard == null) return;

            long now = (long)Time.GetTicksMsec();
            float cooldown = springboard.args.Get<float>("cooldown", 1f);
            long last = springboard.args.Get<long>("last_hit_ms", 0);
            if (now - last < (long)(cooldown * 1000f)) return;

            //目标没有病毒层数就不触发，也不占用冷却
            Buff virus = target.GetBuff("virus");
            if (virus == null || virus.currentStack <= 0) return;

            springboard.args.Set("last_hit_ms", now);
            owner.AddShield(virus.currentStack);
        }
    }

    static readonly System.Collections.Generic.Dictionary<ulong, SpringboardBulletHandler> _springboardHandlers = new();

    #endregion
}

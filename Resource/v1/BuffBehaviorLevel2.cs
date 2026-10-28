using Godot;
using System;

/// <summary>
/// edition2 道具的 Buff 行为。
/// 约定：OnStart 里挂效果/加乘区，OnEnd 里对称地摘掉（乘区用 RemoveMultiplier，事件先 -= 再 += 防重复订阅）
/// </summary>
[GlobalClass]
public partial class BuffBehaviorLevel2 : BuffBehavior
{
    #region 击穿 breakdown

    /// <summary>
    /// 击穿：攻击力乘 80%，并让发射的弹幕都带上"穿透敌怪"标签
    /// </summary>
    public void BreakdownOnStart(Entity entity, Buff buff)
    {
        if (!Game.IsAble(entity)) return;

        entity.attack.Multi(buff.args.Get<float>("attack_multi", 0.8f), "Breakdown", 60);

        //同层buff重复挂载时先解绑，避免重复订阅
        entity.OnShot -= OnBreakdownShot;
        entity.OnShot += OnBreakdownShot;
    }

    public void RemoveBreakdown(Entity entity, Buff buff)
    {
        if (!GodotObject.IsInstanceValid(entity)) return;

        entity.attack.RemoveMultiplier("Breakdown");
        entity.OnShot -= OnBreakdownShot;
    }

    /// <summary>
    /// 给发射出的弹幕追加穿透敌怪标签
    /// </summary>
    void OnBreakdownShot(Entity entity, Bullet bullet)
    {
        if (!Game.IsAble(bullet)) return;

        if (!bullet.bulletTags.Contains(BulletTag.PenetrateEnemy))
        {
            bullet.bulletTags.Add(BulletTag.PenetrateEnemy);
        }
    }

    #endregion

    #region 交叉火力 crossFire

    /// <summary>
    /// 交叉火力：发射弹幕时额外发射 value 发（value = 当前层数），
    /// 角度按 -value 度和 +value 度交替
    /// </summary>
    public void CrossFireOnStart(Entity entity, Buff buff)
    {
        if (!Game.IsAble(entity)) return;

        RemoveCrossFire(entity, buff);

        CrossFireHandler handler = new(entity, buff);
        _crossFireHandlers[entity.GetInstanceId()] = handler;
        entity.OnShot += handler.OnShot;
    }

    public void RemoveCrossFire(Entity entity, Buff buff)
    {
        if (!GodotObject.IsInstanceValid(entity)) return;

        ulong id = entity.GetInstanceId();
        if (_crossFireHandlers.TryGetValue(id, out CrossFireHandler handler))
        {
            entity.OnShot -= handler.OnShot;
            _crossFireHandlers.Remove(id);
        }
    }

    sealed class CrossFireHandler
    {
        readonly Entity _owner;
        readonly Buff _buff;

        public CrossFireHandler(Entity owner, Buff buff)
        {
            _owner = owner;
            _buff = buff;
        }

        public void OnShot(Entity entity, Bullet bullet)
        {
            if (!Game.IsAble(_owner) || !Game.IsAble(bullet)) return;

            int value = _buff.currentStack * 2;
            if (value <= 0) return;

            Vector2 velocity = bullet.GetVelocity() / Bullet.BASE_SHOT_SPEED_FECTOR;
            if (velocity.LengthSquared() < 0.0001f) return;

            RandomSystem random = GameManager.Instance.game.randomSystem;

            for (int i = 0; i < value; i++)
            {
                //在 [-value, +value] 里随机取整数角度，0不要
                int degree = 0;
                for (int t = 0; t < 16 && degree == 0; t++)
                {
                    degree = random.Int(-value, value);
                }
                if (degree == 0)
                {
                    degree = -value;
                }

                //用Create而不是Shot：不会再次触发OnShot，避免无限分裂
                Bullet.Create(_owner, bullet.GlobalPosition, velocity.Rotated(Mathf.DegToRad(degree)), bullet.data);
            }
        }
    }

    static readonly System.Collections.Generic.Dictionary<ulong, CrossFireHandler> _crossFireHandlers = new();

    #endregion

    #region 能量汲取器 EnergyExtractor

    /// <summary>
    /// 能量汲取器：弹幕击杀敌怪时获得等同于buff层数的护盾
    /// </summary>
    public void EnergyExtractorOnStart(Entity entity, Buff buff)
    {
        if (!Game.IsAble(entity)) return;

        entity.OnShot -= OnEnergyExtractorShot;
        entity.OnShot += OnEnergyExtractorShot;
    }

    public void RemoveEnergyExtractor(Entity entity, Buff buff)
    {
        if (!GodotObject.IsInstanceValid(entity)) return;

        entity.OnShot -= OnEnergyExtractorShot;
    }

    void OnEnergyExtractorShot(Entity entity, Bullet bullet)
    {
        if (!Game.IsAble(bullet)) return;

        //弹幕命中时目标若已经死了，说明是这发弹幕打死的
        bullet.OnHit += target =>
        {
            if (!Game.IsAble(entity)) return;
            if (target is not Enemy || Game.IsAble(target)) return;

            Buff self = entity.GetBuff("buff_energy_extractor");
            if (self == null) return;

            entity.AddShield(self.currentStack * self.args.Get<float>("shield_per_stack", 1f));
        };
    }

    #endregion

    #region 黄金硬币 GoldCoin

    /// <summary>
    /// 黄金硬币：每获得一次护盾就加攻击力，触发次数等于层数
    /// </summary>
    public void GoldCoinOnStart(Entity entity, Buff buff)
    {
        buff.args.Set("last_shield", entity.shield);
    }

    public void GoldCoinOnTimer(Entity entity, Buff buff)
    {
        if (!Game.IsAble(entity)) return;

        float lastShield = buff.args.Get<float>("last_shield", entity.shield);
        bool gained = entity.shield > lastShield;
        buff.args.Set("last_shield", entity.shield);

        if (!gained) return;

        entity.attack.AddModify(buff.args.Get<float>("attack_bonus", 5f), "GoldCoinAttack");
        //触发一次消耗一层，层数归零时Buff系统会自己移除它
        entity.ChangeBuffStack(buff, -1);
    }

    #endregion

    #region 机械之心 MechanicalHeart

    /// <summary>
    /// 机械之心：拿生命上限换无上限护盾——最大生命值变成原来的 10%，
    /// 护盾不再受最大生命值限制，并获得1000点护盾
    /// </summary>
    public void MechanicalHeartOnStart(Entity entity, Buff buff)
    {
        if (!Game.IsAble(entity)) return;

        float oldMax = entity.maxHealth.Get();
        entity.maxHealth.Multi(buff.args.Get<float>("maxhealth_multi", 0.1f), "MechanicalHeart", 60);
        float newMax = entity.maxHealth.Get();

        //上限变高就把差值补进血量；上限变低（本道具）则把当前血量压到新上限
        if (newMax > oldMax)
        {
            entity.Heal(newMax - oldMax);
        }
        else
        {
            entity.currentHealth = Mathf.Min(entity.currentHealth, newMax);
        }

        entity.shieldOverLoad = true;
        entity.AddShield(buff.args.Get<float>("shield_value", 1000f));
    }

    public void RemoveMechanicalHeart(Entity entity, Buff buff)
    {
        if (!GodotObject.IsInstanceValid(entity)) return;

        entity.maxHealth.RemoveMultiplier("MechanicalHeart");
        entity.shieldOverLoad = false;
    }

    #endregion

    #region 神秘饮料 MysteriousDrink

    /// <summary>
    /// 神秘饮料：攻击力+30%（回复生命由道具的onPick负责，Buff只管持续30秒的攻击加成）
    /// </summary>
    public void MysteriousDrinkOnStart(Entity entity, Buff buff)
    {
        if (!Game.IsAble(entity)) return;

        entity.attack.Multi(buff.args.Get<float>("attack_multi", 1.3f), "MysteriousDrink", 60);
    }

    public void RemoveMysteriousDrink(Entity entity, Buff buff)
    {
        if (!GodotObject.IsInstanceValid(entity)) return;

        entity.attack.RemoveMultiplier("MysteriousDrink");
    }

    #endregion

    #region 超载模块 OverloadModule

    /// <summary>
    /// 超载模块：攻击力×200%
    /// </summary>
    public void OverloadModuleOnStart(Entity entity, Buff buff)
    {
        if (!Game.IsAble(entity)) return;

        entity.attack.Multi(buff.args.Get<float>("attack_multi", 2f), "OverloadModule", 60);
    }

    public void RemoveOverloadModule(Entity entity, Buff buff)
    {
        if (!GodotObject.IsInstanceValid(entity)) return;

        entity.attack.RemoveMultiplier("OverloadModule");
    }

    #endregion

    #region 加固板 ReinforcingPlate

    /// <summary>
    /// 加固板：获得10点护甲；如果加上之后护甲高于50，再额外获得5点
    /// </summary>
    public void ReinforcingPlateOnStart(Entity entity, Buff buff)
    {
        if (!Game.IsAble(entity)) return;

        entity.defense.AddModify(buff.args.Get<float>("defense_bonus", 10f), "ReinforcingPlate");

        if (entity.defense.Get() > 50f)
        {
            entity.defense.AddModify(buff.args.Get<float>("extra_defense_bonus", 5f), "ReinforcingPlateBonus");
        }
    }

    public void RemoveReinforcingPlate(Entity entity, Buff buff)
    {
        if (!GodotObject.IsInstanceValid(entity)) return;

        entity.defense.RemoveMultiplier("ReinforcingPlate");
        entity.defense.RemoveMultiplier("ReinforcingPlateBonus");
    }

    #endregion
}

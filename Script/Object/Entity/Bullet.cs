using Godot;
using Godot.Collections;

/// <summary>
/// 弹幕标签枚举 - 定义弹幕的各种行为特性和效果
/// </summary>
public enum BulletTag
{
    #region 穿透类

    /// <summary>
    /// 可穿透障碍物 - 弹幕碰到障碍物不会消失，继续飞行
    /// </summary>
    PenetrateObstacle,

    /// <summary>
    /// 可穿透敌人 - 弹幕击中敌人后继续飞行，可命中多个敌人
    /// </summary>
    PenetrateEnemy,

    /// <summary>
    /// 可穿透玩家 - 敌方弹幕击中玩家后继续飞行
    /// </summary>
    PenetratePlayer,

    /// <summary>
    /// 穿透所有实体 - 弹幕不会因碰撞任何实体而消失
    /// </summary>
    PenetrateAll,

    #endregion

    #region 运动行为类

    /// <summary>
    /// 追踪弹幕 - 会自动追踪目标
    /// </summary>
    Homing,

    /// <summary>
    /// 反弹弹幕 - 碰到障碍物会反弹
    /// </summary>
    Bounce,

    /// <summary>
    /// 受重力影响 - 弹幕会受重力作用下坠
    /// </summary>
    Gravity,

    /// <summary>
    /// 螺旋运动 - 弹幕沿螺旋轨迹飞行
    /// </summary>
    Spiral,

    /// <summary>
    /// 波浪运动 - 弹幕沿正弦波轨迹飞行
    /// </summary>
    Wave,

    /// <summary>
    /// 加速运动 - 弹幕会随时间加速
    /// </summary>
    Accelerate,

    /// <summary>
    /// 减速运动 - 弹幕会随时间减速
    /// </summary>
    Decelerate,

    /// <summary>
    /// 随机方向 - 弹幕随机改变方向
    /// </summary>
    RandomDirection,

    #endregion

    #region 伤害效果类

    /// <summary>
    /// 爆炸效果 - 命中时产生范围爆炸伤害
    /// </summary>
    Explosive,

    /// <summary>
    /// 连锁伤害 - 命中后在附近敌人间跳跃
    /// </summary>
    Chain,

    /// <summary>
    /// 灼烧效果 - 命中后附加持续灼烧伤害
    /// </summary>
    Burn,

    /// <summary>
    /// 冰冻效果 - 命中后降低目标移动速度或冻结
    /// </summary>
    Freeze,

    /// <summary>
    /// 中毒效果 - 命中后附加持续中毒伤害
    /// </summary>
    Poison,

    /// <summary>
    /// 眩晕效果 - 命中后使目标短暂眩晕
    /// </summary>
    Stun,

    /// <summary>
    /// 击退效果 - 命中后击退目标
    /// </summary>
    Knockback,

    /// <summary>
    /// 吸血效果 - 命中后回复发射者生命值
    /// </summary>
    Lifesteal,

    #endregion

    #region 归属与判定类

    /// <summary>
    /// 玩家弹幕 - 由玩家发射，可对敌人造成伤害
    /// </summary>
    PlayerOwned,

    /// <summary>
    /// 敌人弹幕 - 由敌人发射，可对玩家造成伤害
    /// </summary>
    EnemyOwned,

    /// <summary>
    /// 无伤害 - 弹幕不造成伤害，仅用于触发效果或视觉表现
    /// </summary>
    NoDamage,

    /// <summary>
    /// 友军免疫 - 不会对友军造成伤害
    /// </summary>
    FriendlyFireImmune,

    #endregion

    #region 特殊效果类

    /// <summary>
    /// 护盾破碎 - 命中时优先破坏护盾
    /// </summary>
    ShieldBreak,

    /// <summary>
    /// 护甲穿透 - 命中时忽略部分或全部护甲
    /// </summary>
    ArmorPiercing,

    /// <summary>
    /// 暴击弹幕 - 有额外暴击几率
    /// </summary>
    Critical,

    /// <summary>
    /// 分裂弹幕 - 命中或消失时分裂成多个小弹幕
    /// </summary>
    Split,

    /// <summary>
    /// 滞留区域 - 命中后在地面创建持续伤害区域
    /// </summary>
    Lingering,

    /// <summary>
    /// 传送弹幕 - 可穿过传送门
    /// </summary>
    Portalable,

    /// <summary>
    /// 定时引爆 - 发射后固定时间爆炸
    /// </summary>
    TimedExplosion,

    /// <summary>
    /// 触发型 - 需要特定条件触发效果（如按键、再次射击等）
    /// </summary>
    Triggered,

    #endregion

    #region 视觉与音效类

    /// <summary>
    /// 闪光效果 - 命中时产生闪光
    /// </summary>
    Flash,

    /// <summary>
    /// 屏幕震动 - 命中时触发屏幕震动
    /// </summary>
    ScreenShake,

    /// <summary>
    /// 拖尾效果 - 弹幕带有拖尾特效
    /// </summary>
    Trail,

    #endregion
}

/// <summary>
/// 弹幕实体类 - 所有弹幕的基础类
/// 继承自Entity，支持弹幕标签系统
/// 不处理物理碰撞，但处理碰撞效果
/// </summary>
public partial class Bullet : Entity
{
    public static PackedScene _bulletPrefab = null;
    public static PackedScene BulletPrefab
    {
        get
        {
            if (_bulletPrefab == null)
            {
                _bulletPrefab = AssetManager.Instance.GetPrefab("Asset.Object.bullet", null);
            }
            return _bulletPrefab;
        }
    }


    #region 碰撞层配置

    /// <summary>
    /// 子弹碰撞层（第3层）
    /// </summary>
    private const int LAYER_BULLET = 3;
    
    /// <summary>
    /// 子弹碰撞掩码
    /// 主动检测：玩家(Layer1)、敌人(Layer2)、障碍物(Layer4)
    /// 因为玩家/敌人的Mask不包含子弹层，所以不会产生物理碰撞
    /// 但子弹能触发body_entered信号处理碰撞效果
    /// </summary>
    private const int MASK_BULLET = (1 << 0) | (1 << 1) | (1 << 3); // Player | Enemy | Obstacle

    #endregion

    #region 弹幕属性

    /// <summary>
    /// 弹幕标签列表
    /// </summary>
    [Export] public Array<BulletTag> bulletTags = new();

    /// <summary>
    /// 弹幕存在时间（秒），0表示永久存在
    /// </summary>
    [Export] public float lifetime = 5f;

    /// <summary>
    /// 弹幕伤害倍率
    /// </summary>
    [Export] public float damageMultiplier = 1f;

    #endregion

    #region 标签管理

    /// <summary>
    /// 检查弹幕是否有指定标签
    /// </summary>
    /// <param name="tag">要检查的标签</param>
    /// <returns>是否拥有该标签</returns>
    public bool CheckTag(BulletTag tag)
    {
        return bulletTags.Contains(tag);
    }

    /// <summary>
    /// 添加弹幕标签
    /// </summary>
    /// <param name="tag">要添加的标签</param>
    public void AddTag(BulletTag tag)
    {
        if (!bulletTags.Contains(tag))
        {
            bulletTags.Add(tag);
        }
    }

    /// <summary>
    /// 移除弹幕标签
    /// </summary>
    /// <param name="tag">要移除的标签</param>
    public void RemoveTag(BulletTag tag)
    {
        if (bulletTags.Contains(tag))
        {
            bulletTags.Remove(tag);
        }
    }

    /// <summary>
    /// 检查弹幕是否可穿透指定类型的实体
    /// </summary>
    /// <param name="target">目标实体</param>
    /// <returns>是否可穿透</returns>
    public bool CanPenetrate(Entity target)
    {
        if (CheckTag(BulletTag.PenetrateAll)) return true;

        if (target.CheckTag(EntityTag.Player) && CheckTag(BulletTag.PenetratePlayer)) return true;
        if (target.CheckTag(EntityTag.Enemy) && CheckTag(BulletTag.PenetrateEnemy)) return true;
        if (target.CheckTag(EntityTag.Obstacle) && CheckTag(BulletTag.PenetrateObstacle)) return true;

        return false;
    }

    /// <summary>
    /// 检查弹幕是否属于玩家
    /// </summary>
    public bool IsPlayerBullet => CheckTag(BulletTag.PlayerOwned);

    /// <summary>
    /// 检查弹幕是否属于敌人
    /// </summary>
    public bool IsEnemyBullet => CheckTag(BulletTag.EnemyOwned);

    /// <summary>
    /// 检查弹幕是否会造成伤害
    /// </summary>
    public bool CanDamage => !CheckTag(BulletTag.NoDamage);

    #endregion

    #region 生命周期管理

    /// <summary>
    /// 弹幕已存在时间
    /// </summary>
    private float _aliveTime = 0f;

    /// <summary>
    /// 更新弹幕生命周期
    /// </summary>
    /// <param name="delta">帧间隔时间</param>
    private void UpdateLifetime(double delta)
    {
        if (lifetime <= 0) return;

        _aliveTime += (float)delta;
        if (_aliveTime >= lifetime)
        {
            OnLifetimeEnd();
        }
    }

    /// <summary>
    /// 生命周期结束时触发
    /// </summary>
    protected virtual void OnLifetimeEnd()
    {
        // 分裂弹幕处理
        if (CheckTag(BulletTag.Split))
        {
            OnSplit();
        }

        // 滞留区域处理
        if (CheckTag(BulletTag.Lingering))
        {
            CreateLingeringZone();
        }

        // 定时引爆处理
        if (CheckTag(BulletTag.TimedExplosion))
        {
            OnTimedExplosion();
        }

        QueueFree();
    }

    #endregion

    #region 碰撞处理

    /// <summary>
    /// 进入碰撞时触发
    /// </summary>
    /// <param name="other">碰撞的实体</param>
    protected override void OnCollisionEntered(Entity other)
    {
        base.OnCollisionEntered(other);

        // 检查是否可穿透
        if (CanPenetrate(other))
        {
            return;
        }

        // 处理碰撞效果
        HandleCollision(other);
    }

    /// <summary>
    /// 处理碰撞效果
    /// </summary>
    /// <param name="other">碰撞的实体</param>
    protected virtual void HandleCollision(Entity other)
    {
        // 造成伤害
        if (CanDamage && other.IsAlive)
        {
            DealDamage(other);
        }

        // 反弹处理
        if (CheckTag(BulletTag.Bounce) && other.CheckTag(EntityTag.Obstacle))
        {
            OnBounce(other);
            return;
        }

        // 默认行为：销毁弹幕
        QueueFree();
    }

    /// <summary>
    /// 对目标造成伤害
    /// </summary>
    /// <param name="target">目标实体</param>
    protected virtual void DealDamage(Entity target)
    {
        // 友军免疫检查
        if (CheckTag(BulletTag.FriendlyFireImmune))
        {
            // 检查是否与发射者阵营相同
            // 具体实现需要根据游戏逻辑调整
        }

        float finalDamage = attack.Get() * damageMultiplier;

        // 护甲穿透处理
        if (CheckTag(BulletTag.ArmorPiercing))
        {
            // 忽略部分或全部防御
            // 具体实现需要根据伤害系统调整
        }

        // 暴击处理
        if (CheckTag(BulletTag.Critical))
        {
            // 增加暴击几率
            // 具体实现需要根据暴击系统调整
        }

        Damage damage = new Damage
        {
            source = this,
            value = new Value(finalDamage)
        };

        target.Hurt(damage);

        // 吸血效果
        if (CheckTag(BulletTag.Lifesteal))
        {
            // 回复发射者生命值
            // 具体实现需要追踪发射者
        }
    }

    #endregion

    #region 特殊效果

    /// <summary>
    /// 反弹时触发
    /// </summary>
    /// <param name="obstacle">反弹的障碍物</param>
    protected virtual void OnBounce(Entity obstacle)
    {
        // 计算反弹方向
        Vector2 bounceDir = (Position - obstacle.Position).Normalized();
        MoveToward(bounceDir);
    }

    /// <summary>
    /// 分裂时触发
    /// </summary>
    protected virtual void OnSplit()
    {
        // 创建多个小弹幕
        // 具体实现需要根据弹幕系统调整
    }

    /// <summary>
    /// 创建滞留区域
    /// </summary>
    protected virtual void CreateLingeringZone()
    {
        // 在地面创建持续伤害区域
        // 具体实现需要根据游戏系统调整
    }

    /// <summary>
    /// 定时引爆时触发
    /// </summary>
    protected virtual void OnTimedExplosion()
    {
        // 产生范围爆炸
        // 具体实现需要根据爆炸系统调整
    }

    #endregion

    #region 生命周期重写

    protected override void OnEnterTree()
    {
        base.OnEnterTree();
        
        // 设置子弹标签
        AddTag(EntityTag.Bullet);
        
        // 设置碰撞层和掩码
        // Layer = 3（子弹层），让其他实体能检测到我们
        // Mask = 0，不主动检测任何层，不产生物理碰撞
        CollisionLayer = LAYER_BULLET;
        CollisionMask = MASK_BULLET;
    }

    protected override void OnStart()
    {
        base.OnStart();
        _aliveTime = 0f;
    }

    protected override void OnUpdate(double delta)
    {
        base.OnUpdate(delta);
        UpdateLifetime(delta);
    }

    #endregion
}

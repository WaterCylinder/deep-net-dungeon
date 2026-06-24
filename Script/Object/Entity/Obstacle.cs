using Godot;

/// <summary>
/// 障碍物实体类 - 地图中的静态障碍物
/// 如墙壁、岩石、箱子等，可阻挡弹幕和实体移动
/// </summary>
public partial class Obstacle : Entity
{
    #region 碰撞层配置

    /// <summary>
    /// 障碍物碰撞层（第4层）
    /// </summary>
    private const int LAYER_OBSTACLE = 4;
    
    /// <summary>
    /// 障碍物碰撞掩码
    /// 检测：玩家(Layer1)、敌人(Layer2)
    /// 与玩家/敌人产生物理碰撞（阻挡移动）
    /// 不检测子弹(Layer3)，所以子弹不会与障碍物产生物理碰撞
    /// </summary>
    private const int MASK_OBSTACLE = (1 << 0) | (1 << 1); // Player | Enemy

    #endregion

    #region 障碍物属性

    /// <summary>
    /// 障碍物是否可被破坏
    /// </summary>
    [Export] public bool isDestructible = false;

    /// <summary>
    /// 障碍物是否可移动（默认静态障碍物不可移动）
    /// </summary>
    [Export] public bool isMovable = false;

    /// <summary>
    /// 障碍物被破坏时是否掉落物品
    /// </summary>
    [Export] public bool dropsItem = false;

    #endregion

    #region 初始化

    protected override void OnEnterTree()
    {
        base.OnEnterTree();
        
        // 设置障碍物标签
        AddTag(EntityTag.Obstacle);
        
        // 设置碰撞层和掩码
        // Layer = 4（障碍物层），让其他实体能检测到我们
        // Mask = Player | Enemy，与玩家/敌人产生物理碰撞（阻挡移动）
        // 不包含Bullet层，所以子弹不会与障碍物产生物理碰撞
        CollisionLayer = LAYER_OBSTACLE;
        CollisionMask = MASK_OBSTACLE;
    }

    protected override void OnStart()
    {
        base.OnStart();

        // 静态障碍物设置物理模式为静态
        if (!isMovable)
        {
            PhysicsServer2D.BodySetMode(GetRid(), PhysicsServer2D.BodyMode.Static);
        }
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

        // 检查是否被弹幕击中
        if (other is Bullet bullet)
        {
            OnBulletHit(bullet);
        }
    }

    /// <summary>
    /// 被弹幕击中时触发
    /// </summary>
    /// <param name="bullet">击中障碍物的弹幕</param>
    protected virtual void OnBulletHit(Bullet bullet)
    {
        // 检查障碍物是否可被破坏
        if (!isDestructible) return;

        // 检查弹幕是否可穿透障碍物
        if (bullet.CheckTag(BulletTag.PenetrateObstacle)) return;

        // 对障碍物造成伤害
        if (bullet.CanDamage)
        {
            Damage damage = new Damage
            {
                source = bullet,
                value = new Value(bullet.attack.Get() * bullet.damageMultiplier)
            };

            Hurt(damage);
        }
    }

    #endregion

    #region 受伤与死亡

    /// <summary>
    /// 受到伤害
    /// </summary>
    /// <param name="damage">伤害数据</param>
    /// <returns>实际受到的伤害值</returns>
    public override float Hurt(Damage damage)
    {
        if (!isDestructible) return 0f;

        float finalDamage = base.Hurt(damage);

        // 检查是否被破坏
        if (IsDead)
        {
            OnDestroyed(damage);
        }

        return finalDamage;
    }

    /// <summary>
    /// 障碍物被破坏时触发
    /// </summary>
    /// <param name="damage">导致破坏的伤害数据</param>
    protected virtual void OnDestroyed(Damage damage)
    {
        // 掉落物品
        if (dropsItem)
        {
            DropItem();
        }

        // 播放破坏效果
        PlayDestroyEffect();

        // 从场景中移除
        QueueFree();
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 掉落物品
    /// </summary>
    protected virtual void DropItem()
    {
        // 具体实现需要根据物品系统调整
        GD.Print($"{Name} 掉落物品");
    }

    /// <summary>
    /// 播放破坏效果
    /// </summary>
    protected virtual void PlayDestroyEffect()
    {
        // 具体实现需要根据特效系统调整
        GD.Print($"{Name} 播放破坏效果");
    }

    #endregion
}
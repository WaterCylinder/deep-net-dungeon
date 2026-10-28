using Godot;
using Godot.Collections;

/// <summary>
/// 弹幕标签枚举 - 定义弹幕的各种行为特性和效果
/// </summary>
public enum BulletTag
{

    /// <summary>
    /// 可穿透障碍物（包括Wall） - 弹幕碰到障碍物不会消失，继续飞行
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

    /// <summary>
    /// 忽略障碍物 - 弹幕不会和障碍物实体发生碰撞事件
    /// </summary>
    IgnoreObstacle,

    /// <summary>
    /// 忽略敌人 - 弹幕不会和敌人发生碰撞事件
    /// </summary>
    IgnoreEnemy,

    /// <summary>
    /// 忽略玩家 - 弹幕不会和玩家发生碰撞事件
    /// </summary>
    IgnorePlayer,

    /// <summary>
    /// 忽略所有实体 - 弹幕不会和任何实体发生碰撞事件
    /// </summary>
    IgnoreAll,

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

}

/// <summary>
/// 弹幕实体类 - 所有弹幕的基础类
/// 继承自Entity，支持弹幕标签系统
/// 不处理物理碰撞，但处理碰撞效果
/// </summary>
public partial class Bullet : Entity
{
    public const float BASE_SHOT_SPEED_FECTOR = 100f;

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

    #region 默认数据

    private static BulletData _defaultData = null;

    /// <summary>
    /// 默认弹幕数据路径
    /// </summary>
    public static string DefaultDataPath
    {
        get => "Data/bullet_data.tres";
    }

    /// <summary>
    /// 默认弹幕数据（从Asset/Default/Data/bullet_data.tres加载）
    /// </summary>
    public static BulletData DefaultData
    {
        get
        {
            if (_defaultData == null)
            {
                _defaultData = AssetManager.Instance.GetDefault<BulletData>(DefaultDataPath);
                if (_defaultData == null)
                {
                    GD.PrintErr("未找到默认弹幕数据，使用新建默认数据");
                    _defaultData = new BulletData();
                }
            }
            return _defaultData;
        }
    }

    private static BulletData _defaultMeleeData = null;
    
    /// <summary>
    /// 默认近战弹幕数据（从Asset/Default/Data/melee_bullet_data.tres加载）
    /// </summary>
    public static BulletData DefaultMeleeData
    {
        get
        {
            if (_defaultMeleeData == null)
            {
                _defaultMeleeData = AssetManager.Instance.GetDefault<BulletData>("Data/melee_bullet_data.tres");
                if (_defaultMeleeData == null)
                {
                    GD.PrintErr("未找到默认近战弹幕数据，使用新建默认数据");
                }
            }
            return _defaultMeleeData;
        }
    }

    #endregion

    #region 工厂方法

    /// <summary>
    /// 在指定位置创建弹幕
    /// </summary>
    /// <param name="owner">发射者</param>
    /// <param name="pos">世界坐标位置</param>
    /// <param name="velocity">初速度</param>
    /// <param name="data">弹幕数据</param>
    /// <param name="parent">父节点（默认添加到当前场景）</param>
    /// <param name="isVisible">是否可见(是否在数据图像为空的情况下显示默认图像)</param>
    /// <returns>创建的弹幕实例</returns>
    public static Bullet Create(Entity owner, Vector2 pos, Vector2 velocity, BulletData data, Node parent = null, bool isVisible = true)
    {
        // 如果数据为空，使用默认数据
        if (data == null)
        {
            data = DefaultData;
        }

        if (BulletPrefab == null)
        {
            GD.PrintErr("无法创建弹幕：Bullet预制体未找到");
            return null;
        }

        Bullet bullet = BulletPrefab.Instantiate<Bullet>();
        if (bullet == null)
        {
            GD.PrintErr("无法创建弹幕：实例化失败");
            return null;
        }

        // 设置发射者
        bullet.owner = owner;

        // 初始化数据
        bullet.Initialize(data);

        
        // 转移攻击/暴击等数值到弹幕上。
        // 注意这里只判断"对象是否有效"，不能用 Game.IsAble：死亡回调里（例如电浆蜘蛛的死亡弹幕）
        // owner 的 isDead 已经是 true，若按 IsAble 判断就会跳过转移，
        // 弹幕只能吃到 bullet.tscn 预制体里的默认攻击力（10），伤害会莫名其妙地变得很低。
        if (owner != null && GodotObject.IsInstanceValid(owner))
        {
            // 转移伤害基础值到弹幕上
            bullet.attack = owner.attack.Get();
            // 转移暴击率和暴击伤害到弹幕上
            bullet.critRate = owner.critRate.Get();
            bullet.critDamage = owner.critDamage.Get();
        }

        // 设置位置和速度
        bullet.Position = pos;
        bullet.moveTowards = velocity.Normalized();
        bullet.speed = velocity.Length() * BASE_SHOT_SPEED_FECTOR;

        // 立即移动
        bullet.Move();

        // 根据归属设置标签
        if (owner != null)
        {
            if (owner.CheckTag(EntityTag.Player))
            {
                bullet.AddTag(BulletTag.PlayerOwned);
            }
            else if (owner.CheckTag(EntityTag.Enemy))
            {
                bullet.AddTag(BulletTag.EnemyOwned);
            }
        }

        // 添加到场景
        if (parent == null)
        {
            parent = Engine.GetMainLoop() is SceneTree sceneTree ? sceneTree.CurrentScene : null;
        }
        parent?.CallDeferred("add_child", bullet);

        //如果生成不可见弹幕，则清除弹幕的精灵图像。
        if (bullet.spriteNode != null && !isVisible)
        {
            bullet.spriteNode.Texture = null;
        }

        bullet.onCreate?.Run(bullet, owner, null);
        bullet.EmitSignal(nameof(bullet.OnCreate));

        return bullet;
    }

    #endregion

    public void PlayAnime(string animeName) => PlayAnime(animeName, animNode);

    #region 弹幕属性

    /// <summary>
    /// 弹幕数据源
    /// </summary>
    public BulletData data;

    /// <summary>
    /// 弹幕发射者
    /// </summary>
    [Export] public Entity owner;

    /// <summary>
    /// 弹幕标签列表
    /// </summary>
    [Export] public Array<BulletTag> bulletTags = new();

    /// <summary>
    /// 弹幕存在时间（秒），0表示永久存在
    /// </summary>
    [Export] public float lifetime = 5f;

    /// <summary>
    /// 出生保护时间（秒）：发射后这段时间内不结算实体碰撞，避免贴脸/重叠生成时被立刻吃掉。
    /// 保护结束时会对"当前仍然重叠"的实体立刻补一次命中检测，所以保护期后不会漏判。
    /// 可以在 BulletData 的 args 里用 spawn_protect 覆盖，0 = 关闭
    /// </summary>
    [Export] public float spawnProtectTime = 0.1f;

    ulong spawnProtectStartMs;
    bool spawnProtectDone;

    /// <summary>
    /// 弹幕伤害倍率
    /// </summary>
    [Export] public float damageMultiplier = 1f;

    [Export] public Node2D layout;

    private bool isRotateWithToward = true;

    public Sprite2D spriteNode;
    public AnimatedSprite2D animNode;

    /// <summary>
    /// 创建时的行为
    /// </summary>
    private Behavior onCreate;
    /// <summary>
    /// 更新时的行为
    /// </summary>
    private Behavior onUpdate;
    private Behavior onPhysicalUpdate;

    /// <summary>
    /// 击中实体时触发的效果（伤害结算后触发）
    /// </summary>
    private Effect onHit;

    /// <summary>
    /// 生命周期结束时触发的效果
    /// </summary>
    private Effect onLifetimeEnd;

    /// <summary>
    /// 弹幕消失时
    /// </summary>
    private Effect onDead;

    /// <summary>
    /// 保证弹幕在生成后不会立刻被墙卡住
    /// </summary>
    private bool generateProtection = true;

    public AudioStream soundOnShot;
    public AudioStream soundOnHit;

    #endregion

    #region 信号

    /// <summary>
    /// 创建时
    /// </summary>
    [Signal]
    public delegate void OnCreateEventHandler();
    /// <summary>
    /// 击中实体时（每个被击中的实体触发一次，与 onHit 效果同时机）
    /// </summary>
    /// <param name="target">被击中的实体</param>
    [Signal]
    public delegate void OnHitEventHandler(Entity target);

    #endregion

    #region 标签管理

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

        if (target.CheckTag(EntityTag.Player))
        {
            if(CheckTag(BulletTag.PenetratePlayer))return true;
            if(IsPlayerBullet)return true;
        }
        if (target.CheckTag(EntityTag.Enemy))
        {
            if(CheckTag(BulletTag.PenetrateEnemy))return true;
            if(IsEnemyBullet)return true;
        } 
        if (target.CheckTag(EntityTag.Obstacle) && CheckTag(BulletTag.PenetrateObstacle)) return true;

        return false;
    }

    /// <summary>
    /// 检测弹幕是否会忽略与指定实体的碰撞事件
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool IsIgnore(Entity target)
    {
        if (CheckTag(BulletTag.IgnoreAll)) return true;

        if (target.CheckTag(EntityTag.Player) && CheckTag(BulletTag.IgnorePlayer))
        {
            if(CheckTag(BulletTag.IgnorePlayer))return true;
            if(IsPlayerBullet)return true;
        }
        if (target.CheckTag(EntityTag.Enemy))
        {
            if(CheckTag(BulletTag.IgnoreEnemy))return true;
            if(target is Enemy enemy)
            {
                if(enemy.CheckTag(EnemyTag.IgnoreBullet))
                    return true;
            }
            if(IsEnemyBullet)return true;
        } 
        if (target.CheckTag(EntityTag.Obstacle) && CheckTag(BulletTag.IgnoreObstacle)) return true;

        return false;
    }

    #endregion

    #region 初始化

    /// <summary>
    /// 使用BulletData初始化弹幕
    /// </summary>
    /// <param name="data">弹幕数据</param>
    public void Initialize(BulletData data)
    {
        // 如果数据为空，使用默认数据
        if (data == null)
        {
            data = DefaultData;
            GD.Print($"弹幕 {Name} 使用默认数据初始化");
        }

        this.data = data;

        // 设置名称
        Name = data.bulletName + Time.GetUnixTimeFromSystem();

        // 设置标签
        bulletTags = [.. data.bulletTags];

        //设置参数
        args = new();
        args.Init(data.args);

        // 设置生命周期
        lifetime = data.lifetime;

        // 设置伤害倍率
        damageMultiplier = data.damageMultiplier;

        // 设置设置项
        isRotateWithToward = data.isRotateWithToward;

        // 设置行为和效果
        onCreate = data.onCreate?.Duplicate(false) as Behavior;
        onUpdate = data.onUpdate?.Duplicate(false) as Behavior;
        onPhysicalUpdate = data.onPhysicalUpdata?.Duplicate(false) as Behavior;
        onHit = data.onHit?.Duplicate(false) as Effect;
        onLifetimeEnd = data.onLifetimeEnd?.Duplicate(false) as Effect;
        onDead = data.onDead?.Duplicate(false) as Effect;

        spriteNode = layout.GetNodeOrNull<Sprite2D>("Sprite");
        animNode = layout.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite");

        soundOnHit = data.soundOnHit;
        soundOnShot = data.soundOnShot;

        // 设置贴图
        if (data.sprite != null)
        {
            if (spriteNode != null)
            {
                spriteNode.Texture = data.sprite;
            }
        }

        if(data.spriteFrames != null)
        {
            if(animNode != null)
            {
                if(data.spriteFrames != null)
                {
                    spriteNode.Visible = false;
                    animNode.SpriteFrames = data.spriteFrames;
                    animNode.Play("normal");
                }
            }
        }

        layout.Scale = data.scale;
        layout.Position = data.layoutOffset;

        //注意：弹幕预制体里的CircleShape2D是SubResource，所有实例默认共享同一个形状资源。
        //直接改Radius会让"最后生成的那发弹幕"决定全场弹幕的碰撞半径——
        //例如WSAWA的body弹幕把半径设成64，之后boss随手打出一发普通弹(默认半径)就会把它改回去。
        //所以这里每个实例复制一份自己的形状
        var areaShape = area.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (areaShape != null && areaShape.Shape is CircleShape2D)
        {
            CircleShape2D shape = (CircleShape2D)areaShape.Shape.Duplicate();
            shape.Radius = data.collisionRadius;
            areaShape.Shape = shape;
        }
        area.Scale = data.scale;
    }

    #endregion

    #region 生命周期

    protected override void OnEnterTree()
    {
        base.OnEnterTree();
        
        // 设置子弹标签
        AddTag(EntityTag.Bullet);
    }
    
    /// <summary>
    /// 弹幕已存在时间
    /// </summary>
    private float aliveTime = 0f;

    /// <summary>
    /// 获取弹幕已存在时间
    /// </summary>
    /// <returns></returns>
    public float GetAliveTime()
    {
        return aliveTime;
    }

    /// <summary>
    /// 更新弹幕生命周期
    /// </summary>
    /// <param name="delta">帧间隔时间</param>
    private void UpdateLifetime(double delta)
    {
        if (lifetime <= 0) return;

        aliveTime += (float)delta;
        if (aliveTime >= lifetime)
        {
            OnLifetimeEnd();
        }
        if(aliveTime > float.MaxValue - 1000f)
        {
            aliveTime = 9999999f; // 防止溢出
        }
    }

    /// <summary>
    /// 生命周期结束时触发
    /// </summary>
    protected virtual void OnLifetimeEnd()
    {   
        onLifetimeEnd?.Run(this, owner, null);

        Kill();
    }


    protected override void OnStart()
    {
        base.OnStart();
        aliveTime = 0f;

        //出生保护：发射后短时间内部不结算碰撞（可用数据里的spawn_protect覆盖）
        spawnProtectTime = args.Get<float>("spawn_protect", spawnProtectTime);
        spawnProtectStartMs = Time.GetTicksMsec();
        spawnProtectDone = spawnProtectTime <= 0f;

        // 取消与障碍物的碰撞
        if (CheckTag(BulletTag.PenetrateObstacle) || CheckTag(BulletTag.PenetrateAll)
            || CheckTag(BulletTag.IgnoreObstacle) || CheckTag(BulletTag.IgnoreAll))
        {
            SetCollisionMaskValue(8, false);
        }
        else
        {
            BodyEntered += OnWallEnter;
        }
        
        owner?.PlaySound(soundOnShot, owner.bulletAudioPlayer);
    }

    protected override void OnUpdate(double delta)
    {
        base.OnUpdate(delta);
        UpdateLifetime(delta);
        UpdateSpawnProtect();

        if(isRotateWithToward && moveTowards != Vector2.Zero)
        {
            RotateTo(moveTowards.Angle());
        }

        // 执行更新行为
        onUpdate?.Run(this, (float)delta);
    }

    protected override void OnPhysicsUpdate(double delta)
    {
        base.OnPhysicsUpdate(delta);
        onPhysicalUpdate?.Run(this, (float)delta);
    }

    protected override void OnDeath(Damage damage, Entity other)
    {
        base.OnDeath(damage, other);
        onDead?.Run(this);
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

        // 忽略与发射者的碰撞
        if(other == owner)
        {
            return;
        }

        // 出生保护期内不结算：交给 UpdateSpawnProtect 在保护结束时补一次检测
        if(!spawnProtectDone)
        {
            return;
        }

        ResolveHit(other);
    }

    /// <summary>
    /// 出生保护检测：时间一到，立刻对当前重叠着的实体补一次碰撞结算。
    /// 因为Area2D一直在监测，重叠列表是实时的，所以"0.1秒后立刻检测"不会漏判；
    /// 保护期内就离开的实体自然不会出现在重叠列表里，也就不会被打到。
    /// </summary>
    void UpdateSpawnProtect()
    {
        if(spawnProtectDone || isDead)
        {
            return;
        }

        if(Time.GetTicksMsec() - spawnProtectStartMs < (ulong)(spawnProtectTime * 1000f))
        {
            return;
        }

        spawnProtectDone = true;

        if(area == null || !IsInstanceValid(area))
        {
            return;
        }

        foreach(Node node in area.GetOverlappingBodies())
        {
            if(node is not Entity other || other == owner)
            {
                continue;
            }

            ResolveHit(other);

            //不可穿透的弹幕已经销毁，不用继续结算
            if(isDead)
            {
                return;
            }
        }
    }

    /// <summary>
    /// 结算一次实体碰撞：命中效果 + 不可穿透则销毁
    /// </summary>
    void ResolveHit(Entity other)
    {
        // 处理碰撞效果
        if (!IsIgnore(other))
        {
            HandleCollision(other);
        }

        // 检查是否可穿透
        if (!CanPenetrate(other))
        {
            Kill();
        }
    }

    /// <summary>
    /// 处理碰撞：先结算伤害，再统一触发命中
    /// </summary>
    /// <param name="other">碰撞的实体</param>
    protected virtual void HandleCollision(Entity other)
    {
        // 造成伤害
        if (CanDamage && other.IsAlive)
        {
            DealDamage(other);
        }

        // 命中实体：触发命中效果与命中信号（伤害已结算）
        OnHitEntity(other);
    }

    /// <summary>
    /// 击中实体时触发：执行 onHit 效果并发出 OnHit 信号。
    /// 弹幕与实体的所有交互效果统一走这里，不再区分"碰撞"与"造成伤害"两套时机。
    /// </summary>
    /// <param name="target">被击中的实体</param>
    protected virtual void OnHitEntity(Entity target)
    {
        // 参数顺序与 onCreate 保持一致：(弹幕, 发射者, 被击中者)
        onHit?.Run(this, owner, target);
        PlaySoundGlobal(soundOnHit);
        EmitSignal(nameof(OnHit), target);
    }

    /// <summary>
    /// 对目标造成伤害，伤害计算时将该弹幕的attack属性的值乘上弹幕伤害乘区。
    /// </summary>
    /// <param name="target">目标实体</param>
    protected virtual void DealDamage(Entity target)
    {
        // 计算最终伤害
        float finalDamage = attack.Get() * damageMultiplier;

        if(finalDamage < 0)return;

        Damage damage = new Damage
        {
            source = this,
            value = new Value(finalDamage)
        };

        damage.critDamage = critDamage;
        damage.critRate = critRate;

        target.Hurt(damage);
    }

    protected void OnWallEnter(Node other)
    {
        if (generateProtection)
        {
            GlobalPosition -= GetVelocity().Normalized() * 1;
            generateProtection = false;
        }
        //对于非实体碰撞如果子弹没有穿过障碍物标签则直接消失
        if (!CheckTag(BulletTag.PenetrateObstacle) && !CheckTag(BulletTag.PenetrateAll)
        && !CheckTag(BulletTag.IgnoreObstacle) && !CheckTag(BulletTag.IgnoreAll))
        {
            Kill();
        }
    }

    #endregion

   
}



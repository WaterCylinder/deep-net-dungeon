using Godot;
using Godot.Collections;

public enum EnemyTag
{
    Normal,
    /// <summary>
    /// 不可被选中
    /// </summary>
    TriggerDisable,
    /// <summary>
    /// 不会被弹幕击中
    /// </summary>
    IgnoreBullet,
    /// <summary>
    /// 忽略物理效果(不会推开玩家和被玩家推开)
    /// </summary>
    IgnorePhysical,
    /// <summary>
    /// BOSS怪物
    /// </summary>
    BOSS,
}

/// <summary>
/// 敌人实体类
/// </summary>
public partial class Enemy : Entity
{
    /// <summary>
    /// 默认近战攻击速度 次/秒
    /// </summary>
    public static float MELEE_SPEED = 2;

    #region 默认数据

    /// <summary>
    /// 默认敌人数据缓存
    /// </summary>
    private static EnemyData _defaultData = null;

    /// <summary>
    /// 获取默认敌人数据（从Asset/Default/Data/enemy_data.tres加载）
    /// </summary>
    public static EnemyData DefaultData
    {
        get
        {
            if (_defaultData == null)
            {
                _defaultData = AssetManager.Instance.GetDefault<EnemyData>("Data/enemy_data.tres");
                if (_defaultData == null)
                {
                    GD.PrintErr("未找到默认敌人数据！");
                    _defaultData = new EnemyData();
                }
            }
            return _defaultData;
        }
    }

    #endregion

    #region 工厂方法

    /// <summary>
    /// 敌人预制体缓存
    /// </summary>
    private static PackedScene _enemyPrefab = null;
    
    /// <summary>
    /// 敌人预制体
    /// </summary>
    public static PackedScene EnemyPrefab
    {
        get
        {
            if (_enemyPrefab == null)
            {
                _enemyPrefab = AssetManager.Instance.GetPrefab("Asset.Object.enemy", null);
            }
            return _enemyPrefab;
        }
    }

    /// <summary>
    /// 在指定位置创建敌人
    /// </summary>
    /// <param name="pos">世界坐标位置</param>
    /// <param name="data">敌人数据</param>
    /// <param name="parent">父节点（默认添加到当前场景）</param>
    /// <returns>创建的敌人实例</returns>
    public static Enemy Create(Vector2 pos, EnemyData data, Node parent = null)
    {
        // 如果数据为空，使用默认数据
        if (data == null)
        {
            GD.PrintErr("创建敌人：使用默认数据");
            data = DefaultData;
        }

        if (EnemyPrefab == null)
        {
            GD.PrintErr("无法创建敌人：Enemy预制体未找到");
            return null;
        }

        Enemy enemy = EnemyPrefab.Instantiate<Enemy>();
        if (enemy == null)
        {
            GD.PrintErr("无法创建敌人：实例化失败");
            return null;
        }

        // 添加到场景
        if (parent == null)
        {
            parent = Engine.GetMainLoop() is SceneTree sceneTree ? sceneTree.CurrentScene : null;
        }
        parent.AddChild(enemy);

        // 设置位置
        enemy.GlobalPosition = pos;
        enemy.Name = data.enemyName;

        // 初始化数据
        enemy.Initialize(data);

        return enemy;
    }

    #endregion

    public void PlayAnime(string animeName = "idle") => PlayAnime(animeName, animNode);

    #region 敌人属性

    public Node2D layoutNode;
    public Sprite2D spriteNode;
    public AnimatedSprite2D animNode;

    public Vector2 scale;

    [Export] public Array<EnemyTag> enemyTags = new();

    /// <summary>
    /// 敌人数据源
    /// </summary>
    [Export] public EnemyData data;

    /// <summary>
    /// 目标实体（用于AI追踪）
    /// </summary>
    [Export] public Entity target;

    /// <summary>
    /// 近战弹幕
    /// </summary>
    [Export] public Bullet meleeBullet;

    [Export] public Effect onGenerate;
    [Export] public Effect onUpdate;
    [Export] public Effect onAttack;
    [Export] public Effect onDeath;

    /// <summary>
    /// 掉落资源点数量区间
    /// </summary>
    [Export] public Vector2I dropValue;

    public bool canMelee = true;

    #endregion
    
    #region 标签管理

    /// <summary>
    /// 检查敌人是否有指定标签
    /// </summary>
    /// <param name="tag">要检查的标签</param>
    /// <returns>是否拥有该标签</returns>
    public bool CheckTag(EnemyTag tag)
    {
        return enemyTags.Contains(tag);
    }

    /// <summary>
    /// 添加敌人标签
    /// </summary>
    /// <param name="tag">要添加的标签</param>
    public void AddTag(EnemyTag tag)
    {
        if (!enemyTags.Contains(tag))
        {
            enemyTags.Add(tag);
        }
    }

    /// <summary>
    /// 移除敌人标签
    /// </summary>
    /// <param name="tag">要移除的标签</param>
    public void RemoveTag(EnemyTag tag)
    {
        if (enemyTags.Contains(tag))
        {
            enemyTags.Remove(tag);
        }
    }

    #endregion

    #region 碰撞层设置

    protected override void OnEnterTree()
    {
        base.OnEnterTree();
        
        // 设置敌人标签
        AddTag(EntityTag.Enemy);
    }

    #endregion

    #region 初始化

    /// <summary>
    /// 使用EnemyData初始化敌人
    /// </summary>
    /// <param name="data">敌人数据</param>
    public void Initialize(EnemyData data)
    {
        // 如果数据为空，使用默认数据
        if (data == null)
        {
            data = DefaultData;
            GD.Print($"敌人使用默认数据初始化");
        }

        this.data = data;

        //设置节点路径
        layoutNode = GetNodeOrNull<Node2D>("Layout");
        spriteNode = layoutNode?.GetNodeOrNull<Sprite2D>("Sprite");
        animNode = layoutNode?.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite");

        // 设置基础属性
        maxHealth = data.maxHealth;
        attack = data.attack;
        defense = data.defense;
        attackSpeed = data.attackSpeed;
        attackRange = data.attackRange;
        critRate = data.critRate;
        critDamage = data.critDamage;
        speed = data.speed;

        onGenerate = data.onGenerate;
        onUpdate = data.onUpdate;
        onAttack = data.onAttack;
        onDeath = data.onDeath;

        dropValue = data.dropValue;

        //设置节点属性
        Name = data.enemyName;

        // 复制标签
        foreach (var tag in data.tags)
        {
            AddTag(tag);
        }

        // 挂载初始Buff
        foreach (var buffData in data.initialBuffs)
        {
            if (buffData != null)
            {
                AttachBuff(buffData);
            }
        }

        // 设置贴图
        if (data.sprite != null)
        {
            if (spriteNode != null)
            {
                spriteNode.Texture = data.sprite;
            }
        }
        // 设置动画
        if (data.spriteFrames != null)
        {
            if (animNode != null)
            {
                animNode.SpriteFrames = data.spriteFrames;
                spriteNode.Visible = false;
                //初始播放默认静止动画
                animNode.Play("idle");
            }
        }
        layoutNode.Position = data.layoutOffset;
        
        // 设置物理性状
        if (data.physicShape != null)
        {
            var physicShape = GetNodeOrNull<CollisionShape2D>("CollisionShape");
            if (physicShape != null)
            {
                physicShape.Shape = data.physicShape;
            }
        }

        // 设置检测范围
        if (data.areaShape != null)
        {
            var areaShape = GetNodeOrNull<CollisionShape2D>("Area/CollisionShape");
            if (areaShape != null)
            {
                areaShape.Shape = data.areaShape;
                areaShape.Position = data.areaOffset;
            }
        }

        //设置尺寸
        scale = data.scale;

        //设置初始额外参数
        args = new();
        args.Init(data.args);

        // 重置生命值到最大值
        currentHealth = maxHealth.Get();

        // 根据tags设置性能
        if (CheckTag(EnemyTag.IgnorePhysical))
        {
            //移除该物体的碰撞层
            SetCollisionLayerValue(1, false);
        }
    }

    #endregion

    #region 行为更新

    public void OnGenerate()
    {
        if(bulletData == null)
        {
            ChangeBullet(Bullet.DefaultData);
        }
        onGenerate?.Run(this);
    }

    float timer = 0f;
    protected override void OnUpdate(double delta)
    {
        base.OnUpdate(delta);

        // 如果有近战形状则生成近战弹幕
        if (data.attackShape != null && canMelee)
        {
            timer += (float)delta;
            if(timer > (1 / MELEE_SPEED))
            {
                timer = 0;
                if (!Game.IsAble(meleeBullet))
                {
                    //生成不可见的近战弹幕
                    meleeBullet = Bullet.Create(
                        owner: this,
                        pos: GlobalPosition + data.meleeOffest,
                        velocity: Vector2.Zero,
                        data: Bullet.DefaultMeleeData,
                        parent: this,
                        isVisible: false
                    );
                    //设置近战弹幕的碰撞形状
                    var shape = meleeBullet.GetNodeOrNull<CollisionShape2D>("Area2D/CollisionShape2D");
                    if(shape != null)
                    {
                        shape.Shape = data.attackShape;
                    }
                    meleeBullet.Position = Vector2.Zero;
                }
                else
                {
                    meleeBullet.Position = Vector2.Zero;
                }
            }
            else
            {
                if (Game.IsAble(meleeBullet))
                {
                    meleeBullet.Position = Vector2.Zero;
                }
            }
            
        }

        onUpdate?.Run(this);
    }

    protected override void OnAttack()
    {
        base.OnAttack();
        onAttack?.Run(this);
    }

    protected override void OnDeath(Damage damage, Entity other)
    {
        base.OnDeath(damage, other);
        int dvalue = game.randomSystem.Int(dropValue.X, dropValue.Y);
        if(dvalue > 0)
        {
            EnergyPoint.CreateEnergyPoint(EnergyType.Coin, GlobalPosition, dvalue);
        }
        //如果有死亡动画，则在死亡时播放死亡动画。
        if(animNode.SpriteFrames != null && animNode.SpriteFrames.HasAnimation("death"))
        {
            queueFreeOnDeath = false;
            ClearAllTimer();
            animNode.Stop();
            animNode.Play("death");
            animNode.AnimationFinished += QueueFree;
            if (meleeBullet != null && IsInstanceValid(meleeBullet))
            {
                meleeBullet.QueueFree();
                canMelee = false;
            }
        }
        onDeath?.Run(this);
    }

    #endregion

    #region 功能方法

    public void ScaleLayout(Vector2 scale)
    {
        if(layoutNode != null)
        {
            layoutNode.Scale = this.scale * scale;
        }
    }

    #endregion
    
}
using Godot;
using Godot.Collections;

/// <summary>
/// 敌人实体类
/// </summary>
public partial class Enemy : Entity
{
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
                _enemyPrefab = AssetManager.Instance.GetPrefab("Scene.Entity.Enemy", null);
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

        // 设置位置
        enemy.Position = pos;
        enemy.Name = data.enemyName;

        // 初始化数据
        enemy.Initialize(data);

        // 添加到场景
        if (parent == null)
        {
            parent = Engine.GetMainLoop() is SceneTree sceneTree ? sceneTree.CurrentScene : null;
        }
        parent?.AddChild(enemy);

        return enemy;
    }

    #endregion

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
                _defaultData = AssetManager.Instance.GetDefault<EnemyData>("Data/enemy_data");
                if (_defaultData == null)
                {
                    GD.PrintErr("未找到默认敌人数据，使用新建默认数据");
                    _defaultData = new EnemyData();
                }
            }
            return _defaultData;
        }
    }

    #endregion

    #region 敌人属性

    /// <summary>
    /// 敌人数据源
    /// </summary>
    public EnemyData data;

    /// <summary>
    /// 敌人行为列表
    /// </summary>
    private Array<Behavior> _behaviors = new();

    /// <summary>
    /// 目标实体（用于AI追踪）
    /// </summary>
    public Entity target;

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
            GD.Print($"敌人 {Name} 使用默认数据初始化");
        }

        this.data = data;

        // 设置基础属性
        maxHealth = new Value(data.maxHealth.baseValue);
        attack = new Value(data.attack.baseValue);
        defense = new Value(data.defense.baseValue);
        attackSpeed = new Value(data.attackSpeed.baseValue);
        attackRange = new Value(data.attackRange.baseValue);
        critRate = new Value(data.critRate.baseValue);
        critDamage = new Value(data.critDamage.baseValue);
        speed = new Value(data.speed.baseValue);

        // 复制标签
        foreach (var tag in data.tags)
        {
            AddTag(tag);
        }

        // 复制行为列表
        _behaviors = new Array<Behavior>(data.behaviors);

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
            var spriteNode = GetNodeOrNull<Sprite2D>("Sprite2D");
            if (spriteNode != null)
            {
                spriteNode.Texture = data.sprite;
                spriteNode.Scale = data.scale;
            }
        }

        // 重置生命值到最大值
        currentHealth = maxHealth.Get();
    }

    #endregion

    #region 行为更新

    protected override void OnUpdate(double delta)
    {
        base.OnUpdate(delta);

        // 执行所有行为
        foreach (var behavior in _behaviors)
        {
            behavior?.Run(this, target, delta);
        }
    }

    #endregion

    #region 受伤与死亡

    protected override void OnHurt(Damage damage, float finalDamage)
    {
        base.OnHurt(damage, finalDamage);

        // 触发受伤效果
        data?.onHurt?.Run(this, damage, finalDamage);
    }

    protected override void OnDeath(Damage damage)
    {
        base.OnDeath(damage);

        // 触发死亡效果
        data?.onDeath?.Run(this, damage);
    }

    #endregion

    
}
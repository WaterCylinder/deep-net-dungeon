using Godot;
using Godot.Collections;
using System.Collections.Generic;

public enum EntityTag
{
    Player,
    Enemy,
    Bullet,
    Obstacle,
}

/// <summary>
/// 实体抽象基类 - 所有游戏实体的基础
/// 包含基础属性系统和Buff管理系统
/// 继承CharacterBody2D以支持物理移动
/// </summary>
public abstract partial class Entity : RigidBody2D
{
    #region 基础属性

    [Export]public Array<EntityTag> tags = new Array<EntityTag>();

    /// <summary>
    /// 最大生命值
    /// </summary>
    [Export] public Value maxHealth = new(100f);

    /// <summary>
    /// 当前生命值
    /// </summary>
    [Export] public float currentHealth = 100f;

    /// <summary>
    /// 攻击力
    /// </summary>
    [Export] public Value attack = new(10f);

    /// <summary>
    /// 防御力
    /// </summary>
    [Export] public Value defense = new(5f);

    /// <summary>
    /// 攻击速度，每秒多少次攻击
    /// </summary>
    [Export] public Value attackSpeed = new(1f);

    /// <summary>
    /// 攻击范围
    /// </summary>
    [Export] public Value attackRange = new(100f);

    /// <summary>
    /// 暴击率（0-1）
    /// </summary>
    [Export] public Value critRate = new(0f);

    /// <summary>
    /// 暴击伤害倍数
    /// </summary>
    [Export] public Value critDamage = new(1.5f);

    /// <summary>
    /// 速度
    /// </summary>
    [Export] public Value speed = new(100f);

    #endregion

    #region 状态属性

    /// <summary>
    /// 实体是否存活
    /// </summary>
    public bool IsAlive => currentHealth > 0;

    /// <summary>
    /// 实体是否已死亡
    /// </summary>
    public bool IsDead => currentHealth <= 0;

    /// <summary>
    /// 生命值百分比（0-1）
    /// </summary>
    public float HealthPercent => maxHealth.Get() > 0 ? currentHealth / maxHealth.Get() : 0f;

    /// <summary>
    /// 攻速计时器
    /// </summary>
    float attackTimer = 0f;

    #endregion
    
    #region 组件

    [Export] public Area2D area = null;

    #endregion

    #region tag

    public bool CheckTag(EntityTag tag)
    {
        return tags.Contains(tag);
    }

    public void AddTag(EntityTag tag)
    {
        if (!tags.Contains(tag))
        {
            tags.Add(tag);
        }
    }

    public void RemoveTag(EntityTag tag)
    {
        if (tags.Contains(tag))
        {
            tags.Remove(tag);
        }
    }

    #endregion 

    #region 实体移动系统

    /// <summary>
    /// 当前速度向量，请勿手动修改（在Move方法里自动计算）
    /// </summary>
    [Export] protected Vector2 velocity = Vector2.Zero;
    /// <summary>
    /// 当前环境速度
    /// </summary>
    protected Vector2 environmentVelocity = Vector2.Zero;
    /// <summary>
    /// 当前移动方向
    /// </summary>
    protected Vector2 moveTowards = Vector2.Zero;
    /// <summary>
    /// 平滑参数
    /// </summary>
    protected float smoothFactor = 0;

    /// <summary>
    /// 移动，平滑移动请帧调用
    /// </summary>
    public void Move()
    {
        Vector2 localVelocity = moveTowards * speed;
        if(smoothFactor > 0)
        {
            // 从当前速度插值到目标速度，smoothFactor越大越快达到目标速度
            velocity = (velocity - environmentVelocity).Lerp(
                localVelocity, smoothFactor
            ) + environmentVelocity;
        }
        else
        {
            velocity = localVelocity + environmentVelocity;
        }
        LinearVelocity = velocity;
    }

    /// <summary>
    /// 向指定方向移动，平滑移动请帧调用
    /// </summary>
    /// <param name="toward"></param>
    public void MoveToward(Vector2 toward)
    {
        moveTowards = toward.Normalized();
        Move();
    }

    /// <summary>
    /// 向指定位置移动，平滑移动请帧调用
    /// </summary>
    /// <param name="toward"></param>
    public void MoveTarget(Vector2 target, float stopDistance = 5f)
    {
        Vector2 toward = target - Position;
        if(toward.Length() > stopDistance)
        {
            MoveToward(toward);
        }
        else
        {
            MoveStop();
        }
    }

    /// <summary>
    /// 停止移动，平滑移动请帧调用
    /// </summary>
    public void MoveStop()
    {
        moveTowards = Vector2.Zero;
        Move();
    }

    #endregion

    #region 碰撞系统

    /// <summary>
    /// 当前碰撞中的Entity集合
    /// </summary>
    private HashSet<Entity> _collidingEntities = new();

    #endregion

    #region Buff系统

    /// <summary>
    /// 当前挂载的所有Buff
    /// </summary>
    private List<Buff> _buffs = new();

    /// <summary>
    /// 获取所有Buff的只读列表
    /// </summary>
    public IReadOnlyList<Buff> Buffs => _buffs.AsReadOnly();

    /// <summary>
    /// 挂载Buff
    /// </summary>
    /// <param name="buff">Buff实例</param>
    /// <param name="baseLayer">基础层数</param>
    /// <returns>挂载后的Buff（可能已存在并叠加）</returns>
    public Buff AttachBuff(Buff buff, int baseLayer = 1)
    {
        if (buff == null || buff.Data == null)
        {
            GD.PrintErr($"尝试挂载空Buff到 {Name}");
            return null;
        }

        // 检查是否是唯一Buff
        if (buff.IsUnique)
        {
            Buff existingBuff = GetBuff(buff.Data.name);
            if (existingBuff != null)
            {
                // 已存在同类Buff，刷新持续时间
                existingBuff.RefreshDuration();
                return existingBuff;
            }
        }

        // 设置Owner并添加
        buff.Owner = this;
        buff.SetStack(baseLayer);
        _buffs.Add(buff);
        
        // 触发挂载事件
        buff.OnAttach();
        OnBuffAttached(buff);
        
        return buff;
    }

    /// <summary>
    /// 通过BuffData挂载Buff
    /// </summary>
    /// <param name="data">Buff数据</param>
    /// <param name="baseLayer">基础层数</param>
    /// <returns>挂载的Buff</returns>
    public Buff AttachBuff(BuffData data, int baseLayer = 1)
    {
        if (data == null)
        {
            GD.PrintErr($"尝试挂载空BuffData到 {Name}");
            return null;
        }

        Buff buff = new Buff(data, baseLayer);
        return AttachBuff(buff, baseLayer);
    }

    /// <summary>
    /// 移除Buff
    /// </summary>
    /// <param name="buff">要移除的Buff</param>
    /// <returns>是否成功移除</returns>
    public bool RemoveBuff(Buff buff)
    {
        if (buff == null) return false;

        if (_buffs.Remove(buff))
        {
            // 触发移除事件
            buff.OnRemove();
            OnBuffRemoved(buff);
            return true;
        }
        return false;
    }

    /// <summary>
    /// 通过名称移除Buff
    /// </summary>
    /// <param name="buffName">Buff名称</param>
    /// <returns>移除的Buff数量</returns>
    public int RemoveBuff(string buffName)
    {
        int count = 0;
        List<Buff> toRemove = new();

        foreach (var buff in _buffs)
        {
            if (buff.Data?.name == buffName)
            {
                toRemove.Add(buff);
            }
        }

        foreach (var buff in toRemove)
        {
            if (RemoveBuff(buff))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// 通过标签移除Buff
    /// </summary>
    /// <param name="tag">Buff标签</param>
    /// <returns>移除的Buff数量</returns>
    public int RemoveBuffByTag(BuffTag tag)
    {
        int count = 0;
        List<Buff> toRemove = new();

        foreach (var buff in _buffs)
        {
            if (buff.HasTag(tag))
            {
                toRemove.Add(buff);
            }
        }

        foreach (var buff in toRemove)
        {
            if (RemoveBuff(buff))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// 清除所有Buff
    /// </summary>
    public void ClearAllBuffs()
    {
        List<Buff> toRemove = new(_buffs);
        foreach (var buff in toRemove)
        {
            RemoveBuff(buff);
        }
    }

    /// <summary>
    /// 获取指定名称的Buff
    /// </summary>
    /// <param name="buffName">Buff名称</param>
    /// <returns>找到的Buff，不存在返回null</returns>
    public Buff GetBuff(string buffName)
    {
        foreach (var buff in _buffs)
        {
            if (buff.Data?.name == buffName)
            {
                return buff;
            }
        }
        return null;
    }

    /// <summary>
    /// 检查是否有指定名称的Buff
    /// </summary>
    /// <param name="buffName">Buff名称</param>
    /// <returns>是否存在</returns>
    public bool HasBuff(string buffName)
    {
        return GetBuff(buffName) != null;
    }

    /// <summary>
    /// 检查是否有指定标签的Buff
    /// </summary>
    /// <param name="tag">Buff标签</param>
    /// <returns>是否存在</returns>
    public bool HasBuffWithTag(BuffTag tag)
    {
        foreach (var buff in _buffs)
        {
            if (buff.HasTag(tag))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 获取指定标签的所有Buff
    /// </summary>
    /// <param name="tag">Buff标签</param>
    /// <returns>Buff列表</returns>
    public List<Buff> GetBuffsWithTag(BuffTag tag)
    {
        List<Buff> result = new();
        foreach (var buff in _buffs)
        {
            if (buff.HasTag(tag))
            {
                result.Add(buff);
            }
        }
        return result;
    }

    /// <summary>
    /// 改变Buff层数
    /// </summary>
    /// <param name="buff">目标Buff</param>
    /// <param name="delta">变化量（正数增加，负数减少）</param>
    /// <returns>实际变化的层数</returns>
    public int ChangeBuffStack(Buff buff, int delta)
    {
        if (buff == null) return 0;

        int oldStack = buff.CurrentStack;
        if (delta > 0)
        {
            return buff.AddStack(delta);
        }
        else if (delta < 0)
        {
            return buff.RemoveStack(-delta);
        }
        return 0;
    }

    /// <summary>
    /// 更新所有Buff
    /// </summary>
    /// <param name="delta">帧间隔时间</param>
    protected void UpdateBuffs(double delta)
    {
        List<Buff> toRemove = new();

        foreach (var buff in _buffs)
        {
            // 更新Buff时间
            if (buff.UpdateTime(delta))
            {
                toRemove.Add(buff);
            }
            
            // 调用Buff的Update
            buff.OnUpdate(delta);
        }

        // 移除过期的Buff
        foreach (var buff in toRemove)
        {
            RemoveBuff(buff);
        }
    }

    #endregion

    #region 伤害系统

    /// <summary>
    /// 受到伤害
    /// </summary>
    /// <param name="damage">伤害数据</param>
    /// <returns>实际受到的伤害值</returns>
    public virtual float Hurt(Damage damage)
    {
        if (damage == null || !IsAlive) return 0f;

        // 触发Buff的受伤事件
        foreach (var buff in _buffs)
        {
            buff.OnEntityHurt(damage);
        }

        // 计算实际伤害
        float rawDamage = damage.value.Get();
        float def = defense.Get();
        
        // 伤害公式：原始伤害 * (100 / (100 + 防御力))
        float damageReduction = 100f / (100f + def);
        float finalDamage = rawDamage * damageReduction;
        
        // 确保伤害不为负
        finalDamage = Mathf.Max(0f, finalDamage);

        // 应用伤害
        float oldHealth = currentHealth;
        float newHealth = Mathf.Max(0f, oldHealth - finalDamage);
        currentHealth = newHealth;

        // 触发受伤抽象方法
        OnHurt(damage, finalDamage);

        // 检查死亡
        if (newHealth <= 0 && oldHealth > 0)
        {
            Dead(damage, damage?.source);
        }

        return finalDamage;
    }

    /// <summary>
    /// 治疗
    /// </summary>
    /// <param name="amount">治疗量</param>
    /// <returns>实际治疗量</returns>
    public virtual float Heal(float amount)
    {
        if (!IsAlive || amount <= 0) return 0f;

        float oldHealth = currentHealth;
        float maxHp = maxHealth.Get();
        float newHealth = Mathf.Min(oldHealth + amount, maxHp);
        float actualHeal = newHealth - oldHealth;

        currentHealth = newHealth;
        OnHeal(actualHeal);

        return actualHeal;
    }

    #endregion

    #region 抽象方法（子类实现）

    /// <summary>
    /// 受伤时触发
    /// </summary>
    /// <param name="damage">原始伤害数据</param>
    /// <param name="finalDamage">最终伤害值</param>
    protected virtual void OnHurt(Damage damage, float finalDamage)
    {
        
    }

    /// <summary>
    /// 死亡时触发
    /// </summary>
    /// <param name="damage">致死伤害数据</param>
    protected virtual void OnDeath(Damage damage, Entity other)
    {
        
    }

    /// <summary>
    /// 治疗时触发
    /// </summary>
    /// <param name="amount">实际治疗量</param>
    protected virtual void OnHeal(float amount)
    {
    }

    /// <summary>
    /// Buff挂载时触发
    /// </summary>
    /// <param name="buff">挂载的Buff</param>
    protected virtual void OnBuffAttached(Buff buff)
    {
    }

    /// <summary>
    /// Buff移除时触发
    /// </summary>
    /// <param name="buff">移除的Buff</param>
    protected virtual void OnBuffRemoved(Buff buff)
    {
    }

    /// <summary>
    /// 初始化时触发
    /// </summary>
    protected virtual void OnStart()
    {
    }

    /// <summary>
    /// 进入场景树时触发（在Ready之前）
    /// 用于设置碰撞层等早期初始化
    /// </summary>
    protected virtual void OnEnterTree()
    {
    }

    /// <summary>
    /// 退出场景树时触发
    /// </summary>
    protected virtual void OnExitTree()
    {
    }

    /// <summary>
    /// 每帧更新（子类可重写）
    /// </summary>
    /// <param name="delta">帧间隔时间</param>
    protected virtual void OnUpdate(double delta)
    {
    }

    /// <summary>
    /// 物理更新（子类可重写）
    /// </summary>
    /// <param name="delta"></param>
    protected virtual void OnPhysicsUpdate(double delta)
    {
    }

    /// <summary>
    /// 进入碰撞时触发（与其他Entity碰撞开始）
    /// </summary>
    /// <param name="other">碰撞的Entity对象</param>
    protected virtual void OnCollisionEntered(Entity other)
    {
    }

    /// <summary>
    /// 碰撞中触发（每帧与其他Entity保持碰撞时）
    /// </summary>
    /// <param name="other">碰撞的Entity对象</param>
    protected virtual void OnCollisionStayed(Entity other)
    {
    }

    /// <summary>
    /// 离开碰撞时触发（与其他Entity碰撞结束）
    /// </summary>
    /// <param name="other">碰撞的Entity对象</param>
    protected virtual void OnCollisionExited(Entity other)
    {
    }

    /// <summary>
    /// 攻击时方法，自动处理攻速
    /// </summary>
    protected virtual void OnAttack()
    {
    }

    #endregion

    #region 生命周期

    public override void _EnterTree()
    {
        base._EnterTree();
        OnEnterTree();
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        OnExitTree();
    }

    public override void _Ready()
    {
        base._Ready();
        currentHealth = maxHealth.Get();

        //检查子节点
        foreach (var child in GetChildren())
        {
            //给Area子节点绑定碰撞事件
            if (child is Area2D a)
            {
                if(area != null)
                {
                    area = a;
                }
                break;
            }
            //初始化所有实体组件
            else if (child is EntityComponent component)
            {
                GD.Print("初始化实体组件: " + component.Name);
                component.Init(this);
                break;
            }
        }
        if(area != null)
        {
            area.BodyEntered += OnBodyEntered;
            area.BodyExited += OnBodyExited;
        }

        OnStart();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        
        // 更新Buff
        UpdateBuffs(delta);

        // 更新攻击计时器
        if (attackTimer <= 1f)
        {
            attackTimer += (float)delta * attackSpeed.Get();
        }
        
        // 子类更新
        OnUpdate(delta);
    }


    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        
        // 处理碰撞中的Entity
        foreach (var entity in _collidingEntities)
        {
            if (entity != null && entity.IsAlive)
            {
                OnCollisionStayed(entity);
            }
        }
        
        // 子类物理更新
        OnPhysicsUpdate(delta);
    }

    /// <summary>
    /// 碰撞体进入时触发（信号回调）
    /// </summary>
    /// <param name="body">碰撞到的物理体</param>
    private void OnBodyEntered(Node body)
    {
        if (body is Entity entity && entity != this)
        {
            _collidingEntities.Add(entity);
            OnCollisionEntered(entity);
        }
    }

    /// <summary>
    /// 碰撞体离开时触发（信号回调）
    /// </summary>
    /// <param name="body">离开的物理体</param>
    private void OnBodyExited(Node body)
    {
        if (body is Entity entity && entity != this)
        {
            _collidingEntities.Remove(entity);
            OnCollisionExited(entity);
        }
    }

    private void Dead(Damage damage, Entity other)
    {
        OnDeath(damage, other);
        QueueFree();
    }

    #endregion

    #region 辅助方法

    public void Attack()
    {
        if(attackTimer >= 1f)
        {
            OnAttack();
            attackTimer = 0;
        }
    }

    /// <summary>
    /// 重置实体状态（复活时使用）
    /// </summary>
    public virtual void Reset()
    {
        currentHealth = maxHealth.Get();
        ClearAllBuffs();
    }

    /// <summary>
    /// 杀死实体
    /// </summary>
    public void Kill()
    {
        currentHealth = 0;
        Dead(null, null);
    }

    public override string ToString()
    {
        return $"{GetType().Name}[{Name}] HP:{currentHealth:F0}/{maxHealth.Get():F0} Buffs:{_buffs.Count}";
    }

    #endregion
}

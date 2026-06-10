using Godot;
using Godot.Collections;
using System.Collections.Generic;

/// <summary>
/// 实体抽象基类 - 所有游戏实体的基础
/// 包含基础属性系统和Buff管理系统
/// 继承CharacterBody2D以支持物理移动
/// </summary>
public abstract partial class Entity : CharacterBody2D
{
    #region 基础属性（使用Value对象）

    /// <summary>
    /// 最大生命值
    /// </summary>
    [Export] public Value MaxHealth { get; protected set; } = new(100f);

    /// <summary>
    /// 当前生命值
    /// </summary>
    [Export] public float CurrentHealth { get; protected set; } = 100f;

    /// <summary>
    /// 攻击力
    /// </summary>
    [Export] public Value Attack { get; protected set; } = new(10f);

    /// <summary>
    /// 防御力
    /// </summary>
    [Export] public Value Defense { get; protected set; } = new(5f);

    /// <summary>
    /// 移动速度
    /// </summary>
    [Export] public Value MoveSpeed { get; protected set; } = new(200f);

    /// <summary>
    /// 攻击速度
    /// </summary>
    [Export] public Value AttackSpeed { get; protected set; } = new(1f);

    /// <summary>
    /// 攻击范围
    /// </summary>
    [Export] public Value AttackRange { get; protected set; } = new(100f);

    /// <summary>
    /// 暴击率（0-1）
    /// </summary>
    [Export] public Value CritRate { get; protected set; } = new(0f);

    /// <summary>
    /// 暴击伤害倍数
    /// </summary>
    [Export] public Value CritDamage { get; protected set; } = new(1.5f);

    #endregion

    #region 状态属性

    /// <summary>
    /// 实体是否存活
    /// </summary>
    public bool IsAlive => CurrentHealth > 0;

    /// <summary>
    /// 实体是否已死亡
    /// </summary>
    public bool IsDead => CurrentHealth <= 0;

    /// <summary>
    /// 生命值百分比（0-1）
    /// </summary>
    public float HealthPercent => MaxHealth.Get() > 0 ? CurrentHealth / MaxHealth.Get() : 0f;

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
        float defense = Defense.Get();
        
        // 伤害公式：原始伤害 * (100 / (100 + 防御力))
        float damageReduction = 100f / (100f + defense);
        float finalDamage = rawDamage * damageReduction;
        
        // 确保伤害不为负
        finalDamage = Mathf.Max(0f, finalDamage);

        // 应用伤害
        float oldHealth = CurrentHealth;
        float newHealth = Mathf.Max(0f, oldHealth - finalDamage);
        CurrentHealth = newHealth;

        // 触发受伤抽象方法
        OnHurt(damage, finalDamage);

        // 检查死亡
        if (newHealth <= 0 && oldHealth > 0)
        {
            OnDeath(damage);
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

        float oldHealth = CurrentHealth;
        float maxHealth = MaxHealth.Get();
        float newHealth = Mathf.Min(oldHealth + amount, maxHealth);
        float actualHeal = newHealth - oldHealth;

        CurrentHealth = newHealth;
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
    protected virtual void OnDeath(Damage damage)
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

    #endregion

    #region 生命周期

    public override void _Process(double delta)
    {
        base._Process(delta);
        
        // 更新Buff
        UpdateBuffs(delta);
        
        // 子类更新
        OnUpdate(delta);
    }

    /// <summary>
    /// 每帧更新（子类可重写）
    /// </summary>
    /// <param name="delta">帧间隔时间</param>
    protected virtual void OnUpdate(double delta)
    {
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 重置实体状态（复活时使用）
    /// </summary>
    public virtual void Reset()
    {
        CurrentHealth = MaxHealth.Get();
        ClearAllBuffs();
    }

    /// <summary>
    /// 杀死实体
    /// </summary>
    public void Kill()
    {
        if (IsAlive)
        {
            CurrentHealth = 0;
            OnDeath(null);
        }
    }

    public override string ToString()
    {
        return $"{GetType().Name}[{Name}] HP:{CurrentHealth:F0}/{MaxHealth.Get():F0} Buffs:{_buffs.Count}";
    }

    #endregion
}
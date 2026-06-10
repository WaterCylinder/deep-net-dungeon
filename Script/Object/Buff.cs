using Godot;
using Godot.Collections;
using System.Collections.Generic;

/// <summary>
/// Buff实例类 - 从BuffData加载属性，管理Buff状态和行为
/// </summary>
public partial class Buff : RefCounted
{
    #region 属性

    /// <summary>
    /// Buff数据源
    /// </summary>
    public BuffData Data { get; private set; }

    /// <summary>
    /// Buff拥有者（挂载的实体）
    /// </summary>
    public Entity Owner { get; set; }

    /// <summary>
    /// 当前层数
    /// </summary>
    public int CurrentStack { get; private set; }

    /// <summary>
    /// 基础层数（挂载时的初始层数）
    /// </summary>
    public int BaseStack { get; private set; }

    /// <summary>
    /// 最大层数
    /// </summary>
    public int MaxStack { get; set; } = 1;

    /// <summary>
    /// 剩余持续时间（秒）
    /// </summary>
    public float RemainingDuration { get; private set; }

    /// <summary>
    /// 总持续时间（秒）
    /// </summary>
    public float TotalDuration { get; private set; }

    /// <summary>
    /// 定时触发间隔（秒）
    /// </summary>
    public float ProcessTime { get; private set; }

    /// <summary>
    /// 定时触发计时器
    /// </summary>
    public float Timer { get; private set; }

    /// <summary>
    /// Buff是否显示（UI上）
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// 是否是唯一Buff（同类Buff只能存在一个）
    /// </summary>
    public bool IsUnique { get; set; } = false;

    /// <summary>
    /// Buff标签
    /// </summary>
    public Array<BuffTag> Tags { get; private set; } = new();

    #endregion

    #region Effect引用（从Data加载）

    private Effect _onStartEffect;
    private Effect _onUpdateEffect;
    private Effect _onTimerEffect;
    private Effect _onEndEffect;
    private Effect _onEntityHurtEffect;
    private Effect _onEntityDeathEffect;
    private Effect _onStackIncreaseEffect;
    private Effect _onStackDecreaseEffect;

    #endregion

    #region 构造函数

    /// <summary>
    /// 从BuffData创建Buff实例
    /// </summary>
    /// <param name="data">Buff数据源</param>
    /// <param name="baseStack">基础层数</param>
    public Buff(BuffData data, int baseStack = 1)
    {
        Data = data;
        BaseStack = baseStack;
        CurrentStack = baseStack;
        MaxStack = data.level > 0 ? data.level : 1;
        
        // 加载持续时间
        TotalDuration = data.duration;
        RemainingDuration = data.duration;
        
        // 加载定时触发间隔
        ProcessTime = data.processTime;
        Timer = 0f;
        
        // 加载显示和唯一性属性
        IsVisible = data.isVisible;
        IsUnique = data.isUnique;
        
        // 浅克隆标签列表
        Tags = new Array<BuffTag>(data.tags);
        
        // 加载Effect引用
        _onStartEffect = data.onStart;
        _onUpdateEffect = data.onUpdate;
        _onTimerEffect = data.onTimer;
        _onEndEffect = data.onEnd;
        _onEntityHurtEffect = data.onEntityHurt;
        _onEntityDeathEffect = data.onEntityDeath;
        _onStackIncreaseEffect = data.onStackIncrease;
        _onStackDecreaseEffect = data.onStackDecrease;
    }

    #endregion

    #region 行为方法

    /// <summary>
    /// Buff挂载时触发
    /// </summary>
    public void OnAttach()
    {
        _onStartEffect?.Run(Owner, this);
    }

    /// <summary>
    /// Buff消失时触发
    /// </summary>
    public void OnRemove()
    {
        _onEndEffect?.Run(Owner, this);
    }

    /// <summary>
    /// 定时触发
    /// </summary>
    public void OnTimer()
    {
        _onTimerEffect?.Run(Owner, this);
    }

    /// <summary>
    /// 每帧更新
    /// </summary>
    /// <param name="delta">帧间隔时间</param>
    public void OnUpdate(double delta)
    {
        _onUpdateEffect?.Run(Owner, this);
    }

    /// <summary>
    /// 实体受伤时触发
    /// </summary>
    /// <param name="damage">伤害数据</param>
    public void OnEntityHurt(Damage damage)
    {
        _onEntityHurtEffect?.Run(Owner, this, damage);
    }

    /// <summary>
    /// 实体死亡时触发
    /// </summary>
    public void OnEntityDeath()
    {
        _onEntityDeathEffect?.Run(Owner, this);
    }

    #endregion

    #region 层数管理

    /// <summary>
    /// 增加层数
    /// </summary>
    /// <param name="amount">增加数量</param>
    /// <returns>实际增加的层数</returns>
    public int AddStack(int amount = 1)
    {
        if (amount <= 0) return 0;
        
        int oldStack = CurrentStack;
        int newStack = Mathf.Min(CurrentStack + amount, MaxStack);
        
        if (newStack > oldStack)
        {
            CurrentStack = newStack;
            OnStackChanged(oldStack, newStack);
            return newStack - oldStack;
        }
        
        return 0;
    }

    /// <summary>
    /// 减少层数
    /// </summary>
    /// <param name="amount">减少数量</param>
    /// <returns>实际减少的层数</returns>
    public int RemoveStack(int amount = 1)
    {
        if (amount <= 0) return 0;
        
        int oldStack = CurrentStack;
        int newStack = Mathf.Max(CurrentStack - amount, 0);
        
        if (newStack < oldStack)
        {
            CurrentStack = newStack;
            OnStackChanged(oldStack, newStack);
            return oldStack - newStack;
        }
        
        return 0;
    }

    /// <summary>
    /// 设置层数
    /// </summary>
    /// <param name="stack">新层数</param>
    public void SetStack(int stack)
    {
        int oldStack = CurrentStack;
        CurrentStack = Mathf.Clamp(stack, 0, MaxStack);
        
        if (CurrentStack != oldStack)
        {
            OnStackChanged(oldStack, CurrentStack);
        }
    }

    /// <summary>
    /// 层数变化时触发
    /// </summary>
    private void OnStackChanged(int oldStack, int newStack)
    {
        if (newStack > oldStack)
        {
            // 层数增加
            OnStackIncrease(oldStack, newStack);
        }
        else if (newStack < oldStack)
        {
            // 层数减少
            OnStackDecrease(oldStack, newStack);
        }
    }

    /// <summary>
    /// 层数增加时触发
    /// </summary>
    private void OnStackIncrease(int oldStack, int newStack)
    {
        _onStackIncreaseEffect?.Run(Owner, this);
    }

    /// <summary>
    /// 层数减少时触发
    /// </summary>
    private void OnStackDecrease(int oldStack, int newStack)
    {
        _onStackDecreaseEffect?.Run(Owner, this);
    }

    #endregion

    #region 时间管理

    /// <summary>
    /// 更新Buff时间
    /// </summary>
    /// <param name="delta">帧间隔时间</param>
    /// <returns>是否已过期</returns>
    public bool UpdateTime(double delta)
    {
        // 更新定时触发器
        if (ProcessTime > 0)
        {
            Timer += (float)delta;
            while (Timer >= ProcessTime)
            {
                Timer -= ProcessTime;
                OnTimer();
            }
        }
        
        // 更新持续时间
        if (TotalDuration > 0)
        {
            RemainingDuration -= (float)delta;
            if (RemainingDuration <= 0)
            {
                return true; // 已过期
            }
        }
        
        return false; // 未过期
    }

    /// <summary>
    /// 重置持续时间
    /// </summary>
    public void RefreshDuration()
    {
        RemainingDuration = TotalDuration;
    }

    /// <summary>
    /// 增加持续时间
    /// </summary>
    /// <param name="amount">增加的秒数</param>
    public void AddDuration(float amount)
    {
        RemainingDuration += amount;
        TotalDuration += amount;
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 检查Buff是否应该移除（层数为0或时间到期）
    /// </summary>
    public bool ShouldRemove()
    {
        return CurrentStack <= 0 || (TotalDuration > 0 && RemainingDuration <= 0);
    }

    /// <summary>
    /// 检查是否有指定标签
    /// </summary>
    public bool HasTag(BuffTag tag)
    {
        return Tags.Contains(tag);
    }

    /// <summary>
    /// 获取进度百分比（用于UI显示）
    /// </summary>
    public float GetProgress()
    {
        if (TotalDuration <= 0) return 1f;
        return RemainingDuration / TotalDuration;
    }

    public override string ToString()
    {
        string name = Data?.name ?? "Unknown";
        return $"Buff[{name}] Stack:{CurrentStack}/{MaxStack} Duration:{RemainingDuration:F1}/{TotalDuration:F1}";
    }

    #endregion
}
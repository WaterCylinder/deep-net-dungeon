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
    public BuffData data;

    /// <summary>
    /// Buff拥有者（挂载的实体）
    /// </summary>
    public Entity owner;

    public string id;
    
    public string name;

    public Texture2D sprite;

    public string intro;

    /// <summary>
    /// 当前层数
    /// </summary>
    public int currentStack;

    /// <summary>
    /// 基础层数（挂载时的初始层数）
    /// </summary>
    public int baseStack;

    /// <summary>
    /// 最大层数
    /// </summary>
    public int maxStack = 1;

    /// <summary>
    /// 剩余持续时间（秒）
    /// </summary>
    public float remainingDuration;

    /// <summary>
    /// 总持续时间（秒）
    /// </summary>
    public float totalDuration;

    /// <summary>
    /// 定时触发间隔（秒）
    /// </summary>
    public float processTime;

    /// <summary>
    /// 定时触发计时器
    /// </summary>
    public float timer;

    /// <summary>
    /// Buff是否显示（UI上）
    /// </summary>
    public bool isVisible = true;

    /// <summary>
    /// 是否是唯一Buff（同类Buff只能存在一个）
    /// </summary>
    public bool isUnique = false;

    /// <summary>
    /// Buff标签
    /// </summary>
    public Array<BuffTag> tags = new();

    #endregion

    #region Effect

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
        this.data = data;
        this.baseStack = baseStack;
        currentStack = baseStack;
        maxStack = data.level > 0 ? data.level : 1;

        id = data.id;
        name = data.name;
        sprite = data.sprite;
        intro = data.intro;
        
        // 加载持续时间
        totalDuration = data.duration;
        remainingDuration = data.duration;
        
        // 加载定时触发间隔
        processTime = data.processTime;
        timer = 0f;
        
        // 加载显示和唯一性属性
        isVisible = data.isVisible;
        isUnique = data.isUnique;
        
        // 浅克隆标签列表
        tags = new Array<BuffTag>(data.tags);
        
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
        _onStartEffect?.Run(owner, this);
    }

    /// <summary>
    /// Buff消失时触发
    /// </summary>
    public void OnRemove()
    {
        _onEndEffect?.Run(owner, this);
    }

    /// <summary>
    /// 定时触发
    /// </summary>
    public void OnTimer()
    {
        _onTimerEffect?.Run(owner, this);
    }

    /// <summary>
    /// 每帧更新
    /// </summary>
    /// <param name="delta">帧间隔时间</param>
    public void OnUpdate(double delta)
    {
        _onUpdateEffect?.Run(owner, this);
    }

    /// <summary>
    /// 实体受伤时触发
    /// </summary>
    /// <param name="damage">伤害数据</param>
    public void OnEntityHurt(Damage damage)
    {
        _onEntityHurtEffect?.Run(owner, this, damage);
    }

    /// <summary>
    /// 实体死亡时触发
    /// </summary>
    public void OnEntityDeath()
    {
        _onEntityDeathEffect?.Run(owner, this);
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
        
        int oldStack = currentStack;
        int newStack = Mathf.Min(currentStack + amount, maxStack);
        
        if (newStack > oldStack)
        {
            currentStack = newStack;
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
        
        int oldStack = currentStack;
        int newStack = Mathf.Max(currentStack - amount, 0);
        
        if (newStack < oldStack)
        {
            currentStack = newStack;
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
        int oldStack = currentStack;
        currentStack = Mathf.Clamp(stack, 0, maxStack);
        
        if (currentStack != oldStack)
        {
            OnStackChanged(oldStack, currentStack);
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
        _onStackIncreaseEffect?.Run(owner, this);
    }

    /// <summary>
    /// 层数减少时触发
    /// </summary>
    private void OnStackDecrease(int oldStack, int newStack)
    {
        _onStackDecreaseEffect?.Run(owner, this);
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
        if (processTime > 0)
        {
            timer += (float)delta;
            while (timer >= processTime)
            {
                timer -= processTime;
                OnTimer();
            }
        }
        
        // 更新持续时间
        if (totalDuration > 0)
        {
            remainingDuration -= (float)delta;
            if (remainingDuration <= 0)
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
        remainingDuration = totalDuration;
    }

    /// <summary>
    /// 增加持续时间
    /// </summary>
    /// <param name="amount">增加的秒数</param>
    public void AddDuration(float amount)
    {
        remainingDuration += amount;
        totalDuration += amount;
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 检查Buff是否应该移除（层数为0或时间到期）
    /// </summary>
    public bool ShouldRemove()
    {
        return currentStack <= 0 || (totalDuration > 0 && remainingDuration <= 0);
    }

    /// <summary>
    /// 检查是否有指定标签
    /// </summary>
    public bool HasTag(BuffTag tag)
    {
        return tags.Contains(tag);
    }

    /// <summary>
    /// 获取进度百分比（用于UI显示）
    /// </summary>
    public float GetProgress()
    {
        if (totalDuration <= 0) return 1f;
        return remainingDuration / totalDuration;
    }

    public override string ToString()
    {
        string name = data?.id ?? "Unknown";
        return $"Buff[{name}] Stack:{currentStack}/{maxStack} Duration:{remainingDuration:F1}/{totalDuration:F1}";
    }

    #endregion
}
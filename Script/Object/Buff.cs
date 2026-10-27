using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;

public enum BuffTag
{
    None,
    Negative,
    Positive,
}

/// <summary>
/// Buff实例类 - 从BuffData加载属性，管理Buff状态和行为
/// </summary>
public partial class Buff : RefCounted
{
    #region 通用buff

    public static BuffData _fire;
    public static string _fireDataPath = "Resource.Buff.fire";
    public static BuffData Fire
    {
        get
        {
            if(_fire == null)
            {
                _fire = AssetManager.Instance.GetData<BuffData>(_fireDataPath, null);
                if(_fire == null)
                {
                    GD.Print($"[BUFF] 在路径{_fireDataPath}找不到BUFF数据");
                }
            }
            return _fire;
        }
    }

    public static BuffData _virus;
    public static string _virusDataPath = "Resource.Buff.virus";
    public static BuffData Virus
    {
        get
        {
            if(_virus == null)
            {
                _virus = AssetManager.Instance.GetData<BuffData>(_virusDataPath, null);
                if(_virus == null)
                {
                    GD.Print($"[BUFF] 在路径{_virusDataPath}找不到BUFF数据");
                }
            }
            return _virus;
        }
    }

    public static BuffData _collapse;
    public static string _collapseDataPath = "Resource.Buff.collapse";
    public static BuffData Collapse
    {
        get
        {
            if(_collapse == null)
            {
                _collapse = AssetManager.Instance.GetData<BuffData>(_collapseDataPath, null);
                if(_collapse == null)
                {
                    GD.Print($"[BUFF] 在路径{_collapseDataPath}找不到BUFF数据");
                }
            }
            return _collapse;
        }
    }

    public static BuffData _blood;
    public static string _bloodDataPath = "Resource.Buff.blood";
    public static BuffData Blood
    {
        get
        {
            if(_blood == null)
            {
                _blood = AssetManager.Instance.GetData<BuffData>(_bloodDataPath, null);
                if(_blood == null)
                {
                    GD.Print($"[BUFF] 在路径{_bloodDataPath}找不到BUFF数据");
                }
            }
            return _blood;
        }
    }

    public static BuffData _flintBullet;
    public static string _flintBulletDataPath = "Resource.v1.Item.Buff.Data.buff_flint_bullet";
    public static BuffData FlintBullet
    {
        get
        {
            if(_flintBullet == null)
            {
                _flintBullet = AssetManager.Instance.GetData<BuffData>(_flintBulletDataPath, null);
                if(_flintBullet == null)
                {
                    GD.Print($"[BUFF] 在路径{_flintBulletDataPath}找不到BUFF数据");
                }
            }
            return _flintBullet;
        }
    }

    public static BuffData _springboardBullet;
    public static string _springboardBulletDataPath = "Resource.v1.Item.Buff.Data.buff_springboard_bullet";
    public static BuffData SpringboardBullet
    {
        get
        {
            if(_springboardBullet == null)
            {
                _springboardBullet = AssetManager.Instance.GetData<BuffData>(_springboardBulletDataPath, null);
                if(_springboardBullet == null)
                {
                    GD.Print($"[BUFF] 在路径{_springboardBulletDataPath}找不到BUFF数据");
                }
            }
            return _springboardBullet;
        }
    }

    #endregion

    #region 属性

    /// <summary>
    /// Buff数据源
    /// </summary>
    public BuffData data;

    /// <summary>
    /// Buff拥有者（挂载的实体）
    /// </summary>
    public Entity owner;

    /// <summary>
    /// BuffID（来自BuffData，唯一标识）
    /// </summary>
    [Export] public string id;

    /// <summary>
    /// Buff显示名称
    /// </summary>
    [Export] public string name;

    [Export] public Texture2D sprite;

    [Export] public string intro;

    /// <summary>
    /// 当前层数
    /// </summary>
    [Export] public int currentStack;

    /// <summary>
    /// 基础层数（挂载时的初始层数）
    /// </summary>
    [Export] public int baseStack;

    /// <summary>
    /// 最大层数
    /// </summary>
    [Export] public int maxStack = 1;

    /// <summary>
    /// 剩余持续时间（秒）
    /// </summary>
    [Export] public float remainingDuration;

    /// <summary>
    /// 总持续时间（秒）
    /// </summary>
    [Export] public float totalDuration;

    /// <summary>
    /// 定时触发间隔（秒）
    /// </summary>
    [Export] public float processTime;

    /// <summary>
    /// 定时触发计时器
    /// </summary>
    [Export] public float timer;

    /// <summary>
    /// Buff是否显示（UI上）
    /// </summary>
    [Export] public bool isVisible = true;

    /// <summary>
    /// 是否是唯一Buff（同类Buff只能存在一个）
    /// </summary>
    [Export] public bool isUnique = false;

    /// <summary>
    /// 重复挂载同类Buff时是否叠加层数（仅在 isUnique 时生效）
    /// </summary>
    [Export] public bool isAdded = false;

    /// <summary>
    /// Buff标签
    /// </summary>
    [Export] public Array<BuffTag> tags = new();

    [Export] public Args args = new();

    #endregion

    #region Effect

    public Effect onStartEffect;
    public Effect onUpdateEffect;
    public Effect onTimerEffect;
    public Effect onEndEffect;
    public Effect onEntityHurtEffect;
    public Effect onEntityDeathEffect;
    public Effect onStackIncreaseEffect;
    public Effect onStackDecreaseEffect;

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
        maxStack = data.maxStack != 0 ? data.maxStack : 1;
        if(maxStack < 0)
        {
            maxStack = int.MaxValue;
        }

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
        isAdded = data.isAdded;
        
        // 浅克隆标签列表
        tags = [.. data.tags];

        //参数
        args = new();
        args.Init(data.args);
        
        // 加载Effect引用
        onStartEffect = data.onStart;
        onUpdateEffect = data.onUpdate;
        onTimerEffect = data.onTimer;
        onEndEffect = data.onEnd;
        onEntityHurtEffect = data.onEntityHurt;
        onEntityDeathEffect = data.onEntityDeath;
        onStackIncreaseEffect = data.onStackIncrease;
        onStackDecreaseEffect = data.onStackDecrease;
    }

    #endregion

    #region 行为方法

    /// <summary>
    /// Buff挂载时触发
    /// </summary>
    public void OnAttach()
    {
        onStartEffect?.Run(owner, this);
    }

    /// <summary>
    /// Buff消失时触发
    /// </summary>
    public void OnRemove()
    {
        onEndEffect?.Run(owner, this);
    }

    /// <summary>
    /// 定时触发
    /// </summary>
    public void OnTimer()
    {
        onTimerEffect?.Run(owner, this);
    }

    /// <summary>
    /// 每帧更新
    /// </summary>
    /// <param name="delta">帧间隔时间</param>
    public void OnUpdate(double delta)
    {
        onUpdateEffect?.Run(owner, this);
    }

    /// <summary>
    /// 实体受伤时触发
    /// </summary>
    /// <param name="damage">伤害数据</param>
    public void OnEntityHurt(Damage damage)
    {
        onEntityHurtEffect?.Run(owner, this, damage);
    }

    /// <summary>
    /// 实体死亡时触发
    /// </summary>
    public void OnEntityDeath()
    {
        onEntityDeathEffect?.Run(owner, this);
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

        // 通知宿主：层数变了，让订阅了OnBuffsChanged的UI等刷新
        if (GodotObject.IsInstanceValid(owner))
        {
            owner.NotifyBuffsChanged(this);
        }
    }

    /// <summary>
    /// 层数增加时触发
    /// </summary>
    private void OnStackIncrease(int oldStack, int newStack)
    {
        onStackIncreaseEffect?.Run(owner, this);
    }

    /// <summary>
    /// 层数减少时触发
    /// </summary>
    private void OnStackDecrease(int oldStack, int newStack)
    {
        onStackDecreaseEffect?.Run(owner, this);
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
        string buffId = data?.id ?? "Unknown";
        return $"Buff[{buffId}] Stack:{currentStack}/{maxStack} Duration:{remainingDuration:F1}/{totalDuration:F1}";
    }

    #endregion
}
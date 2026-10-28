using Godot;
using Godot.Collections;
using System;

public enum EntityTag
{
    Player,
    Enemy,
    Bullet,
    Obstacle,
    Energy,
}

/// <summary>
/// 实体抽象基类 - 所有游戏实体的基础
/// 包含基础属性系统和Buff管理系统
/// 继承CharacterBody2D以支持物理移动
/// </summary>
public abstract partial class Entity : RigidBody2D
{
    public const float BASE_ATTACK_SPEED_FACTOR = 5;

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

    /// <summary>
    /// 护甲值
    /// </summary>
    [Export] public float shield = 0;


    /// <summary>
    /// 可变参数
    /// </summary>
    [Export] public Args args = new Args();

    /// <summary>
    /// 实体组件
    /// </summary>
    [Export] public Array<EntityComponent> components = new();
    
    /// <summary>
    /// 音频播放器
    /// </summary>
    [Export] public AudioStreamPlayer2D audioPlayer;
    /// <summary>
    /// 专用弹幕音频播放器
    /// </summary>
    [Export] public AudioStreamPlayer2D bulletAudioPlayer;

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
    /// 当前实体所在的房间
    /// </summary>
    public Room room;

    /// <summary>
    /// 攻速计时器
    /// </summary>
    float attackTimer = 0f;

    /// <summary>
    /// 传送位置，hasPendingTrans为true时在IntegrateForces中重置位置
    /// 注意：不能用Vector2.Zero当哨兵，否则传送到世界原点(0,0)会被静默丢弃
    /// </summary>
    Vector2 transPos = Vector2.Zero;

    /// <summary>
    /// 是否有待处理的传送
    /// </summary>
    bool hasPendingTrans = false;

    /// <summary>
    /// 旋转目标角度，Mathf.Inf表示未设置
    /// </summary>
    float rotateTarget = Mathf.Inf;

    /// <summary>
    /// 临时计时器，用于处理buff等
    /// </summary>
    [Export] Godot.Collections.Dictionary<string, Timer> tempTimerList = new();

    /// <summary>
    /// 当前弹幕
    /// </summary>
    [Export] public BulletData bulletData;

    /// <summary>
    /// 最新的弹幕
    /// </summary>
    public Bullet latestBullet;

    /// <summary>
    /// 是否在死亡时立刻移除
    /// </summary>
    public bool queueFreeOnDeath = true;

    /// <summary>
    /// 是否在状态上已死亡
    /// </summary>
    public bool isDead = false;

    #endregion

    #region 控制属性

    public bool shieldOverLoad = false;

    #endregion
    
    public Game game => GameManager.Instance.game;

    #region 信号

    /// <summary>
    /// 传送信号
    /// </summary>
    /// <param name="pos"></param>
    [Signal] public delegate void OnTransEventHandler(Vector2 pos);
    /// <summary>
    /// 攻击信号
    /// </summary>
    [Signal] public delegate void OnAttackSignalEventHandler(Entity entity);
    /// <summary>
    /// 受伤信号
    /// </summary>
    /// <param name="damageValue"></param>
    [Signal] public delegate void OnHurtSignalEventHandler(Entity entity, float damageValue);
    /// <summary>
    /// 添加buff信号
    /// </summary>
    /// <param name="buff"></param>
    [Signal] public delegate void OnAttachBuffSignalEventHandler(Entity entity, Buff buff);

    /// <summary>
    /// Buff列表或层数发生变化时发出：
    /// 挂载新Buff、唯一Buff叠加层数/刷新时间、层数增减、Buff被移除。
    /// </summary>
    /// <param name="entity">Buff所属实体</param>
    /// <param name="buff">发生变化的那一个Buff</param>
    [Signal] public delegate void OnBuffsChangedSignalEventHandler(Entity entity, Buff buff);

    /// <summary>
    /// 碰撞点信号
    /// </summary>
    /// <param name="posList"></param>
    /// <param name="normalList"></param>
    [Signal] public delegate void OnContactEventHandler(Array<Vector2> posList, Array<Vector2> normalList);
    /// <summary>
    /// 发射弹幕时
    /// </summary>
    /// <param name="bullet"></param>
    [Signal] public delegate void OnShotEventHandler(Entity entity, Bullet bullet);
    /// <summary>
    /// 死亡时信号
    /// </summary>
    [Signal] public delegate void OnDeathSignalEventHandler(Entity entity, Damage damage);

    /// <summary>
    /// 拾取能量点时信号
    /// </summary>
    [Signal] public delegate void OnPickEnergyEventHandler(Entity entity, EnergyPoint energyPoint);

    /// <summary>
    /// 护盾消失时信号
    /// </summary>
    [Signal] public delegate void OnShieldBrokenSignalEventHandler(Entity entity);

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
    /// 真实速度：按物理帧之间的实际位移算出来的速度
    /// （velocity是"想往哪走"，贴着墙走不动时它仍是非零，而真实速度会接近0）
    /// </summary>
    protected Vector2 realVelocity = Vector2.Zero;
    Vector2 lastRealVelocityPosition = Vector2.Zero;
    bool hasLastRealVelocityPosition = false;
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
    /// 向指定方向移动，平滑移动请帧调用。可用于设置方向，那之后帧级调用Move朝向指定方向移动。
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
        Vector2 toward = target - GlobalPosition;
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

    /// <summary>
    /// 获取真实速度：按物理帧之间的位移计算。
    /// 敌怪对着墙走、被挡住时 velocity 仍是 moveTowards*speed，但真实速度接近0，
    /// 弹幕预判用这个才不会对着墙打歪
    /// </summary>
    public Vector2 GetRealVelocity()
    {
        return realVelocity;
    }

    /// <summary>
    /// 实体传送到指定位置
    /// </summary>
    /// <param name="pos"></param>
    public void TransTo(Vector2 pos)
    {
        transPos = pos;
        hasPendingTrans = true;
        //传送是瞬移，别把这段位移算进真实速度
        hasLastRealVelocityPosition = false;
        realVelocity = Vector2.Zero;
        EmitSignal(nameof(OnTrans), pos);
    }

    /// <summary>
    /// 实体旋转到指定角度
    /// </summary>
    /// <param name="target"></param>
    public void RotateTo(float target)
    {
        rotateTarget = target;
    }

    public Vector2 GetVelocity()
    {
        return velocity;
    }

    public Vector2 GetToward()
    {
        return GetVelocity().Normalized();
    }

    #endregion

    #region 碰撞系统

    /// <summary>
    /// 当前碰撞中的Entity集合
    /// </summary>
    private System.Collections.Generic.HashSet<Entity> _collidingEntities = new();

    #endregion

    #region Buff系统

    /// <summary>
    /// 当前挂载的所有Buff
    /// </summary>
    [Export] public Array<Buff> buffs = new();

    /// <summary>
    /// 挂载Buff
    /// </summary>
    /// <param name="buff">Buff实例</param>
    /// <param name="stack">层数。传null（默认）表示使用该实例自身的基础层数 buff.baseStack</param>
    /// <returns>挂载后的Buff（可能已存在并被叠加）；层数非法时返回null且不挂载</returns>
    /// <remarks>同类Buff已存在时的处理方式由 BuffData.isAdded 决定：叠加层数或只重置持续时间（仅对 isUnique 的Buff生效）</remarks>
    public Buff AttachBuff(Buff buff, int? stack = null)
    {
        if (buff == null || buff.data == null)
        {
            GD.PrintErr($"尝试挂载空Buff到 {Name}");
            return null;
        }

        int? finalStack = ValidateStack(stack ?? buff.baseStack, buff.data);
        if (!finalStack.HasValue)
        {
            return null;
        }

        // 唯一Buff：同类只保留一个实例，按 isAdded 决定叠加层数还是只刷新持续时间
        if (buff.isUnique)
        {
            Buff existingBuff = GetBuff(buff.data.id);
            if (existingBuff != null)
            {
                if (buff.isAdded)
                {
                    // 叠层：在已有Buff上增加层数（上限由BuffData.level决定，达到上限时不再增加）
                    // 层数真的变了的话AddStack内部已经通知过变化，这里不用再发
                    existingBuff.AddStack(finalStack.Value);
                }
                else
                {
                    existingBuff.RefreshDuration();
                    //只刷新了时间，列表没变但UI的剩余时间显示要更新
                    NotifyBuffsChanged(existingBuff);
                }
                return existingBuff;
            }
        }

        // 设置owner并添加
        //先入列表再设层数：SetStack可能触发层数变化通知，此时Buff应该已经属于本实体
        buff.owner = this;
        buffs.Add(buff);
        buff.SetStack(finalStack.Value);
        
        // 触发挂载事件
        buff.OnAttach();

        OnBuffAttached(buff);
        EmitSignal(nameof(OnAttachBuffSignal), this, buff);
        NotifyBuffsChanged(buff);
        
        return buff;
    }

    /// <summary>
    /// 通过BuffData挂载Buff
    /// </summary>
    /// <param name="data">Buff数据</param>
    /// <param name="stack">层数。传null（默认）表示使用 BuffData.baseStack</param>
    /// <returns>挂载的Buff；层数非法时返回null且不挂载</returns>
    /// <remarks>同类Buff已存在时的处理方式由 BuffData.isAdded 决定（仅对 isUnique 的Buff生效）</remarks>
    public Buff AttachBuff(BuffData data, int? stack = null)
    {
        if (data == null)
        {
            GD.PrintErr($"尝试挂载空BuffData到 {Name}");
            return null;
        }

        int? finalStack = ValidateStack(stack ?? data.baseStack, data);
        if (!finalStack.HasValue)
        {
            return null;
        }

        Buff buff = new Buff(data, finalStack.Value);
        return AttachBuff(buff, finalStack.Value);
    }

    /// <summary>
    /// 校验挂载层数：层数必须大于0，否则视为非法并打印告警。
    /// 层数归零在Buff系统里表示"该移除"（Buff.ShouldRemove），因此不能作为挂载层数使用；
    /// 显式传入非法层数时不做静默回退，避免把调用方的意图悄悄改写成"用默认层数"
    /// </summary>
    /// <param name="stack">待校验的层数</param>
    /// <param name="data">用于告警信息的Buff数据</param>
    /// <returns>合法层数；非法时返回null（表示本次不挂载）</returns>
    private static int? ValidateStack(int stack, BuffData data)
    {
        if (stack > 0)
        {
            return stack;
        }

        string buffName = data == null
            ? "null"
            : (string.IsNullOrEmpty(data.id) ? data.name : data.id);
        GD.PushWarning($"[Buff] 挂载层数 {stack} 非法（Buff: {buffName}），本次不挂载");
        return null;
    }

    /// <summary>
    /// 移除Buff
    /// </summary>
    /// <param name="buff">要移除的Buff</param>
    /// <returns>是否成功移除</returns>
    public bool RemoveBuff(Buff buff)
    {
        if (buff == null) return false;

        if (buffs.Remove(buff))
        {
            // 触发移除事件
            buff.OnRemove();
            OnBuffRemoved(buff);
            NotifyBuffsChanged(buff);
            return true;
        }
        return false;
    }

    /// <summary>
    /// 通过名称移除Buff
    /// </summary>
    /// <param name="buffId">BuffID（唯一标识）</param>
    /// <returns>移除的Buff数量</returns>
    public int RemoveBuff(string buffId)
    {
        int count = 0;
        System.Collections.Generic.List<Buff> toRemove = new();

        foreach (var buff in buffs)
        {
            if (buff?.data != null && buff.data.id == buffId)
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
        System.Collections.Generic.List<Buff> toRemove = new();

        foreach (var buff in buffs)
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
        System.Collections.Generic.List<Buff> toRemove = new(buffs);
        foreach (var buff in toRemove)
        {
            RemoveBuff(buff);
        }
    }

    /// <summary>
    /// 获取指定名称的Buff
    /// </summary>
    /// <param name="buffId">BuffID（唯一标识）</param>
    /// <returns>找到的Buff，不存在返回null</returns>
    public Buff GetBuff(string buffId)
    {
        foreach (var buff in buffs)
        {
            if (buff?.data != null && buff.data.id == buffId)
            {
                return buff;
            }
        }
        return null;
    }

    /// <summary>
    /// 检查是否有指定名称的Buff
    /// </summary>
    /// <param name="buffId">BuffID（唯一标识）</param>
    /// <returns>是否存在</returns>
    public bool HasBuff(string buffId)
    {
        return GetBuff(buffId) != null;
    }

    /// <summary>
    /// 检查是否有指定标签的Buff
    /// </summary>
    /// <param name="tag">Buff标签</param>
    /// <returns>是否存在</returns>
    public bool HasBuffWithTag(BuffTag tag)
    {
        foreach (var buff in buffs)
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
    public System.Collections.Generic.List<Buff> GetBuffsWithTag(BuffTag tag)
    {
        System.Collections.Generic.List<Buff> result = new();
        foreach (var buff in buffs)
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

        int oldStack = buff.currentStack;
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
    /// 广播"Buff发生变化"（挂载/叠层/层数增减/移除/刷新时间）。
    ///
    /// 通知入口统一放在Buff侧（Buff.OnStackChanged直接调用这里），
    /// 因为层数可以被Buff自己改（例如行为里直接buff.RemoveStack(1)），
    /// 只在Entity的接口里发信号会漏掉这种调用
    /// </summary>
    /// <param name="buff">发生变化的Buff</param>
    public void NotifyBuffsChanged(Buff buff)
    {
        EmitSignal(SignalName.OnBuffsChangedSignal, this, buff);
    }

    /// <summary>
    /// 更新所有Buff
    /// </summary>
    /// <param name="delta">帧间隔时间</param>
    protected void UpdateBuffs(double delta)
    {
        System.Collections.Generic.List<Buff> toRemove = new();

        //用快照遍历：Buff的计时器/更新行为里可能会挂上或摘掉Buff，
        //最典型的是灼烧、病毒这类"自己把自己打死"的dot——死亡时Dead()会调用ClearAllBuffs()把buffs清空，
        //这时候如果还在foreach live数组，枚举器的索引就会越界（ArgumentOutOfRangeException）
        System.Collections.Generic.List<Buff> snapshot = new(buffs);

        foreach (var buff in snapshot)
        {
            if (buff == null)
            {
                continue;
            }

            //遍历过程中已经被移除的Buff（层数归零、死亡清空等）不再更新
            if (!buffs.Contains(buff))
            {
                continue;
            }

            // 更新Buff时间
            buff.UpdateTime(delta);
            
            // 调用Buff的Update
            buff.OnUpdate(delta);

            // 层数归零或时间到期则移除
            if (buff.ShouldRemove())
            {
                toRemove.Add(buff);
            }
        }

        // 移除失效的Buff
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
        foreach (var buff in buffs)
        {
            buff.OnEntityHurt(damage);
        }

        bool isCrit = false;

        // 计算实际伤害
        float rawDamage = damage.value;
        if (damage.critRate > 0 && game.randomSystem.Float(0, 1) < damage.critRate)
        {
            rawDamage *= damage.critDamage;
            isCrit = true;
        }

        if (!damage.CheckTag(DamageTag.IgnoreShield))
        {
            // 伤害先计算到护盾上, 溢出的伤害减半应用到血量
            if(shield > 0)
            {
                float shieldTemp = shield;
                shield -= rawDamage;
                OnShieldHurt(damage, rawDamage);
                rawDamage -= shieldTemp;
                if(shield < 0)
                {
                    shield = 0;
                    rawDamage /= 2;
                    OnShieldBroken(damage, rawDamage);
                }
            }
            // 护盾后计算一次伤害如果为0则不进行下面计算
            if(rawDamage <= 0)
            {
                return 0;
            }
        }

        float finalDamage = rawDamage;

        if (!damage.CheckTag(DamageTag.IgnoreDefense))
        {
            float def = defense.Get();
        
            // 伤害公式：原始伤害 * (100 / (100 + 防御力)) - (防御力 / 10)
            float damageReduction = 100f / (100f + def);
            finalDamage = rawDamage * damageReduction - Mathf.Min(def * 0.1f, 10);
        }    
    
        // 确保伤害不为负
        finalDamage = Mathf.Max(0f, finalDamage);

        // 应用伤害
        float oldHealth = currentHealth;
        float newHealth = Mathf.Max(0f, oldHealth - finalDamage);
        currentHealth = newHealth;

        //显示伤害文本
        Color color = Colors.White;
        if (damage.CheckTag(DamageTag.Fire))
        {
            color = Colors.Orange;
        }
        else if (damage.CheckTag(DamageTag.Virus))
        {
            color = Colors.Red;
        }
        else if (damage.CheckTag(DamageTag.Collapse))
        {
            color = Colors.Purple;
        }
        else if (damage.CheckTag(DamageTag.Blood))
        {
            color = new Color(0.75f, 0.12f, 0.18f);
        }
        SmallText st = SmallText.CreateSmallText(SmallTextType.QuickShow, GlobalPosition, "" + finalDamage.ToString("0"), color);
        if (isCrit)
        {
            st.Scale = new Vector2(1.4f,1.4f);
        }
        // 触发受伤抽象方法
        OnHurt(damage, finalDamage);

        EmitSignal(nameof(OnHurtSignal), this, finalDamage);

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
        game.CreateParticles("blood", GlobalPosition);
    }

    /// <summary>
    /// 护盾受击时
    /// </summary>
    /// <param name="damage"></param>
    /// <param name="finalDamage"></param>
    protected virtual void OnShieldHurt(Damage damage, float shieldDamage)
    {
        game.CreateParticles("shield_hurt", GlobalPosition);
    }

    /// <summary>
    /// 护盾破碎后
    /// </summary>
    protected virtual void OnShieldBroken(Damage damage, float shieldDamage)
    {
        EmitSignal(SignalName.OnShieldBrokenSignal, this);
        game.CreateParticles("shield_broken", GlobalPosition, this.Scale.X);
    }


    /// <summary>
    /// 死亡时触发
    /// </summary>
    /// <param name="damage">致死伤害数据</param>
    protected virtual void OnDeath(Damage damage, Entity other)
    {
        EmitSignal(SignalName.OnDeathSignal, this, damage);
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
        EmitSignal(SignalName.OnAttackSignal, this);
    }

    #endregion

    #region 生命周期

    public override void _EnterTree()
    {
        base._EnterTree();

        // [Export]的Args在PackedScene里是共享的SubResource：同一个预制体的所有实例
        // 默认指向同一个Args对象，运行期写入的参数（行为状态机、事件标记等）会互相覆盖。
        // 这里给每个实体实例复制一份独立参数，保证参数是"每实例"的。
        args = args != null ? args.Copy() : new Args();

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
                area ??= a;
                continue;
            }
            //初始化所有实体组件
            else if (child is EntityComponent component)
            {
                component.Init(this);
                components.Add(component); 
                continue;
            }
        }
        if(area != null)
        {
            area.BodyEntered += OnBodyEntered;
            area.BodyExited += OnBodyExited;
        }

        //设置音频组件
        if(audioPlayer == null)
        {
            AudioStreamPlayer2D player = new();
            AddChild(player);
            audioPlayer = player;
        }
        if(bulletAudioPlayer == null)
        {
            AudioStreamPlayer2D player = new();
            AddChild(player);
            bulletAudioPlayer = player;
        }


        OnStart();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (isDead)
        {   
            //死亡时停止帧更新
            MoveStop();
            return;
        }
        
        // 更新Buff
        UpdateBuffs(delta);

        //Buff的持续伤害可能就在这一帧把实体打死：这时不能再跑OnUpdate，
        //否则行为里的animNode.Play("idle"/"move")会把刚播上的死亡动画顶掉，
        //死亡动画的AnimationFinished永远不会触发，尸体就会一直卡在待机/移动动画上
        if (isDead)
        {
            MoveStop();
            return;
        }

        // 更新攻击计时器
        if (attackTimer <= 1f)
        {
            attackTimer += (float)delta * attackSpeed.Get() * BASE_ATTACK_SPEED_FACTOR;
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

        // 真实速度：用物理帧之间的位移计算（贴墙时velocity非零但位移≈0）
        if (hasLastRealVelocityPosition && delta > 0.0)
        {
            realVelocity = (GlobalPosition - lastRealVelocityPosition) / (float)delta;
        }
        lastRealVelocityPosition = GlobalPosition;
        hasLastRealVelocityPosition = true;
    }

    /// <summary>
    /// 物理引擎整合回调方法，在这里同步修改刚体的Transforms属性
    /// </summary>
    /// <param name="state"></param>
    public override void _IntegrateForces(PhysicsDirectBodyState2D state)
    {
        base._IntegrateForces(state);

        if (isDead)
        {
            //死亡则不进行特殊transform处理
            return;
        }

        //传送
        if(hasPendingTrans)
        {
            Transform2D t = state.Transform;
            t.Origin = transPos;
            state.Transform = t;
            hasPendingTrans = false;
        }

        //旋转
        if(!Mathf.IsInf(rotateTarget))
        {
            Transform2D t = state.Transform;
            t = t.RotatedLocal(rotateTarget - t.Rotation);
            state.Transform = t;
            rotateTarget = Mathf.Inf;
        }

        //碰撞点
        int contactCount = state.GetContactCount();
        if(state.GetContactCount() > 0)
        {
            Array<Vector2> contactPosition = new();
            Array<Vector2> contactNormal = new();

            for(int i = 0; i < contactCount; i++)
            {
                contactPosition.Add(state.GetContactLocalPosition(i));
                contactNormal.Add(state.GetContactLocalNormal(i));
            }

            EmitSignal(nameof(OnContact), contactPosition, contactNormal);
        }
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
        isDead = true;

        // 先让每个Buff响应死亡（onEntityDeath），再统一移除所有Buff。
        // 死亡后_Process会提前return，Buff不会再被推进，所以必须在这里清理：
        // 否则onEnd（退订事件、移除数值乘区、取消订阅）不会执行，
        // 尤其是复用实体（对象池/复活）时会把事件订阅和数值加成残留到下一次使用
        // 用快照遍历：onEntityDeath的Effect里可能会反过来增删Buff，直接遍历buffs会破坏枚举
        System.Collections.Generic.List<Buff> deathBuffs = new(buffs);
        foreach (var buff in deathBuffs)
        {
            if (buff == null)
            {
                continue;
            }
            buff.OnEntityDeath();
        }
        ClearAllBuffs();

        OnDeath(damage, other);
        
        if(queueFreeOnDeath)
        {
            QueueFree();
        }
        else
        {
            area.SetDeferred("monitorable", false);
            area.SetDeferred("monitoring", false);
        }
    }

    #endregion
    
    #region 计时器功能

    /// <summary>
    /// 添加计时器，无法添加同名计时器。
    /// </summary>
    /// <param name="label"></param>
    /// <param name="duration"></param>
    /// <param name="action"></param>
    /// <returns></returns>
    public bool AddTempTimer(string label, float duration, Action action)
    {
        if(tempTimerList.ContainsKey(label))
        {
            return false;
        }
        Timer timer = new Timer();
        timer.Timeout += () => {
            action?.Invoke();
            tempTimerList.Remove(label);
            timer.QueueFree();
        };
        AddChild(timer);
        timer.Start(duration);
        tempTimerList.Add(label, timer);
        return true;
    }

    /// <summary>
    /// 清除所有计时器
    /// </summary>
    public void ClearAllTimer()
    {
        foreach (Node child in GetChildren())
        {
            if (child is Timer && IsInstanceValid(child))
            {
                child.QueueFree();
            }
        }
    }

    #endregion

    #region 弹幕融合

    /// <summary>
    /// 修改当前弹幕数据，如果已有则对当前弹幕数据进行融合或覆盖
    /// </summary>
    /// <param name="newBulletData"></param>
    public void ChangeBullet(BulletData newBulletData)
    {
        if(newBulletData == null)
            return;
        bulletData = newBulletData;
    }

    /// <summary>
    /// 射击当前弹幕，在Attack里调用
    /// </summary>
    /// <param name="toward"></param>
    /// <param name="pos"></param>
    public void Shot(Vector2 toward, Vector2 pos = default, BulletData _data = null)
    {
        if(_data == null)
            _data = bulletData;
        if(_data == null)
            return;
        if(pos == default)
        {
            pos = GlobalPosition;
        }
        //GD.Print($"{Name}射击弹幕{_data.bulletName}");
        Bullet but = Bullet.Create(
            this,
            pos,
            toward,
            _data
        );
        EmitSignal(SignalName.OnShot, this, but);
        latestBullet = but;
    }


    #endregion

    #region 音频

    public void PlaySound(AudioStream sound, AudioStreamPlayer2D player = null, string bus = "Sound")
    {
        if(sound == null)return;
        if(player == null)player = audioPlayer;

        //播放器可能已经跟着它的实体一起被释放了：子弹挂在场景根上、比发射者活得久，
        //切房间/发射者死亡后子弹的 OnStart 仍会来播射击音效，这时访问 disposed 的
        //GodotObject 会抛 ObjectDisposedException。这里跳过这次播放即可，不该让整帧报错
        try
        {
            player.Stream = sound;
            player.Bus = bus;
            player.Play();
        }
        catch(ObjectDisposedException)
        {
        }
    }

    public void PlaySoundGlobal(AudioStream sound)
    {
        if(sound == null)return;
        Game game = GameManager.Instance.game;
        game.PlaySound(sound);
    }

    #endregion

    #region 动画

    /// <summary>
    /// 播放动画，需指定动画节点，默认播放“idle”
    /// </summary>
    /// <param name="animeName"></param>
    /// <param name="animeNode"></param>
    public void PlayAnime(string animeName, AnimatedSprite2D animeNode)
    {
        if (animeNode.SpriteFrames.HasAnimation(animeName))
        {
            animeNode.Play(animeName);
        }
        else
        {
            if (animeNode.SpriteFrames.HasAnimation("idle"))
            {
                animeNode.Play("idle");
            }
        }
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
    /// 无视攻击速度强制攻击
    /// </summary>
    /// <param name="resetAttackTime"></param>
    public void ForceAttack(bool resetAttackTime = false)
    {
        OnAttack();
        if (resetAttackTime)
        {
            attackTimer = 0;
        }
    }

    /// <summary>
    /// 一般的添加护盾方法，一般情况此方法获得的护盾无法大于最大生命值
    /// </summary>
    /// <param name="num"></param>
    public void AddShield(float num)
    {
        if (shieldOverLoad)
        {
            shield += num;
        }
        else
        {
            float diff = maxHealth.Get() - shield;
            diff = Mathf.Max(0, diff);
            shield += Mathf.Min(diff, num);
        }
    }

    public T GetComponent<T>() where T : EntityComponent
    {
        foreach(var c in components)
        {
            if(c is T result)
            {
                return result;
            }
        }
        return null;
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
        return $"{GetType().Name}[{Name}] HP:{currentHealth:F0}/{maxHealth.Get():F0} Buffs:{buffs.Count}";
    }

    #endregion
}

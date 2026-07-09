using Godot;
using Godot.Collections;

/// <summary>
/// 敌人数据类 - 定义敌人的初始属性和行为
/// 用于在创建敌人时初始化实体数据
/// </summary>
[GlobalClass]
public partial class EnemyData : Resource
{
    /// <summary>
    /// 敌人名称（唯一标识）
    /// </summary>
    [Export] public string enemyName = "";

    /// <summary>
    /// 敌人显示名称
    /// </summary>
    [Export] public string displayName = "";

    /// <summary>
    /// 敌人描述
    /// </summary>
    [Export] public string description = "";

    /// <summary>
    /// 敌人标签
    /// </summary>
    [Export] public Array<EntityTag> tags = new();

    /// <summary>
    /// 最大生命值
    /// </summary>
    [Export] public Value maxHealth = new(100f);

    /// <summary>
    /// 攻击力
    /// </summary>
    [Export] public Value attack = new(10f);

    /// <summary>
    /// 防御力
    /// </summary>
    [Export] public Value defense = new(5f);

    /// <summary>
    /// 攻击速度
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
    /// 移动速度
    /// </summary>
    [Export] public Value speed = new(100f);

    /// <summary>
    /// 敌人贴图
    /// </summary>
    [Export] public Texture2D sprite;

    /// <summary>
    /// 敌人缩放
    /// </summary>
    [Export] public Vector2 scale = Vector2.One;

    /// <summary>
    /// 敌人行为列表
    /// </summary>
    [Export] public Array<Behavior> behaviors = new();

    /// <summary>
    /// 敌人初始Buff列表
    /// </summary>
    [Export] public Array<BuffData> initialBuffs = new();

    /// <summary>
    /// 死亡时触发的效果
    /// </summary>
    [Export] public Effect onDeath;

    /// <summary>
    /// 受伤时触发的效果
    /// </summary>
    [Export] public Effect onHurt;

    /// <summary>
    /// 深拷贝EnemyData
    /// </summary>
    public EnemyData Clone()
    {
        return new EnemyData
        {
            enemyName = this.enemyName,
            displayName = this.displayName,
            description = this.description,
            tags = new Array<EntityTag>(this.tags),
            maxHealth = new Value(this.maxHealth.baseValue),
            attack = new Value(this.attack.baseValue),
            defense = new Value(this.defense.baseValue),
            attackSpeed = new Value(this.attackSpeed.baseValue),
            attackRange = new Value(this.attackRange.baseValue),
            critRate = new Value(this.critRate.baseValue),
            critDamage = new Value(this.critDamage.baseValue),
            speed = new Value(this.speed.baseValue),
            sprite = this.sprite,
            scale = this.scale,
            behaviors = new Array<Behavior>(this.behaviors),
            initialBuffs = new Array<BuffData>(this.initialBuffs),
            onDeath = this.onDeath,
            onHurt = this.onHurt,
        };
    }
}
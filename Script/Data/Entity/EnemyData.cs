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
    [Export] public float maxHealth = 100;

    /// <summary>
    /// 攻击力
    /// </summary>
    [Export] public float attack = 10;

    /// <summary>
    /// 防御力
    /// </summary>
    [Export] public float defense = 5;

    /// <summary>
    /// 攻击速度
    /// </summary>
    [Export] public float attackSpeed = 1;

    /// <summary>
    /// 攻击范围
    /// </summary>
    [Export] public float attackRange = 1;

    /// <summary>
    /// 暴击率（0-1）
    /// </summary>
    [Export] public float critRate = 1;

    /// <summary>
    /// 暴击伤害倍数
    /// </summary>
    [Export] public float critDamage = 1;

    /// <summary>
    /// 移动速度
    /// </summary>
    [Export] public float speed = 100;

    /// <summary>
    /// 敌人贴图
    /// </summary>
    [Export] public Texture2D sprite;

    /// <summary>
    /// 敌人动画帧
    /// </summary>
    [Export] public SpriteFrames spriteFrames;

    /// <summary>
    /// 敌人物理形状
    /// </summary>
    [Export] public Shape2D physicShape;
    /// <summary>
    /// 敌人碰撞区域形状
    /// </summary>
    [Export] public Shape2D areaShape;
    /// <summary>
    /// 近战攻击形状，为空则不生成近战弹幕。
    /// </summary>
    [Export] public Shape2D attackShape;

    /// <summary>
    /// 敌人缩放
    /// </summary>
    [Export] public Vector2 scale = Vector2.One;



    /// <summary>
    /// 敌人初始Buff列表
    /// </summary>
    [Export] public Array<BuffData> initialBuffs = new();
}
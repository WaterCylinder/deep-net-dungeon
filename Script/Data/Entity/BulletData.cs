using Godot;
using Godot.Collections;

/// <summary>
/// 弹幕数据类 - 定义弹幕的初始属性和行为
/// 用于在创建弹幕时初始化实体数据
/// </summary>
[GlobalClass]
public partial class BulletData : Resource
{
    /// <summary>
    /// 弹幕名称（唯一标识）
    /// </summary>
    [Export] public string bulletName = "Bullet";

    /// <summary>
    /// 弹幕标签列表
    /// </summary>
    [Export] public Array<BulletTag> bulletTags = new();

    /// <summary>
    /// 弹幕存在时间（秒），0表示永久存在
    /// </summary>
    [Export] public float lifetime = 5f;

    /// <summary>
    /// 弹幕伤害倍率
    /// </summary>
    [Export] public float damageMultiplier = 1f;

    /// <summary>
    /// 弹幕贴图
    /// </summary>
    [Export] public Texture2D sprite;

    /// <summary>
    /// 弹幕缩放
    /// </summary>
    [Export] public Vector2 scale = Vector2.One;

    /// <summary>
    /// 弹幕碰撞形状偏移
    /// </summary>
    [Export] public Vector2 collisionOffset = Vector2.Zero;

    /// <summary>
    /// 弹幕碰撞形状半径
    /// </summary>
    [Export] public float collisionRadius = 5f;

    /// <summary>
    /// 是否跟随移动方向改变精灵图像的旋转角度，默认角度为向右
    /// </summary>
    [Export] public bool isRotateWithToward = true;

    /// <summary>
    /// 创建时的行为
    /// </summary>
    [Export] public BulletBehavior onCreate;

    /// <summary>
    /// 更新时的行为
    /// </summary>
    [Export] public BulletBehavior onUpdate;

    /// <summary>
    /// 碰撞时触发的效果
    /// </summary>
    [Export] public Effect onCollision;

    /// <summary>
    /// 生命周期结束时触发的效果
    /// </summary>
    [Export] public Effect onLifetimeEnd;

}
using Godot;
using Godot.Collections;

public enum BuffTag
{
    None
}

[GlobalClass]
public partial class BuffData : Resource
{
    /// <summary>
    /// Buff图标
    /// </summary>
    [Export] public Texture2D sprite;

    /// <summary>
    /// Buff名称（唯一标识）
    /// </summary>
    [Export] public string name;

    /// <summary>
    /// Buff描述
    /// </summary>
    [Export] public string description;

    /// <summary>
    /// 持续时间（秒），0表示永久
    /// </summary>
    [Export] public int duration;

    /// <summary>
    /// 最大层数
    /// </summary>
    [Export] public int level;

    /// <summary>
    /// 定时触发间隔（秒），0表示不定时触发
    /// </summary>
    [Export] public float processTime;

    /// <summary>
    /// Buff标签列表
    /// </summary>
    [Export] public Array<BuffTag> tags;

    /// <summary>
    /// Buff是否显示（UI上）
    /// </summary>
    [Export] public bool isVisible = true;

    /// <summary>
    /// 是否是唯一Buff（同类Buff只能存在一个）
    /// </summary>
    [Export] public bool isUnique = false;

    #region Effect行为

    /// <summary>
    /// 挂载时触发的Effect
    /// </summary>
    [Export] public Effect onStart;

    /// <summary>
    /// 每帧更新时触发的Effect
    /// </summary>
    [Export] public Effect onUpdate;

    /// <summary>
    /// 定时触发的Effect（按processTime间隔）
    /// </summary>
    [Export] public Effect onTimer;

    /// <summary>
    /// 消失时触发的Effect
    /// </summary>
    [Export] public Effect onEnd;

    /// <summary>
    /// 实体受伤时触发的Effect
    /// </summary>
    [Export] public Effect onEntityHurt;

    /// <summary>
    /// 实体死亡时触发的Effect
    /// </summary>
    [Export] public Effect onEntityDeath;

    /// <summary>
    /// 层数增加时触发的Effect
    /// </summary>
    [Export] public Effect onStackIncrease;

    /// <summary>
    /// 层数减少时触发的Effect
    /// </summary>
    [Export] public Effect onStackDecrease;

    #endregion
}
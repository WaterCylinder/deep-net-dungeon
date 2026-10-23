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
    /// BuffID（唯一标识）
    /// </summary>
    [Export] public string id;

    /// <summary>
    /// Buff显示名称
    /// </summary>
    [Export] public string name;

    /// <summary>
    /// Buff描述
    /// </summary>
    [Export] public string intro;

    /// <summary>
    /// 持续时间（秒），0表示永久
    /// </summary>
    [Export] public int duration;

    /// <summary>
    /// 最大层数
    /// </summary>
    [Export] public int maxStack;

    /// <summary>
    /// 挂载时的默认层数。
    /// 挂载时不显式传层数（或传入 ≤0）时使用本值；叠加层数（isAdded）时每层也按本值增加。
    /// ≤0 时按 1 层处理。会被 level（最大层数）截断
    /// </summary>
    [Export] public int baseStack = 1;

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

    /// <summary>
    /// 重复挂载同类Buff时是否叠加层数（仅在 isUnique = true 时生效）：
    /// true = 在已有Buff上叠加 baseStack 指定的层数；
    /// false = 只重置持续时间，层数不变
    /// </summary>
    [Export] public bool isAdded = false;

    [Export] public Dictionary<string, Variant> args;
 
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
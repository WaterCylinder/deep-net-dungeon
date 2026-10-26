using Godot;
using Godot.Collections;

public enum DamageTag
{
    /// <summary>
    /// 无
    /// </summary>
    None,
    /// <summary>
    /// 灼烧伤害
    /// </summary>
    Fire,
    /// <summary>
    /// 病毒伤害
    /// </summary>
    Virus,
    /// <summary>
    /// 崩坏伤害
    /// </summary>
    Collapse,
    /// <summary>
    /// 暴击伤害
    /// </summary>
    Crit,
    /// <summary>
    /// 忽略护甲
    /// </summary>
    IgnoreDefense,
    /// <summary>
    /// 忽略护盾
    /// </summary>
    IgnoreShield,
}

public partial class Damage : Node
{
    [Export]public Entity source;
    [Export]public Value value;
    [Export]public float critRate = 0;
    [Export]public float critDamage = 1;
    [Export]public Array<DamageTag> tags = new();
    public Damage(){}
    public Damage(float value, Entity source = null)
    {
        this.value = new Value(value);
        this.source = source;
    }
    public void AddTag(DamageTag tag)
    {
        if (!tags.Contains(tag))
        {
            tags.Add(tag);
        }
    }
    public bool CheckTag(DamageTag tag)
    {
        return tags.Contains(tag);
    }
}

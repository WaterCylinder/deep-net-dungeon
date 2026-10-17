using Godot;
using Godot.Collections;

public enum DamageTag
{
    Physical,
    Magical,
    True,
    Fire,
}

public partial class Damage : Node
{
    [Export]public Entity source;
    [Export]public Value value;
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

using Godot;
using Godot.Collections;

public enum DamageTag
{
    Physical,
    Magical,
    True
}

public partial class Damage : Node
{
    [Export]public Entity source;
    [Export]public Value value;
    [Export]public Array<DamageTag> tags = new();
}

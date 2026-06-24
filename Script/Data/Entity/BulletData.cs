using Godot;
using Godot.Collections;

public partial class BulletData : Resource
{
    [Export] public string bulletName;
    [Export] public Array<BulletTag> bulletTags;
    [Export] public float lifetime = 0f;
    [Export] public Behavior onUpdate;
}

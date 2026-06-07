using Godot;
using Godot.Collections;

public enum BuffTag
{
    None
}

[GlobalClass]
public partial class BuffData : Resource
{
    [Export]public Texture2D sprite;
    [Export]public string name;
    [Export]public string description;
    [Export]public int duration;
    [Export]public int level;
    [Export]public float processTime;

    [Export]public Array<BuffTag> tags;
    [Export]public Effect onStart;
    [Export]public Effect onUpdate;
    [Export]public Effect onTimer;
    [Export]public Effect onEnd;

}

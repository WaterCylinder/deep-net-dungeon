using Godot;
using Godot.Collections;

public enum EffectTag
{
    None,
}
[GlobalClass]
public partial class Effect : Resource
{
    [Export]public string effectName;
    [Export]public Condition condition;
    [Export]public Behavior behavior;
    [Export]public Array<EffectTag> tags;
    public bool Run(params object[] args)
    {
        if (condition.Run(args))
        {
            behavior.Run(args);
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool CheckTag(EffectTag tag)
    {
        return tags.Contains(tag);
    }

    public void AddTag(EffectTag tag)
    {
        if (!CheckTag(tag))
            tags.Add(tag);
    }

    public void RemoveTag(EffectTag tag)
    {
        if (CheckTag(tag))
            tags.Remove(tag);
    }

}

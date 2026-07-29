using Godot;
using Godot.Collections;

public enum EffectTag
{
    None,
}

/// <summary>
/// Effect基类 - 非泛型版本，可在编辑器中直接使用
/// behavior 类型不限（任何 Behavior 子类）
/// </summary>
[GlobalClass]
public partial class Effect : Resource
{
    [Export] public string effectName;
    [Export] public Condition condition;
    [Export] public Behavior behavior;
    [Export] public Array<EffectTag> tags = new();

    /// <summary>
    /// 执行Effect：若条件满足则执行行为
    /// </summary>
    public virtual bool Run(params object[] args)
    {
        if (condition?.Run(args) == true)
        {
            behavior?.Run(args);
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
using System.Collections.Generic;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class BindBehavior : Behavior
{
    [Export]public Array<Behavior> behaviorArray;
    public override T Run<T>(params object[] args)
    {
        behaviorName = "RunList";
        return base.Run<T>([args]);
    }
    
    public List<object> RunList(object[] args)
    {
        List<object> result = new(); 
        foreach (Behavior behavior in behaviorArray)
        {
            result.Add(behavior.Run<object>([.. args]));
        }
        return result;
    }
}
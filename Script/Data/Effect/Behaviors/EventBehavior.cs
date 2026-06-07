using Godot;
using System;

[GlobalClass]
public partial class EventBehavior : Behavior
{
	public Action action;
	public override T Run<T>(params object[] args)
    {
        behaviorName = "Execute";
        return base.Run<T>();
    }
	public object Execute()
	{
		action?.Invoke();
		return null;
	}
}

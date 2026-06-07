using System;
using Godot;
using Godot.Collections;

[GlobalClass]
public abstract partial class Condition : Resource
{
	[Export]public Behavior behavior;
	[Export]public string info;
	[Export]public bool not;
	[Export]public bool result;

	/// <summary>
	/// 条件行为初始化方法
	/// </summary>
	/// <param name="behavior"></param>
	public Action<Behavior> OnBehaviorInit;

	public virtual bool Run(params object[] args)
	{
		try
		{
			OnBehaviorInit?.Invoke(behavior);
		}
		catch {}
		
		result = behavior.Run<bool>(args);
		if (not)
			result = !result;
		return result;
	}
}




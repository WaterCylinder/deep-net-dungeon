using Godot;
using Godot.Collections;

/// <summary>
/// 或条件集
/// </summary>
[GlobalClass]
public partial class OrCondition : Condition
{
	[Export]public Array<Condition> conditionArray = new();

	public override bool Run(params object[] args)
	{	
		result = false;
		foreach(Condition c in conditionArray)
		{
			c.OnBehaviorInit = OnBehaviorInit;
			if (c.Run(args))
			{
				result = true;
				return result;
			}
		}
		if (not)
			result = !result;
		return result;
	}

}
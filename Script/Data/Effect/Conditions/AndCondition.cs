using Godot;
using Godot.Collections;

/// <summary>
/// 与条件集
/// </summary>
[GlobalClass]
public partial class AndCondition : Condition
{
	[Export]public Array<Condition> conditionArray = new();

	public override bool Run(params object[] args)
	{	
		result = true;
		foreach(Condition c in conditionArray)
		{
			c.OnBehaviorInit = OnBehaviorInit;
			if (!c.Run(args))
			{
				result = false;
				return result;
			}
		}
		if (not)
			result = !result;
		return result;
	}

}
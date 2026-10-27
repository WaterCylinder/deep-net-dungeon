using System.Linq;
using Godot;
using Godot.Collections;
using Utils;

/// <summary>
/// 物品池，用于定义物品集合用于随机抽取
/// </summary>
[GlobalClass]
public partial class ItemPool : Resource
{
	[Export]public Dictionary<ItemData, int> pool;

	public System.Random random;
	public System.Random Random
	{
		get
		{
			if(random == null)
			{
				random = GameManager.Instance.game.randomSystem.GetGenerator("item_pool");
			}
			return random;
		}
	}

	public ItemData Pick()
	{
		//池子为空、或所有条目权重都<=0时，PickWeighted会返回空列表，
		//直接取[0]会抛异常；这里统一返回null，由调用方决定要不要生成东西
		if(pool == null || pool.Count == 0)
			return null;

		System.Collections.Generic.List<ItemData> datas = Tools.PickWeighted(pool.ToDictionary(), 1, Random);
		if(datas == null || datas.Count == 0)
			return null;

		return datas[0];
	}
}

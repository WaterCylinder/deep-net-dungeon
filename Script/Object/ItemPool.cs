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
		ItemData data = Tools.PickWeighted(pool.ToDictionary(), 1, Random)[0];
		return data;
	}
}

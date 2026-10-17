using Godot;
using System;
using System.Collections.Generic;

public partial class RandomSystem
{
	private Random randomObjDefault;
	private int defaultSeed;
	private Dictionary<string, Random> randomObjDict = new();
	private int randomSeed;
	public int NextSeed()
	{
		randomSeed += 1;
		return randomSeed;
	}
	public RandomSystem(object seed = null)
	{
		int result;
		if(seed == null)
		{
			seed = Time.GetUnixTimeFromSystem();
		}
		try
		{
			result = Convert.ToInt32(seed);
		}
		catch (Exception e)
		{
			e.GetHashCode();
			result = seed.GetHashCode();
		}
		randomObjDefault = new Random(result);
		defaultSeed = result;
		randomSeed = defaultSeed;
		GD.Print("随机数系统种子基数：", result);
	}

	/// <summary>
	/// 设置生成器，未配置则随机名称和种子
	/// </summary>
	/// <param name="seed"></param>
	/// <param name="generatorName"></param>
	/// <returns>返回生成器名称</returns>
	public string SetRandomGenerator(int seed = -1, string generatorName = null)
	{
		if (seed == -1)
		{
			seed = NextSeed();
		}
		if (generatorName == null)
		{
		    generatorName = Guid.NewGuid().ToString();
		}
		if (! randomObjDict.ContainsKey(generatorName))
		{
			randomObjDict.Add(generatorName, new Random(seed));
		}
		return generatorName;
	}

	/// <summary>
	/// 获取随机数生成器，未配置或不存在则获取默认生成器
	/// </summary>
	/// <param name="generator"></param>
	/// <returns></returns>
	public Random GetGenerator(string generator)
	{
		Random rTemp = null;
		if (generator == null || !randomObjDict.ContainsKey(generator))
		{
			rTemp = randomObjDefault;
		}
		else
		{
			rTemp = randomObjDict[generator];
		}
		return rTemp;
	}

	public Random GetDefaultGenerator()
	{
		return GetGenerator(null);
	}

	/// <summary>
	/// 生成min到max的随机浮点数
	/// </summary>
	/// <param name="min"></param>
	/// <param name="max"></param>
	/// <param name="generator"></param>
	/// <returns></returns>
	public float Float(float min = 0, float max = 1, string generator = null)
	{
		Random rTemp = GetGenerator(generator);
		float r = (float)rTemp.NextDouble();
		float result = min + (max - min) * r;
		return result;
	}

	/// <summary>
	/// 生成min到max的随机整数，包含min和max
	/// </summary>
	/// <param name="min"></param>
	/// <param name="max"></param>
	/// <param name="generator"></param>
	/// <returns></returns>
	public int Int(int min = 0, int max = 100, string generator = null)
	{
		Random rTemp = GetGenerator(generator);
		int result = rTemp.Next(min, max + 1);
		return result;
	}

	/// <summary>
	/// 从列表中随机抽取
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="list"></param>
	/// <param name="generator"></param>
	/// <returns></returns>
	public T Pick<T>(List<T> list, string generator = null)
	{
		int count = list.Count;
		if(count == 0)
		{
			return default;
		}
		else if(count == 1)
		{	
			return list[0];
		}
		int r = Int(0, list.Count - 1, generator);
		return list[r];
	}

	/// <summary>
	/// 从列表中随机抽取
	/// </summary>
	/// <param name="list"></param>
	/// <param name="generator"></param>
	/// <returns></returns>
	public T Pick<[MustBeVariant] T>(Godot.Collections.Array<T> list, string generator = null)
	{
		int count = list.Count;
		if(count == 0)
		{
			return default;
		}
		else if(count == 1)
		{	
			return list[0];
		}
		int r = Int(0, list.Count - 1, generator);
		return list[r];
	}

}

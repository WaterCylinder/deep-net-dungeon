using System.Linq;
using Godot;
using Godot.Collections;
using Utils;

[GlobalClass]
public partial class BuildingPool : Resource
{
    [Export] public Dictionary<BuildingData, int> pool;

    Game game => GameManager.Instance.game;
    IncrementWorld world => game.incrementWorld;

    public BuildingData PickOne()
    {
        var dict = pool.ToDictionary();
        return Tools.PickWeighted(dict, 1, game.randomSystem.GetGenerator(world.random))[0];
    }

    public Array<BuildingData> Pick(int count)
    {
        var dict = pool.ToDictionary();
        var result = Tools.PickWeighted(dict, count, game.randomSystem.GetGenerator(world.random));
        return [.. result];
    }

    /// <summary>
    /// 扩展，新增池
    /// </summary>
    /// <param name="other"></param>
    public void Extend(BuildingPool other)
    {
        foreach(var kp in other.pool)
        {
            pool[kp.Key] = kp.Value;
        } 
    }
}

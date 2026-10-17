using System;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class BuildingBehavior : Behavior
{
    protected Game game => GameManager.Instance.game;
    protected IncrementWorld world => game.incrementWorld;

    public void ResourcePointChange(Building building)
    {
        int value = (int)building.value.Get();

        world.ChangeResourcePoint(value);
        building.Log($"获得 {value} 点资源");
        SmallText.CreateSmallText(
            SmallTextType.ValueChangeShow,
            building.GlobalPosition + new Vector2(0, -64),
            value.ToString(),
            value > 0 ? new Color("dd00ff") : Colors.Red 
        ).SetScale(2);
    }

    /// <summary>
    /// 兼容实体类和数据类的
    /// </summary>
    /// <param name="building"></param>
    /// <param name="data"></param>
    public void GetResourcePoint(Building building, BuildingData data)
    {
        if(building == null)
        {
            int value = (int)data.value.Get();
            world.ChangeResourcePoint(value);
        }
        else
        {
            ResourcePointChange(building);
        }
    }

    /// <summary>
    /// 提升value参数
    /// </summary>
    /// <param name="building"></param>
    public void PromoteValue(Building building)
    {
        int promote = building.args.Get<int>("promote");

        building.value.Add(promote, "promote", 5);
    }

    /// <summary>
    /// 修改建筑物参数，仅限整数参数，key为value则修改基础值
    /// </summary>
    /// <param name="building"></param>
    public void ChangeArg(Building building)
    {
        string key = Arg("key", "value");
        int value = Arg("value", 0);

        if(key == "value")
        {
            building.value.AddBase(value);
        }

        if (building.args.Check(key))
        {
            building.args.Set(key, building.args.Get<int>(key) + value);
        }
    }

    /// <summary>
    /// 基础数值增加1
    /// </summary>
    public void ValueUp(Building building)
    {
        building.value.AddModify(building.data.value, "LevelUp");
    }

    /// <summary>
    /// 等级提升
    /// </summary>
    /// <param name="building"></param>
    public void LevelUp(Building building)
    {
        building.LevelUp();
    }

    /// <summary>
    /// turn回合后升级
    /// </summary>
    /// <param name="building"></param>
    public void LevelUpAfterTurn(Building building)
    {
        int turn = building.args.Get<int>("turn");
        int turn_max = building.args.Get<int>("turn_max");

        turn -= 1;
        if(turn <= 0)
        {
            LevelUp(building);
            turn = turn_max;
        }
        building.args.Set("turn", turn);
    }

    /// <summary>
    /// 获取建筑的行为，兼容data
    /// </summary>
    /// <param name="building"></param>
    public void GetBuilding(Building building, BuildingData _data)
    {
        BuildingData data = null;
        if(building == null)
        {
            data = building.args.Get<BuildingData>("building");
        }
        else if(_data != null)
        {
            data = _data.defaultArgs["building"].As<BuildingData>();
        }
        
        world.AddBuildingItem(data);
    }

    /// <summary>
    /// 回合后获取建筑
    /// </summary>
    /// <param name="building"></param>
    public void GetBuildingAfterTurn(Building building)
    {
        int turn = building.args.Get<int>("turn");
        int turn_max = building.args.Get<int>("turn_max");
        BuildingData data = building.args.Get<BuildingData>("building");
        turn -= 1;
        if(turn <= 0)
        {
            world.AddBuildingItem(data);
            turn = turn_max;
        }
        building.args.Set("turn", turn);
    }

    /// <summary>
    /// 获取建筑，兼容data
    /// </summary>
    /// <param name="building"></param>
    public void GetBuildingByValue(Building building, BuildingData _data)
    {
        int value = 1;
        BuildingData data = null;
        if (building != null)
        {
            value = (int)building.value.Get();
            data = building.args.Get<BuildingData>("building");
        }
        else if(_data != null)
        {
            value = (int)_data.value.Get();
            data = _data.defaultArgs["building"].As<BuildingData>();
        }
        for (int i = 1; i<=value; i++)
        {
            world.AddBuildingItem(data);
        }
    }

    /// <summary>
    /// 回合后获取建筑
    /// </summary>
    /// <param name="building"></param>
    public void GetBuildingByValueAfterTurn(Building building)
    {
        int turn = building.args.Get<int>("turn");
        int turn_max = building.args.Get<int>("turn_max");
        BuildingData data = building.args.Get<BuildingData>("building");
        int value = (int)building.value.Get();
        turn -= 1;
        if(turn <= 0)
        {
            for (int i = 1; i<=value; i++)
            {
                world.AddBuildingItem(data);
            }
            turn = turn_max;
        }
        building.args.Set("turn", turn);
    }

    /// <summary>
    /// 从建筑池里获取建筑
    /// </summary>
    /// <param name="building"></param>
    public void GetBuildingFromPool(Building building, BuildingData _data)
    {
        int value = 1;
        if(building != null)
        {
            value = (int)building.value.Get();
        }
        else if(_data != null)
        {
            value = (int)_data.value.Get();
        }
        string poolPath = Arg<string>("pool");
        BuildingPool pool = AssetManager.Instance.GetData<BuildingPool>(poolPath, null);
        for(int i = 1; i<=value; i++)
        {
            BuildingData data = pool?.PickOne();
            world.AddBuildingItem(data);
        }
        
    }

    /// <summary>
    /// 四周的建筑资源量提升该建筑的数值
    /// </summary>
    /// <param name="building"></param>
    public void AroundPromoteEffect(Building building)
    {
        foreach(var b in world.GetAllBuilding())
        {
            if(b.id == building.id)continue;
            var list = b.GetAround4();
            int count = 0;
            foreach(var bb in list)
            {
                if(bb.id == building.id)
                {
                    count += (int)bb.value.Get();
                }
            }
            b.value.Add(count, "AroundPromoteEffect", 2);
        }

    }
}

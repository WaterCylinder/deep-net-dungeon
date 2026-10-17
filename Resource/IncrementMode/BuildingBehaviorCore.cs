using Godot;
using System;

[GlobalClass]
public partial class BuildingBehaviorCore : BuildingBehavior
{

    public void CoreValueUp(Building building)
    {
        int value = (int)building.value.Get();

        foreach(var b in world.GetAllBuilding())
        {
            if(b.id == "core")
            {
                b.value.AddModify(value, "CoreValueUp");
            }
        }
    }

    /// <summary>
    /// turn回合后核心的资源量上升
    /// </summary>
    /// <param name="building"></param>
    public void CoreValueUpAfterTurn(Building building)
    {
        int turn = building.args.Get<int>("turn");
        int turn_max = building.args.Get<int>("turn_max");
        turn -= 1;
        if(turn <= 0)
        {
            CoreValueUp(building);
            turn = turn_max;
        }
        building.args.Set("turn", turn);
    }

    /// <summary>
    /// turn回合后获取与核心等量的资源
    /// </summary>
    /// <param name="building"></param>
    public void CoreGetCoreValueAfterTurn(Building building)
    {
        int turn = building.args.Get<int>("turn");
        int turn_max = building.args.Get<int>("turn_max");
        turn -= 1;
        if(turn <= 0)
        {
            int value = 0;
            foreach(var b in world.GetAllBuilding())
            {
                if(b.id == "core")
                {
                    value += (int)b.value.Get();
                }
            }
            building.value.SetBase(value);
            ResourcePointChange(building);
            turn = turn_max;
        }
        building.args.Set("turn", turn);
    }

    /// <summary>
    /// turn回合后获取指定value个建筑
    /// </summary>
    /// <param name="building"></param>
    public void CoreGetBuildingAfterTurn(Building building)
    {
        int turn = building.args.Get<int>("turn");
        int turn_max = building.args.Get<int>("turn_max");
        BuildingData data = building.args.Get<BuildingData>("building");
        turn -= 1;
        if(turn <= 0)
        {
            for(int i = 1; i<=building.value; i++)
                world.AddBuildingItem(data);
            turn = turn_max;
        }
        building.args.Set("turn", turn);
    }

    /// <summary>
    /// 核心主机的核心数值翻倍效果
    /// </summary>
    /// <param name="building"></param>
    public void CoreDouble(Building building)
    {
        int count = 1;
        foreach(var b in world.GetAllBuilding())
        {
            if(b.id == building.id)
            {
                count += (int)b.value.Get();
            }
        }
        foreach(var b in world.GetAllBuilding())
        {
            if(b.id == "core")
            {
                b.value.Multi(count, "CoreDouble", 10);
            }
        }
    }
}

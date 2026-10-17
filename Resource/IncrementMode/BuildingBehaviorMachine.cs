using System.Collections.Generic;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class BuildingBehaviorMachine : BuildingBehavior
{
    /// <summary>
    /// 机械常见光环效果，根据周围四格的机械数量提升自身value
    /// </summary>
    public void MachinePromoteHaloEffect(Building building)
    {
        int promote = building.args.Get<int>("promote");
        var list = building.GetAround4(BuildingTag.Machine);

        building.value.Add(list.Count * promote, "MachinePromoteHaloEffect", 5);
    }

    /// <summary>
    /// 中央机械的全场光环效果
    /// </summary>
    /// <param name="building"></param>
    public void MachineCenterEffect(Building building)
    {
        int value = (int)building.value.Get();

        foreach(var b in world.GetAllBuilding(BuildingTag.Machine))
        {
            if(b.id != "central_control_machine")
                b.value.Add(value, "MachineCenterEffect", 5);
        }
    }

    /// <summary>
    /// 钟表行为
    /// </summary>
    /// <param name="building"></param>
    public void MachineClock(Building building)
    {
        int turn = building.args.Get<int>("turn");
        if(turn > 0)
        {
            turn -= 1;
        }
        else
        {
            int value = (int)building.value.Get();
            turn += value;
            ResourcePointChange(building);
            building.value.AddBase(1);
        }
        building.args.Set("turn", turn);
    }
}
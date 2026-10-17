using Godot;
using System;

[GlobalClass]
public partial class BuildingBehaviorChemical : BuildingBehavior
{
    /// <summary>
    /// 化工分馏器
    /// </summary>
    /// <param name="building"></param>
    public void ChemicalDistillationUnit(Building building)
    {
        int value = (int)building.value.Get();

        foreach(var b in world.GetAllBuilding())
        {
            if(b.id == "chemical_liquid_storage_tank")
            {
                b.value.AddModify(value, "ChemicalDistillationUnit");
            }
        }
    }

    public void ChemicalDistillationUnitLevelUp(Building building)
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
    /// 兼容data的模式
    /// </summary>
    /// <param name="building"></param>
    public void ChemicalInjector(Building building, BuildingData data)
    {
        int value;
        if (building != null)
        {
            value = (int)building.value.Get();
        }
        else
        {
            value = (int)data.value.Get();
        }
        
        foreach(var b in world.GetAllBuilding())
        {
            if(b.id == "chemical_liquid_storage_tank")
            {
                b.value.AddModify(value, "ChemicalInjector");
            }
        }
    }
}

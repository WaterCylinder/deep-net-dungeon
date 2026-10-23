using Godot;
using System;

[GlobalClass]
public partial class BuffBehaviorLevel1 : BuffBehavior
{
    public void HealOneWhenGetScore(Entity entity, Buff buff)
    {
        entity.OnPickEnergy += _HealOneWhenGetScore;
    }

    public void RemoveHealOneWhenGetScore(Entity entity, Buff buff)
    {
        entity.OnPickEnergy -= _HealOneWhenGetScore;
    }

    void _HealOneWhenGetScore(Entity entity, EnergyPoint energyPoint)
    {
        if(Game.IsAble(energyPoint) && energyPoint.energyType == EnergyType.Coin)
        {
            entity.Heal(energyPoint.value);
        }
    }
}

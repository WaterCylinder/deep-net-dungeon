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

    //紧急避险道具效果，护盾消失时获得护甲
    public void EmergencyManual(Entity entity, Buff buff)
    {
        entity.OnShieldBrokenSignal += _GetDefenseOnShieldBroken;
    }
    public void RemoveEmergencyManual(Entity entity, Buff buff)
    {
        entity.OnShieldBrokenSignal -= _GetDefenseOnShieldBroken;
    }
    void _GetDefenseOnShieldBroken(Entity entity)
    {
        BuffData data = AssetManager.Instance.GetData<BuffData>("Resource/v1/Item/Buff/Data/buff_emergency_defense", null);
        entity.AttachBuff(data);
    }

    //紧急避险道具效果，获得100点护甲
    public void EmergencyManualGetDefense(Entity entity, Buff buff)
    {
        entity.defense.Add(100, "EmergencyManual", 5);
    }
    public void EmergencyManualRemoveDefense(Entity entity, Buff buff)
    {
        entity.defense.RemoveMultiplier("EmergencyManual");
    }

    //病毒免疫效果,
    public void AntivirusSoftware(Entity entity, Buff buff)
    {
        entity.OnAttachBuffSignal += _DeleteVirusBuff;
    }
    public void RemoveAntivirusSoftware(Entity entity, Buff buff)
    {
        entity.OnAttachBuffSignal -= _DeleteVirusBuff;
    }
    //移除病毒效果
    void _DeleteVirusBuff(Entity entity, Buff buff)
    {
        if(buff.id == "virus")
            entity.RemoveBuff(buff);
    }

    //迭代器效果,受到伤害时获得等量护盾
    public void Iterator(Entity entity, Buff buff)
    {
        entity.OnHurtSignal += GetShiledWhenHurt;
    }
    public void RemoveIterator(Entity entity, Buff buff)
    {
        entity.OnHurtSignal -= GetShiledWhenHurt;
    }
    void GetShiledWhenHurt(Entity entity, float damage)
    {
        entity.AddShield(damage);
    }

    public void RiftArmorSetTrigger(Entity entity, Buff buff)
    {
        
    }
}

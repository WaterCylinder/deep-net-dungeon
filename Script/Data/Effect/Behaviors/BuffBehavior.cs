using Godot;

/// <summary>
/// Buff行为基类 - 标记类
/// 具体行为逻辑在Buff类中定义，通过Effect.Run()执行
/// </summary>
[GlobalClass]
public partial class BuffBehavior : Behavior
{
    //火焰效果，每0.5秒造成火焰层数伤害，火焰层数降低10%，火焰伤害无视护甲；
    public void Fire(Entity owner, Buff buff)
    {
        Damage damage = new Damage();
        damage.AddTag(DamageTag.Fire);
        damage.AddTag(DamageTag.IgnoreDefense);
        damage.value = buff.currentStack;
        if(damage.value >= 1)
            owner.Hurt(damage);
        buff.RemoveStack((int)Mathf.Max(1, buff.currentStack * 0.1f));
    }

    //病毒效果，每1秒造成病毒层数伤害，病毒伤害无视护盾护甲
    public void Virus(Entity owner, Buff buff)
    {
        Damage damage = new Damage();
        damage.AddTag(DamageTag.Virus);
        damage.AddTag(DamageTag.IgnoreShield);
        damage.AddTag(DamageTag.IgnoreDefense);
        damage.value = buff.currentStack;
        if(damage.value >= 1)
            owner.Hurt(damage);
    }

    //崩坏效果，每1秒造成崩坏层数2倍的伤害，崩坏层数降低50%。
    public void Collapse(Entity owner, Buff buff)
    {
        Damage damage = new Damage();
        damage.AddTag(DamageTag.Collapse);
        damage.value = buff.currentStack * 2;
        if(damage.value >= 1)
            owner.Hurt(damage);
        buff.RemoveStack((int)Mathf.Max(1, buff.currentStack * 0.5f));
    }
}
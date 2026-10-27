using Godot;
using System;

[GlobalClass]
public partial class BulletBehavior : Behavior
{
    public void Test(Bullet bullet, Entity source, Entity target)
    {
        GD.Print($"触发子弹行为: Bullet={bullet?.Name}, Source={source?.Name}, Target={target?.Name}");
    }

    /// <summary>
    /// 给予目标敌方单位10%攻击力的灼烧效果
    /// </summary>
    /// <param name="bullet"></param>
    /// <param name="source"></param>
    /// <param name="target"></param>
    public void SetFire(Bullet bullet, Entity source, Entity target)
    {
        float value_per = bullet.args.Get("fire_per", 0.1f);
        int value = (int)Mathf.Ceil(bullet.attack.Get() * value_per);
        target.AttachBuff(Buff.Fire, value);
    }

    /// <summary>
    /// 给予目标敌方单位1%攻击力的病毒效果
    /// </summary>
    /// <param name="bullet"></param>
    /// <param name="source"></param>
    /// <param name="target"></param>
    public void SetVirus(Bullet bullet, Entity source, Entity target)
    {
        float value_per = bullet.args.Get("virus_per", 0.01f);
        int value = (int)Mathf.Ceil(bullet.attack.Get() * value_per);
        target.AttachBuff(Buff.Virus, value);
    }

    /// <summary>
    /// 给予目标指定层数的流血效果，层数取参数blood_stack，默认1层
    /// </summary>
    /// <param name="bullet"></param>
    /// <param name="source"></param>
    /// <param name="target"></param>
    public void AddBlood(Bullet bullet, Entity source, Entity target)
    {
        int value = bullet.args.Get("blood_stack", 1);
        if (Game.IsAble(target) && value > 0)
        {
            target.AttachBuff(Buff.Blood, value);
        }
    }
}

using Godot;
using System;

[GlobalClass]
public partial class BulletBehaviorLevel1 : BulletBehavior
{
    public void DollCircleMove(Bullet bullet, float delta)
    {
        if (Game.IsAble(bullet.owner))
        {
            bullet.args.Add<float>("angle", 0);
            float angle = bullet.args.Get<float>("angle");
            angle += delta * 2;
            bullet.GlobalPosition = bullet.owner.GlobalPosition + Vector2.Up.Rotated(angle) * 80;
            bullet.args.Set("angle", angle);
        }
    }

    //给予固定数值的病毒效果
    public void GiveVirus(Bullet bullet, Entity owner, Entity target)
    {
        int value = bullet.args.Get<int>("value");
        if (Game.IsAble(target))
        {
            target.AttachBuff(Buff.Virus, value);
        }
    }

    //金色子弹附带护盾值的伤害
    public void GoldBulletOnHit(Bullet bullet, Entity owner, Entity target)
    {
        if(!Game.IsAble(owner) || !Game.IsAble(target))return;
        float value = owner.shield * 0.1f;
        Damage damage = new Damage
        {
            source = owner,
            value = new(value)
        };
        target.Hurt(damage);
    }
}

using Godot;
using Godot.Collections;

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
        float value = owner.shield * 0.3f;
        Damage damage = new Damage
        {
            source = owner,
            value = value
        };
        target.Hurt(damage);
    }

    //大火球死亡后发射小火球
    public void BigRiftFireDeath(Bullet bullet)
    {
        BulletData bulletData = bullet.args.Get<BulletData>("child_bullet");
        Vector2 toward = Vector2.Right;
        float angle = 360 / 5;
        for(int i = 1; i<=5; i++)
        {
            toward = toward.Rotated(Mathf.DegToRad(angle));
            Bullet.Create(bullet.owner, bullet.GlobalPosition, toward.Normalized() * 2, bulletData);
        }
    }
}

using Godot;
using System;

[GlobalClass]
public partial class ItemBehaviorLevel1 : ItemBehavior
{   
    /// <summary>
    /// 获得等同于护甲值的护盾
    /// </summary>
    /// <param name="item"></param>
    /// <param name="target"></param>
    public void GetShieldWithDefense(Item item, Entity target)
    {
        if(!Game.IsAble(target))return;
        float defense = target.defense;
        target.AddShield(defense);
    }
    /// <summary>
    /// 获得等同于最大生命值的护盾
    /// </summary>
    /// <param name="item"></param>
    /// <param name="target"></param>
    public void GetShieldWithMaxhealth(Item item, Entity target)
    {
        if(!Game.IsAble(target))return;
        float max = target.maxHealth;
        target.AddShield(max);
    }
    /// <summary>
    /// 朝向鼠标方向发射弹幕
    /// </summary>
    /// <param name="item"></param>
    /// <param name="target"></param>
    public void ShotBulletTowardMouse(Item item, Entity target)
    {
        BulletData data = item.args.Get<BulletData>("bullet_data");
        float speed = item.args.Get<float>("bullet_speed", 10);
        if(data != null)
        {
            Vector2 toward = InputManager.Instance.GetMouseWorldPosition() - target.GlobalPosition;
            Bullet b = Bullet.Create(target, target.GlobalPosition, toward.Normalized() * speed, data);
        }
    }

    /// <summary>
    /// 终结：让使用者所在房间的所有敌怪获得 fire_stack 层灼烧（默认10层），
    /// 随后立刻按"当前灼烧层数 × 2"结算一次伤害（算上敌怪原本已有的灼烧层数）。
    /// </summary>
    public void TheEnd(Item item, Entity user)
    {
        if (!Game.IsAble(user) || user.room == null) return;

        Room room = user.room;
        if (room.enemyNode == null) return;

        int stack = item.args.Get<int>("fire_stack", 10);

        foreach (Node node in room.enemyNode.GetChildren())
        {
            if (node is not Enemy enemy || !Game.IsAble(enemy)) continue;

            //先上灼烧
            enemy.AttachBuff(Buff.Fire, stack);

            //再按灼烧层数结算一次伤害
            Buff fire = enemy.GetBuff("fire");
            float damageValue = (fire != null ? fire.currentStack : stack) * 2f;
            if (damageValue < 1f) continue;

            Damage damage = new Damage
            {
                source = user,
                value = damageValue
            };
            enemy.Hurt(damage);
        }
    }

}

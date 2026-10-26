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

}

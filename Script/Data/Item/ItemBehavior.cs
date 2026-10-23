using Godot;
using System;

[GlobalClass]
public partial class ItemBehavior : Behavior
{
    /// <summary>
    /// 根据bullet参数设置子弹
    /// </summary>
    /// <param name="item"></param>
    /// <param name="target"></param>
    public void SetBullet(Item item, Entity target)
    {
        BulletData data = item.args.Get<BulletData>("bullet");
        if (Game.IsAble(target))
        {
            target.ChangeBullet(data);
        }
    }

    /// <summary>
    /// 攻击力增加普通乘区加数值value，放在pick执行
    /// </summary>
    /// <param name="item"></param>
    /// <param name="target"></param>
    public void ChangeAttackAddNormal(Item item, Entity target)
    {
        float value = item.args.Get<float>("value");

        if (Game.IsAble(target))
        {
            target.attack.AddModify(value, "Normal");
        }
    }

    /// <summary>
    /// 生命值增加普通乘区加数值value，放在pick执行
    /// </summary>
    /// <param name="item"></param>
    /// <param name="target"></param>
    public void ChangeMaxHealthAddNormal(Item item, Entity target)
    {
        float value = item.args.Get<float>("value");

        if (Game.IsAble(target))
        {
            target.maxHealth.AddModify(value, "Normal");
        }
    }

    /// <summary>
    /// 恢复value的血量
    /// </summary>
    /// <param name="item"></param>
    /// <param name="target"></param>
    public void Heal(Item item, Entity target)
    {
        float value = item.args.Get<float>("value");

        if (Game.IsAble(target))
        {
            target.Heal(value);
        }
    }

    /// <summary>
    /// 防御力增加普通乘区加数值value，放在pick执行
    /// </summary>
    /// <param name="item"></param>
    /// <param name="target"></param>
    public void ChangeDefenseAddNormal(Item item, Entity target)
    {
        float value = item.args.Get<float>("value");

        if (Game.IsAble(target))
        {
            target.defense.AddModify(value, "Normal");
        }
    }

    /// <summary>
    /// 速度增加普通乘区加数值value，放在pick执行
    /// </summary>
    /// <param name="item"></param>
    /// <param name="target"></param>
    public void ChangeSpeedAddNormal(Item item, Entity target)
    {
        float value = item.args.Get<float>("value");

        if (Game.IsAble(target))
        {
            target.speed.AddModify(value, "Normal");
        }
    }
    
}

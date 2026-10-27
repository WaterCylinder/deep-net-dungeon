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
    /// 生命值增加普通乘区乘数值value，放在pick执行
    /// </summary>
    /// <param name="item"></param>
    /// <param name="target"></param>
    public void ChangeMaxHealthMutiNormal(Item item, Entity target)
    {
        float value_per = item.args.Get<float>("value_per");

        if (Game.IsAble(target))
        {
            target.maxHealth.MultiModify(value_per, "Normal");
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
    /// 恢复value百分比的血量
    /// </summary>
    /// <param name="item"></param>
    /// <param name="target"></param>
    public void HealPer(Item item, Entity target)
    {
        float value = item.args.Get<float>("value_per");

        if (Game.IsAble(target))
        {
            float num = target.maxHealth.Get() * (value / 100);
            target.Heal(num);
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
    /// 暴击率增加普通乘区加数值value，放在pick执行
    /// </summary>
    /// <param name="item"></param>
    /// <param name="target"></param>
    public void ChangeCritRateAddNormal(Item item, Entity target)
    {
        float value = item.args.Get<float>("value");

        if (Game.IsAble(target))
        {
            target.critRate.AddModify(value, "Normal");
        }
    }

    /// <summary>
    /// 暴击伤害增加普通乘区加数值value，放在pick执行
    /// </summary>
    /// <param name="item"></param>
    /// <param name="target"></param>
    public void ChangeCritDamageAddNormal(Item item, Entity target)
    {
        float value = item.args.Get<float>("value");

        if (Game.IsAble(target))
        {
            target.critDamage.AddModify(value, "Normal");
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

    /// <summary>
    /// 攻击速度增加普通乘区加数值value，放在pick执行
    /// </summary>
    /// <param name="item"></param>
    /// <param name="target"></param>
    public void ChangeAtackSpeedAddNormal(Item item, Entity target)
    {
        float value = item.args.Get<float>("value");

        if (Game.IsAble(target))
        {
            target.attackSpeed.AddModify(value, "Normal");
        }
    }

    /// <summary>
    /// 增加护盾value，放在pick执行
    /// </summary>
    /// <param name="item"></param>
    /// <param name="target"></param>
    public void AddShield(Item item, Entity target)
    {
        float value = item.args.Get<float>("value");

        if (Game.IsAble(target))
        {
            target.AddShield(value);
        }
    }

    /// <summary>
    /// 移除所有负面效果
    /// </summary>
    /// <param name="item"></param>
    /// <param name="target"></param>
    public void ClearAllNegativeBuff(Item item, Entity target)
    {
        target.RemoveBuffByTag(BuffTag.Negative);
    }

    /// <summary>
    /// 释放一个弹幕
    /// </summary>
    /// <param name="item"></param>
    /// <param name="target"></param>
    public void PushBullet(Item item, Entity target)
    {
        BulletData data = item.args.Get<BulletData>("bullet_data");

        target.args.Add<Bullet>("bullet_now");

        GD.Print("【PUSHBULLET】创建弹幕");
        Bullet b = Bullet.Create(target, target.GlobalPosition, target.GetToward(), data, target);
        target.args.Set("bullet_now", b);
    }

    /// <summary>
    /// 移除释放的弹幕
    /// </summary>
    /// <param name="item"></param>
    /// <param name="target"></param>
    public void RemoveBullet(Item item, Entity target)
    {
        Bullet b = target.args.Get<Bullet>("bullet_now");
        if (Game.IsAble(b))
        {
            b.Kill();
        }
    }
    
}

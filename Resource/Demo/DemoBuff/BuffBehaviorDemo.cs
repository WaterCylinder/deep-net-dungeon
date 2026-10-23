using Godot;
using System;

/// <summary>
/// Buff行为示例 - 改变玩家弹幕类型
/// </summary>
[GlobalClass]
public partial class BuffBehaviorDemo : BuffBehavior
{
    protected Game game => GameManager.Instance.game;
    protected Player player => game.player;

    /// <summary>
    /// 设置玩家的弹幕类型
    /// </summary>
    /// <param name="owner">Buff拥有者</param>
    /// <param name="buff">Buff实例</param>
    public void SetBullet(Entity owner, Buff buff)
    {
        if(owner is not Player targetPlayer)return;

        targetPlayer.ChangeBullet(Arg<BulletData>("bulletData"));
    }

    public void AddAttack(Entity owner, Buff buff)
    {
        float num = Arg("num", 10);
        string key = Arg("key", buff.id + "_add_attack");
        owner.attack.Add(num, key, 4);
    }

    public void MultiAttack(Entity owner, Buff buff)
    {
        float num = Arg("num", 1.1f);
        string key = Arg("key", buff.id + "_multi_attack");
        owner.attack.Multi(num, key, 4);
    }

    /// <summary>
    /// 给弹幕添加点燃buff
    /// </summary>
    /// <param name="owner"></param>
    /// <param name="buff"></param>
    public void AddFireBulletBuff(Entity owner, Buff buff)
    {
        BuffData data = AssetManager.Instance.GetData<BuffData>("Resource.Demo.DemoBuff.buff_fire_bullet", null);
        owner.OnShot += (e, bullet) =>
        {
            bullet.AttachBuff(data);
        };
    }

    /// <summary>
    /// 使弹幕具有点燃目标实体的作用
    /// </summary>
    /// <param name="owner"></param>
    /// <param name="buff"></param>
    public void FireBullet(Entity owner, Buff buff)
    {
        //只给弹幕实体
        if(owner is not Bullet)return;

        BuffData burning = AssetManager.Instance.GetData<BuffData>("Resource.Demo.DemoBuff.buff_burning", null);

        Bullet bullet = owner as Bullet;
        bullet.OnHit += target =>
        {
            // 层数 = 弹幕攻击力的1/10（向下取整），最低1层；是否叠层由 buff_burning 的 isAdded 决定
            int stack = Mathf.Max(1, Mathf.FloorToInt(bullet.attack.Get() / 5f) + 1);
            target.AttachBuff(burning, stack);
        };

    }

    /// <summary>
    /// 燃烧效果
    /// </summary>
    /// <param name="owner"></param>
    /// <param name="buff"></param>
    public void Burning(Entity owner, Buff buff)
    {
        Damage damage = new(buff.currentStack);
        damage.AddTag(DamageTag.Fire);
        owner.Hurt(damage);
        buff.RemoveStack(1);
    }
}

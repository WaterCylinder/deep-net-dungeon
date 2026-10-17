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
}

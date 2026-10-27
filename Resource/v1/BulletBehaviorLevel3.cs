using Godot;
using Godot.Collections;

[GlobalClass]
public partial class BulletBehaviorLevel3 : BulletBehavior
{
    /// <summary>
    /// 电浆弹幕死亡：在原位置放置一发"静止"的爆炸弹幕。
    /// 命中敌人、撞墙、寿命耗尽都会走到 OnDead，所以三种情况都会炸开。
    /// 速度为 Vector2.Zero，爆炸弹幕因此留在原地，不继承原弹幕的飞行方向。
    /// 参数 boom_bullet：爆炸弹幕数据。优先取行为资源自己的 args，
    /// 其次取弹幕数据里的同名参数（两处都能配，方便同一个行为给不同弹幕复用）。
    /// </summary>
    public void ElectDeath(Bullet bullet)
    {
        if (bullet == null || !GodotObject.IsInstanceValid(bullet)) return;

        BulletData boom = Arg<BulletData>("boom_bullet", null);
        if (boom == null)
        {
            boom = bullet.args.Get<BulletData>("boom_bullet");
        }
        if (boom == null)
        {
            GD.PushWarning($"[BulletBehaviorLevel3.ElectDeath] 未配置爆炸弹幕数据（boom_bullet），{bullet.data?.bulletName} 死亡时不会爆炸");
            return;
        }

        Bullet.Create(bullet.owner, bullet.GlobalPosition, Vector2.Zero, boom);
    }
}
using Godot;
using System;

[GlobalClass]
public partial class BulletBehavior : Behavior
{
    public void Test(Bullet bullet, Entity source, Entity target)
    {
        GD.Print($"触发子弹行为: Bullet={bullet?.Name}, Source={source?.Name}, Target={target?.Name}");
    }
}

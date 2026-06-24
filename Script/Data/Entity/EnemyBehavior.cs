using Godot;
using System;

public partial class EnemyBehavior : Behavior
{
    public void Test(Enemy enemy)
    {
        GD.Print($"触发敌人行为: Enemy={enemy.Name}");
    }
}

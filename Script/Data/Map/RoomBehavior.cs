using Godot;
using System;

[GlobalClass]
public partial class RoomBehavior : Behavior
{
    Game game => GameManager.Instance.game;
    public void GenerateItem(Room room)
    {
        ItemData data = room.args.Get<ItemData>("item_data");

        Item.DropItem(data, room.GlobalPosition);
    }

    public void GenerateItemFromPool(Room room)
    {
        ItemPool pool = room.args.Get<ItemPool>("item_pool");

        ItemData data = pool.Pick();

        Item.DropItem(data, room.GlobalPosition);
    }

    //设置boss实体，用于显示boss血条
    public void SetBoss(Room room, Enemy enemy)
    {
        if (Game.IsAble(enemy))
        {
            if (enemy.CheckTag(EnemyTag.BOSS))
            {
                room.Log($"设置Boss敌怪为{enemy}");
                game.hud.BossHealthBar.Visible = true;
                game.hud.BossHealthBar.SetTarget(enemy);
                enemy.OnDeathSignal += (e, dmg) =>
                {
                    game.hud.BossHealthBar.Visible = false;
                };
            }
        }
    }

}

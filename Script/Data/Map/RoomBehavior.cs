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

        //房间数据没配 item_pool 时这里拿到的是 null，直接 pool.Pick() 会抛 NullReferenceException，
        //整个房间的通关流程（onClear 效果链）会被打断，所以这里只告警并跳过
        if (pool == null)
        {
            GD.PushWarning($"[RoomBehavior.GenerateItemFromPool] 房间'{room.Name}'（{room.data?.ResourcePath}）的 args 里没有配置 item_pool，本次不生成道具");
            return;
        }

        ItemData data = pool.Pick();
        if (data == null)
        {
            GD.PushWarning($"[RoomBehavior.GenerateItemFromPool] 房间'{room.Name}'（{room.data?.ResourcePath}）的 item_pool 是空池子（或权重全为0），本次不生成道具");
            return;
        }

        Item.DropItem(data, room.GlobalPosition);
    }

    /// <summary>
    /// 在房间位置生成资源能量点
    /// </summary>
    /// <param name="room"></param>
    public void GenerateCoinEnergy(Room room)
    {
        Vector2I count = room.args.Get("coin_count", Vector2I.One);
        Vector2I value = room.args.Get("coin_value", Vector2I.One);

        EnergyPoint.CreateEnergyPoint(EnergyType.Coin, room.GlobalPosition, game.randomSystem.Int(count.X, count.Y), game.randomSystem.Int(value.X, value.Y), 128, room);
    }

    /// <summary>
    /// 在房间位置生成恢复能量点
    /// </summary>
    /// <param name="room"></param>
    public void GenerateHealthEnergy(Room room)
    {
        Vector2I count = room.args.Get("health_count", Vector2I.One);
        Vector2I value = room.args.Get("health_value", Vector2I.One);

        EnergyPoint.CreateEnergyPoint(EnergyType.Health, room.GlobalPosition, game.randomSystem.Int(count.X, count.Y), game.randomSystem.Int(value.X, value.Y), 128, room);
    }

    /// <summary>
    /// 在房间位置生成护盾能量点。
    /// args：shield_count 数量、shield_value 每颗提供的护盾值。
    /// 注意护盾点的拾取效果是 value×2，所以这里传 shield_value/2，
    /// 让数据里配的 shield_value 就等于玩家实际拿到的护盾。
    /// </summary>
    /// <param name="room"></param>
    public void GenerateShieldEnergy(Room room)
    {
        Vector2I count = room.args.Get("shield_count", Vector2I.One);
        Vector2I value = room.args.Get("shield_value", Vector2I.One);

        EnergyPoint.CreateEnergyPoint(EnergyType.Shield, room.GlobalPosition, game.randomSystem.Int(count.X, count.Y), game.randomSystem.Int(value.X, value.Y) * 0.5f, 128, room);
    }

    /// <summary>
    /// 在房间位置生成攻击能量点（红色）：拾取永久 +攻击力。
    /// args：attack_count 数量、attack_value 每颗提供的攻击力
    /// </summary>
    /// <param name="room"></param>
    public void GenerateAttackEnergy(Room room)
    {
        Vector2I count = room.args.Get("attack_count", Vector2I.One);
        Vector2I value = room.args.Get("attack_value", Vector2I.One);

        EnergyPoint.CreateEnergyPoint(EnergyType.Attack, room.GlobalPosition, game.randomSystem.Int(count.X, count.Y), game.randomSystem.Int(value.X, value.Y), 128, room);
    }

    /// <summary>
    /// 在房间位置生成防御能量点（蓝色）：拾取永久 +护甲。
    /// args：defense_count 数量、defense_value 每颗提供的护甲
    /// </summary>
    /// <param name="room"></param>
    public void GenerateDefenseEnergy(Room room)
    {
        Vector2I count = room.args.Get("defense_count", Vector2I.One);
        Vector2I value = room.args.Get("defense_value", Vector2I.One);

        EnergyPoint.CreateEnergyPoint(EnergyType.Defense, room.GlobalPosition, game.randomSystem.Int(count.X, count.Y), game.randomSystem.Int(value.X, value.Y), 128, room);
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

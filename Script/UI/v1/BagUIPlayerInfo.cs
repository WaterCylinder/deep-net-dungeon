using Godot;
using System;

public partial class BagUIPlayerInfo : Control
{
    /// <summary>
    /// 最大生命值
    /// </summary>
    [Export] public Label maxHealth;

    /// <summary>
    /// 当前生命值
    /// </summary>
    [Export] public Label currentHealth;

    /// <summary>
    /// 攻击力
    /// </summary>
    [Export] public Label attack;

    /// <summary>
    /// 防御力
    /// </summary>
    [Export] public Label defense;
    /// <summary>
    /// 护盾
    /// </summary>
    [Export] public Label shield;

    /// <summary>
    /// 攻击速度，每秒多少次攻击
    /// </summary>
    [Export] public Label attackSpeed;

    /// <summary>
    /// 攻击范围
    /// </summary>
    [Export] public Label attackRange;

    /// <summary>
    /// 暴击率（0-1）
    /// </summary>
    [Export] public Label critRate;

    /// <summary>
    /// 暴击伤害倍数
    /// </summary>
    [Export] public Label critDamage;

    /// <summary>
    /// 速度
    /// </summary>
    [Export] public Label speed;


    public void Update()
    {
        Player player = GameManager.Instance.game.player;
        if (Game.IsAble(player))
        {
            maxHealth.Text = player.maxHealth.Get().ToString("0.#");
            currentHealth.Text = player.currentHealth.ToString("0.#");
            attack.Text = player.attack.Get().ToString("0.#");
            defense.Text = player.defense.Get().ToString("0.#");
            attackSpeed.Text = player.attackSpeed.Get().ToString("0.#");
            critRate.Text = player.critRate.Get().ToString("0.##");
            critDamage.Text = player.critDamage.Get().ToString("0.##");
            speed.Text = player.speed.Get().ToString("0.#");
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        Update();
    }

}

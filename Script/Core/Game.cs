using Godot;
using System;

/// <summary>
/// 游戏核心类
/// 
/// 负责：
///   - 根据GameManager中的gameMode字符串加载对应的游戏模式
///   - 在游戏生命周期的各个阶段调用游戏模式的对应方法
/// </summary>
public partial class Game : Node
{

    [Export] public string gameMode = "Test"; // 可在编辑器中设置默认游戏模式
    /// <summary>
    /// 当前游戏模式实例
    /// </summary>
    public GameModeBase CurrentMode { get; private set; }

    /// <summary>
    /// 当前玩家
    /// </summary>
    public Player player = null;

    public override void _EnterTree()
    {
        base._EnterTree();
        GameManager.Instance.game = this;
    }


    public override void _Ready()
    {
        GD.Print($"[Game] 加载游戏模式: {gameMode}");

        // 通过工厂创建游戏模式实例
        CurrentMode = GameModeFactory.CreateMode(gameMode);

        if (CurrentMode == null)
        {
            GD.PrintErr($"[Game] 无法加载游戏模式: {gameMode}，使用默认模式 Classic");
            CurrentMode = GameModeFactory.CreateMode("Classic");
        }

        // 执行模式生命周期：初始化 -> 开始 -> 加载
        CurrentMode?.OnInit();
        CurrentMode?.OnStart();
        CurrentMode?.OnLoad();
    }

    public override void _Process(double delta)
    {
        CurrentMode?.OnProcess(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        CurrentMode?.OnPhysicsProcess(delta);
    }

    public override void _ExitTree()
    {
        CurrentMode?.OnCleanup();
        CurrentMode = null;
    }

    // ========== 以下为游戏事件中调用模式方法的便捷接口 ==========

    /// <summary>
    /// 进入下一个房间时调用
    /// </summary>
    public void NotifyNextRoom(int roomIndex)
    {
        CurrentMode?.OnNextRoom(roomIndex);
    }

    /// <summary>
    /// 进入下一层时调用
    /// </summary>
    public void NotifyNextLevel(int levelIndex)
    {
        CurrentMode?.OnNextLevel(levelIndex);
    }

    /// <summary>
    /// 获得物品时调用
    /// </summary>
    public void NotifyGetItem(string itemId, int quantity = 1)
    {
        CurrentMode?.OnGetItem(itemId, quantity);
    }

    /// <summary>
    /// 使用物品时调用
    /// </summary>
    public void NotifyUseItem(string itemId)
    {
        CurrentMode?.OnUseItem(itemId);
    }

    /// <summary>
    /// 丢弃物品时调用
    /// </summary>
    public void NotifyDropItem(string itemId)
    {
        CurrentMode?.OnDropItem(itemId);
    }

    /// <summary>
    /// 玩家受伤时调用
    /// </summary>
    public void NotifyPlayerDamaged(float damage, string source = "")
    {
        CurrentMode?.OnPlayerDamaged(damage, source);
    }

    /// <summary>
    /// 玩家治疗时调用
    /// </summary>
    public void NotifyPlayerHealed(float amount)
    {
        CurrentMode?.OnPlayerHealed(amount);
    }

    /// <summary>
    /// 玩家死亡时调用
    /// </summary>
    public void NotifyPlayerDeath()
    {
        CurrentMode?.OnPlayerDeath();
    }

    /// <summary>
    /// 敌人被击杀时调用
    /// </summary>
    public void NotifyEnemyKilled(string enemyId, string enemyType)
    {
        CurrentMode?.OnEnemyKilled(enemyId, enemyType);
    }

    /// <summary>
    /// 房间被清理时调用
    /// </summary>
    public void NotifyRoomCleared(int roomIndex)
    {
        CurrentMode?.OnRoomCleared(roomIndex);
    }

    /// <summary>
    /// Boss战开始时调用
    /// </summary>
    public void NotifyBossStart(string bossId)
    {
        CurrentMode?.OnBossStart(bossId);
    }

    /// <summary>
    /// Boss被击败时调用
    /// </summary>
    public void NotifyBossDefeated(string bossId)
    {
        CurrentMode?.OnBossDefeated(bossId);
    }

    /// <summary>
    /// 触发事件时调用
    /// </summary>
    public void NotifyEventTriggered(string eventName, Variant data = default)
    {
        CurrentMode?.OnEventTriggered(eventName, data);
    }

    /// <summary>
    /// 游戏结束时调用
    /// </summary>
    public void NotifyEnd(bool isVictory)
    {
        CurrentMode?.OnEnd(isVictory);
    }
}
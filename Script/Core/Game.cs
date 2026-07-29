using Godot;
using System;

/// <summary>
/// 游戏核心类
/// 
/// 负责：
///   - 根据GameManager中的gameMode字符串加载对应的游戏模式
///   - 在游戏生命周期的各个阶段调用游戏模式的对应方法
///   - 管理敌人创建（通过EnemyGenerator），并发出敌人生成信号
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

    /// <summary>
    /// 当前相机
    /// </summary>
    public CameraController camera = null;

    /// <summary>
    /// 敌人生成信号
    /// 当敌人生成器被创建时发出，可用于播放生成特效等
    /// </summary>
    [Signal]
    public delegate void EnemySpawnedEventHandler(Enemy enemy);

    [Signal]
    public delegate void RoomSpawnedEventHandler(Room room);

    # region 父类节点

    [Export] public Node enemyNode;

    [Export] public Node roomNode;

    # endregion

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

        //获取相机
        camera = GetNode<CameraController>("%MainCamera");

        // 执行模式生命周期：初始化 -> 开始 -> 加载
        CurrentMode?.OnInit();
        CurrentMode?.OnLoad();
        CurrentMode?.OnStart();
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

    # region 通用方法

    /// <summary>
    /// 在指定位置生成敌人
    /// 创建EnemyGenerator来实际生成敌人，并发出敌人生成信号
    /// </summary>
    /// <param name="position">生成位置（世界坐标）</param>
    /// <param name="data">敌人数据</param>
    /// <param name="parent">EnemyGenerator的父节点（通常是房间）</param>
    /// <returns>创建的EnemyGenerator实例</returns>
    public Enemy SpawnEnemy(EnemyData data, Vector2 position, Node parent = null)
    {
        if(parent == null)
        {
            parent = enemyNode;
        }
        Enemy enemy = Enemy.Create(position, data, parent);

        // 发出敌人生成信号
        EmitSignal(SignalName.EnemySpawned, position, data);

        return enemy;
    }

    /// <summary>
    /// 在指定位置生成房间
    /// </summary>
    /// <param name="position"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public Room SpawnRoom(Vector2 position, RoomData data)
    {

        Room room = Room.Create(position, data, roomNode);

        // 发出敌人生成信号
        EmitSignal(SignalName.RoomSpawned, position, data);

        return room;
    }

    #endregion
}
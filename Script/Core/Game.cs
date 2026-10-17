using Godot;
using Godot.Collections;

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
    #region 游戏工具

    public static bool IsAble(Node node)
    {
        if(node == null || !IsInstanceValid(node) || node.IsQueuedForDeletion())
        {
            return false;
        }
        if(node is Entity entity)
        {
            if (entity.isDead)
            {
                return false;
            }
        }
        return true;
    }

    #endregion

    #region 游戏属性

    [Export] public string gameMode = "Test"; // 可在编辑器中设置默认游戏模式
    /// <summary>
    /// 当前游戏模式实例
    /// </summary>
    [Export] public GameModeBase currentMode;

    /// <summary>
    /// 流程变量
    /// </summary>
    [Export] public int A = 0;

    /// <summary>
    /// 当前玩家
    /// </summary>
    public Player player = null;

    /// <summary>
    /// 当前相机
    /// </summary>
    public CameraController camera = null;

    /// <summary>
    /// 当前地图
    /// </summary>
    public Map map = null;

    /// <summary>
    /// 当前房间
    /// </summary>
    public Room room = null;

    /// <summary>
    /// 敌人生成信号
    /// 当敌人生成器被创建时发出，可用于播放生成特效等
    /// </summary>
    [Signal]
    public delegate void EnemySpawnedEventHandler(Enemy enemy);

    [Signal]
    public delegate void RoomSpawnedEventHandler(Room room);

    [Signal]
    public delegate void ItemSpawnedEventHandler(Item item);

    #endregion

    #region 游戏系统

    public RandomSystem randomSystem = new();

    #endregion

    #region 父类节点

    [Export] public HUD hud;
    [Export] public AudioStreamPlayer2D musicPlayer;
    [Export] public Node soundPlayeyPoolNode;
    [Export] public Node enemyNode;

    [Export] public Node roomNode;

    [Export] public Node2D dropItemNode;

    [Export] public Node smallObjectNode;

    [Export] public IncrementWorld incrementWorld;

    # endregion

    #region 游戏流程变量控制

    public void Next(int step = 100)
    {
        A += step;
    }

    int _cache = 0;
    public void GoTo(int target)
    {
        A = target;
        _cache = target;
    }

    public void Back()
    {
        A = _cache;
    }

    #endregion

    #region 生命周期
    public override void _EnterTree()
    {
        base._EnterTree();
        GameManager.Instance.game = this;
    }

 
    public override void _Ready()
    {
        GD.Print($"[Game] 加载游戏模式: {gameMode}");

        // 通过工厂创建游戏模式实例
        currentMode = GameModeFactory.CreateMode(gameMode);

        if (currentMode == null)
        {
            GD.PrintErr($"[Game] 无法加载游戏模式: {gameMode}，使用默认模式 Classic");
         currentMode = GameModeFactory.CreateMode("Classic");
        }

        //获取相机
        camera = GetNode<CameraController>("%MainCamera");

        //获取地图
        map = GetNode<Map>("%Map");

        // 执行模式生命周期：初始化 -> 开始 -> 加载
        currentMode?.OnInit();
        currentMode?.OnLoad();
        currentMode?.OnStart();
    }

    public override void _Process(double delta)
    {
     currentMode?.OnProcess(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
     currentMode?.OnPhysicsProcess(delta);
    }

    public override void _ExitTree()
    {
     currentMode?.OnCleanup();
     currentMode = null;
    }

    #endregion

    // ========== 以下为游戏事件中调用模式方法的便捷接口 ==========

    /// <summary>
    /// 进入下一个房间时调用
    /// </summary>
    public void NotifyNextRoom(int roomIndex)
    {
     currentMode?.OnNextRoom(roomIndex);
    }

    /// <summary>
    /// 进入下一层时调用
    /// </summary>
    public void NotifyNextLevel(int levelIndex)
    {
     currentMode?.OnNextLevel(levelIndex);
    }

    /// <summary>
    /// 获得物品时调用
    /// </summary>
    public void NotifyGetItem(string itemId, int quantity = 1)
    {
     currentMode?.OnGetItem(itemId, quantity);
    }

    /// <summary>
    /// 使用物品时调用
    /// </summary>
    public void NotifyUseItem(string itemId)
    {
     currentMode?.OnUseItem(itemId);
    }

    /// <summary>
    /// 丢弃物品时调用
    /// </summary>
    public void NotifyDropItem(string itemId)
    {
     currentMode?.OnDropItem(itemId);
    }

    /// <summary>
    /// 玩家受伤时调用
    /// </summary>
    public void NotifyPlayerDamaged(float damage, string source = "")
    {
     currentMode?.OnPlayerDamaged(damage, source);
    }

    /// <summary>
    /// 玩家治疗时调用
    /// </summary>
    public void NotifyPlayerHealed(float amount)
    {
     currentMode?.OnPlayerHealed(amount);
    }

    /// <summary>
    /// 玩家死亡时调用
    /// </summary>
    public void NotifyPlayerDeath()
    {
     currentMode?.OnPlayerDeath();
    }

    /// <summary>
    /// 敌人被击杀时调用
    /// </summary>
    public void NotifyEnemyKilled(string enemyId, string enemyType)
    {
     currentMode?.OnEnemyKilled(enemyId, enemyType);
    }

    /// <summary>
    /// 房间被清理时调用
    /// </summary>
    public void NotifyRoomCleared(int roomIndex)
    {
     currentMode?.OnRoomCleared(roomIndex);
    }

    /// <summary>
    /// Boss战开始时调用
    /// </summary>
    public void NotifyBossStart(string bossId)
    {
     currentMode?.OnBossStart(bossId);
    }

    /// <summary>
    /// Boss被击败时调用
    /// </summary>
    public void NotifyBossDefeated(string bossId)
    {
     currentMode?.OnBossDefeated(bossId);
    }

    /// <summary>
    /// 触发事件时调用
    /// </summary>
    public void NotifyEventTriggered(string eventName, Variant data = default)
    {
     currentMode?.OnEventTriggered(eventName, data);
    }

    /// <summary>
    /// 游戏结束时调用
    /// </summary>
    public void NotifyEnd(bool isVictory)
    {
     currentMode?.OnEnd(isVictory);
    }

    #region 音效池

    public Array<AudioStreamPlayer> soundPlayerPool = new();
    bool init = false;
    int maxSoundCount = 16;

    public void SoundPlayerPoolInit()
    {
        for(int i = 1; i<=maxSoundCount; i++)
        {
            AudioStreamPlayer player = new AudioStreamPlayer();
            soundPlayeyPoolNode.AddChild(player);
            player.Name = "SoundPlayer" + i.ToString();
            player.Bus = "Sound";
            player.MaxPolyphony = 4;
            soundPlayerPool.Add(player);
        }
    }

    /// <summary>
    /// 尽可能获取空闲的音效播放器，没有空闲的则获取第一个
    /// </summary>
    /// <returns></returns>
    public AudioStreamPlayer GetFreeSoundPlayer()
    {
        foreach(var player in soundPlayerPool)
        {
            if(!player.Playing)return player;
        }
        return soundPlayerPool[0];
    }

    public void PlaySound(AudioStream sound)
    {
        if(sound == null)return;
        if(soundPlayeyPoolNode == null)return;
        if(!init)SoundPlayerPoolInit();
        AudioStreamPlayer soundPlayer = GetFreeSoundPlayer();
        soundPlayer.Stream = sound;
        soundPlayer.Play();
    }

    #endregion

    #region 通用方法

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
        EmitSignal(SignalName.EnemySpawned, enemy);

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

        // 发出房间生成信号
        EmitSignal(SignalName.RoomSpawned, room);

        return room;
    }

    /// <summary>
    /// 在指定位置生成物品
    /// </summary>
    /// <param name="position"></param>
    /// <param name="data"></param>
    /// <param name="parent"></param>
    /// <returns></returns>
    public Item SpawnItem(Vector2 position, ItemData data, Node parent= null)
    {
        if(parent == null)
        {
            parent = dropItemNode;
        }
        Item item = Item.CreateItem(data, position, parent);

        EmitSignal(SignalName.ItemSpawned, item);

        return item;
    }

    #endregion
}
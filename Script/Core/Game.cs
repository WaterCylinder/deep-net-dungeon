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



    /// <summary>
    /// 特殊事件信号，检测info来处理特殊事件
    /// </summary>
    /// <param name="item"></param>
    [Signal]
    public delegate void SpecialEventEventHandler(string info);

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
    [Export] public Node2D energyNode;

    [Export] public Node2D smallObjectNode;
    [Export] public Node2D particlesNode;

    [Export] public IncrementWorld incrementWorld;
    [Export] public BGAutoRotateZoom bg;

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
        //清理粒子
        ClearParticles();
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
    /// 敌人生成时调用
    /// </summary>
    public void NotifyEnemySpawned(Enemy enemy)
    {
        currentMode?.OnEnemySpawned(enemy);
    }

    /// <summary>
    /// 敌人被击杀时调用
    /// </summary>
    public void NotifyEnemyKilled(Enemy enemy)
    {
        currentMode?.OnEnemyKilled(enemy);
    }

    /// <summary>
    /// 房间被清理时调用
    /// </summary>
    public void NotifyRoomCleared(Room room)
    {
        currentMode?.OnRoomCleared(room);
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

    #region 音乐（渐变切换）

    /// <summary>
    /// 淡入淡出时用的静音音量
    /// </summary>
    public const float MusicSilentDb = -80f;

    /// <summary>
    /// 第二个音乐播放器：和 musicPlayer 轮流使用，切歌时一个淡出、另一个淡入。
    /// 没在检查器里指定时会自动创建（挂在和 musicPlayer 同一个父节点下）
    /// </summary>
    [Export] public AudioStreamPlayer2D musicPlayer2;

    /// <summary>
    /// 当前正在使用的音乐播放器（两个播放器轮换，记录它才能知道另一个是"空闲"的）
    /// </summary>
    public AudioStreamPlayer2D currentMusicPlayer => musicUsing;

    AudioStreamPlayer2D musicUsing;
    Tween musicTween;

    /// <summary>
    /// 音乐的目标音量（dB），淡入淡出都往这个值走
    /// </summary>
    [Export] public float musicVolumeDb = 0f;

    /// <summary>
    /// 默认渐变时长（秒）
    /// </summary>
    [Export] public float musicFadeTime = 1f;

    /// <summary>
    /// 渐变切换到指定音乐：当前播放器音量降低、另一个播放器音量升高，切换完成后停掉旧的。
    /// 传 null 等同于 StopMusic
    /// </summary>
    /// <param name="stream">要播放的音乐</param>
    /// <param name="fadeTime">渐变时长（秒），传负数用 musicFadeTime，传0立即切换</param>
    public void PlayMusic(AudioStream stream, float fadeTime = -1f)
    {
        if (stream == null)
        {
            StopMusic(fadeTime);
            return;
        }

        InitMusicPlayers();
        if (musicUsing == null)
        {
            return;
        }

        if (fadeTime < 0f)
        {
            fadeTime = musicFadeTime;
        }

        //同一首还在放：只把音量渐变到目标值，不重新开始
        if (musicUsing.Stream == stream && musicUsing.Playing)
        {
            musicTween?.Kill();
            if (fadeTime <= 0f)
            {
                musicUsing.VolumeDb = musicVolumeDb;
                return;
            }
            musicTween = CreateTween();
            musicTween.TweenProperty(musicUsing, "volume_db", musicVolumeDb, fadeTime);
            return;
        }

        AudioStreamPlayer2D from = musicUsing;
        AudioStreamPlayer2D to = (musicUsing == musicPlayer) ? musicPlayer2 : musicPlayer;

        //切换使用的播放器
        musicUsing = to;

        to.Stream = stream;
        to.VolumeDb = MusicSilentDb;
        to.Play();

        musicTween?.Kill();

        if (fadeTime <= 0f)
        {
            to.VolumeDb = musicVolumeDb;
            from.Stop();
            from.Stream = null;
            return;
        }

        musicTween = CreateTween().SetParallel();
        if (from.Playing)
        {
            musicTween.TweenProperty(from, "volume_db", MusicSilentDb, fadeTime);
        }
        musicTween.TweenProperty(to, "volume_db", musicVolumeDb, fadeTime);
        musicTween.Chain().TweenCallback(Callable.From(() =>
        {
            if (GodotObject.IsInstanceValid(from))
            {
                from.Stop();
                from.Stream = null;
            }
        }));
    }

    /// <summary>
    /// 渐变停止当前音乐
    /// </summary>
    /// <param name="fadeTime">渐变时长（秒），传负数用 musicFadeTime，传0立即停止</param>
    public void StopMusic(float fadeTime = -1f)
    {
        InitMusicPlayers();
        if (musicUsing == null || !musicUsing.Playing)
        {
            return;
        }

        if (fadeTime < 0f)
        {
            fadeTime = musicFadeTime;
        }

        AudioStreamPlayer2D from = musicUsing;

        musicTween?.Kill();

        if (fadeTime <= 0f)
        {
            from.Stop();
            from.VolumeDb = MusicSilentDb;
            return;
        }

        musicTween = CreateTween();
        musicTween.TweenProperty(from, "volume_db", MusicSilentDb, fadeTime);
        musicTween.TweenCallback(Callable.From(() =>
        {
            if (GodotObject.IsInstanceValid(from))
            {
                from.Stop();
                from.Stream = null;
            }
        }));
    }

    /// <summary>
    /// 确保两个音乐播放器都存在，并把场景里的 musicPlayer 记为当前播放器
    /// </summary>
    void InitMusicPlayers()
    {
        if (musicPlayer == null)
        {
            return;
        }

        if (musicPlayer2 == null)
        {
            musicPlayer2 = new AudioStreamPlayer2D
            {
                Name = "MusicPlayer2",
                Bus = musicPlayer.Bus,
                Position = musicPlayer.Position,
                VolumeDb = MusicSilentDb,
            };

            //和 musicPlayer 放同一个父节点下，2D定位保持一致
            musicPlayer.GetParent()?.AddChild(musicPlayer2);
        }

        if (musicUsing == null)
        {
            musicUsing = musicPlayer;
            if (musicPlayer.VolumeDb <= MusicSilentDb)
            {
                musicPlayer.VolumeDb = musicVolumeDb;
            }
        }
    }

    #endregion

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

    #region 粒子池

    public Array<GpuParticles2D> particlesPool = new();
    public int particlesMaxCount = 128;

    public GpuParticles2D CreateParticles(string name, Vector2 pos, float scale = 1)
    {
        if(particlesPool.Count >= particlesMaxCount)
        {
            //粒子效果超过上限
        }
        string path = "Asset.Particles." + name;
        PackedScene particlesPrefab = AssetManager.Instance.GetPrefab(path, null);
        if(particlesPrefab != null)
        {
            GpuParticles2D p = particlesPrefab.Instantiate<GpuParticles2D>();
            p.Scale = new Vector2(scale, scale);
            Callable.From(() =>
            {
                particlesNode.AddChild(p);
                p.GlobalPosition = pos;
            }).CallDeferred();
            p.Emitting = true;
            particlesPool.Add(p);
            return p;
        }
        else
        {
            GD.Print($"未找到粒子预制体 {path}");
            return null;
        }
    }

    /// <summary>
    /// 清除播放完毕的粒子
    /// </summary>
    public void ClearParticles()
    {
        foreach(var p in particlesPool)
        {
            if (IsAble(p) && !p.Emitting)
            {
                p.QueueFree();
            }
        }
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

        NotifyEnemySpawned(enemy);

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
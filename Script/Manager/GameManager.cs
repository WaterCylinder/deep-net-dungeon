using Godot;

/// <summary>
/// 游戏管理器 — 所有子管理器的容器节点。
/// 在 Godot 初始化脚本/场景中加载此节点，
/// _Ready 时自动反射扫描并初始化所有 ManagerBase 子类。
/// 
/// 节点结构：
///   GameManager
///     ├── AssetManager
///     ├── (未来的其他 Manager...)
/// </summary>
public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; }

    /// <summary>
    /// 全局配置管理器，供所有子管理器共享使用
    /// </summary>
    public Config config;

    public Game game;

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        // 初始化全局 Config 并加载所有配置
        config = new Config();
        config.LoadConfig();           // config.json
        config.LoadConfig("key_mapper"); // key_mapper.json
        ApplyAudioConfig();            // 把保存的三个音量应用到音频总线

        // 注册所有游戏模式
        RegisterGameModes();

        ManagerBase.InitAllManager(this);
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// 把配置中保存的音量应用到音频总线，配置缺项时保留总线默认布局
    /// </summary>
    private void ApplyAudioConfig()
    {
        ApplyBusVolume(AudioUtil.BUS_MASTER, "audio.masterVolume");
        ApplyBusVolume(AudioUtil.BUS_SOUND, "audio.sfxVolume");
        ApplyBusVolume(AudioUtil.BUS_MUSIC, "audio.musicVolume");
    }

    /// <summary>
    /// 应用单个总线的音量配置
    /// </summary>
    /// <param name="busName">总线名称</param>
    /// <param name="configPath">配置键路径</param>
    private void ApplyBusVolume(string busName, string configPath)
    {
        Variant value = config.GetConfig(configPath);
        if (value.VariantType == Variant.Type.Nil)
        {
            return;
        }
        AudioUtil.SetVolume(busName, value.AsSingle());
    }

    /// <summary>
    /// 注册所有游戏模式到工厂
    /// </summary>
    private void RegisterGameModes()
    {
        GameModeFactory.Clear();
        GameModeFactory.Register<ClassicMode>("Classic");
        GameModeFactory.Register<TestMode>("Test");
        GameModeFactory.Register<IncrementMode>("Increment");
        // 在此处注册更多游戏模式...
        // GameModeFactory.Register<RushMode>("Rush");
        // GameModeFactory.Register<ChallengeMode>("Challenge");
    }

    /// <summary>
    /// 设置游戏模式（在切换到Game场景前调用）
    /// </summary>
    /// <param name="modeName">模式名称</param>
    public void SetGameMode(string modeName)
    {
        if (GameModeFactory.HasMode(modeName))
        {
            GD.Print($"[GameManager] 设置游戏模式: {modeName}");
        }
        else
        {
            GD.PrintErr($"[GameManager] 未知的游戏模式: {modeName}");
        }
    }
}

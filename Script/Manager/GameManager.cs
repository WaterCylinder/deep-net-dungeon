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
    public Config Config { get; private set; }

    /// <summary>
    /// 当前游戏模式名称（由外部设置，Game类读取此值加载对应模式）
    /// 默认值为 "Classic"
    /// </summary>
    public string GameMode { get; set; } = "Classic";

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        // 初始化全局 Config 并加载所有配置
        Config = new Config();
        Config.LoadConfig();           // config.json
        Config.LoadConfig("key_mapper"); // key_mapper.json

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
    /// 注册所有游戏模式到工厂
    /// </summary>
    private void RegisterGameModes()
    {
        GameModeFactory.Clear();
        GameModeFactory.Register<ClassicMode>("Classic");
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
            GameMode = modeName;
            GD.Print($"[GameManager] 设置游戏模式: {modeName}");
        }
        else
        {
            GD.PrintErr($"[GameManager] 未知的游戏模式: {modeName}");
        }
    }
}

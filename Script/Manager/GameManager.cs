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

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        ManagerBase.InitAllManager(this);
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }
}
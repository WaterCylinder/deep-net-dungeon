using Godot;
using Godot.Collections;

/// <summary>
/// 资源管理器 - 处理所有资源加载和卸载
/// </summary>
public partial class AssetManager : ManagerBase
{
    public static AssetManager Instance { get; private set; }

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    public override void Init()
    {
        Log("资源管理器初始化完成");
    }

    public override void Cleanup()
    {
        Log("资源管理器已清理");
    }

    /// <summary>
    /// 从res://路径加载资源，如果资源不存在则返回GetDefault(_default)的值
    /// </summary>
    /// <typeparam name="T">资源类型，必须继承自Resource</typeparam>
    /// <param name="path">res://路径下的资源路径</param>
    /// <param name="_default">默认资源的文件名，用于从Asset/Default目录加载</param>
    public T GetResource<T>(string path, string _default) where T : Resource
    {
        if (string.IsNullOrEmpty(path))
        {
            Log("GetResource: path为空，使用默认资源");
            return GetDefault<T>(_default);
        }

        T resource = ResourceLoader.Load<T>(path);
        if (resource != null)
        {
            return resource;
        }

        Log($"资源不存在: {path}，使用默认资源: {_default}");
        return GetDefault<T>(_default);
    }

    /// <summary>
    /// 从res://Asset/Default/路径加载默认资源
    /// </summary>
    /// <typeparam name="T">资源类型，必须继承自Resource</typeparam>
    /// <param name="path">Asset/Default下的相对路径</param>
    public T GetDefault<T>(string path) where T : Resource
    {
        if (string.IsNullOrEmpty(path))
        {
            Log("GetDefault: path为空");
            return null;
        }

        string fullPath = $"res://Asset/Default/{path}";
        T resource = ResourceLoader.Load<T>(fullPath);
        if (resource == null)
        {
            Log($"默认资源加载失败: {fullPath}");
        }
        return resource;
    }

    /// <summary>
    /// 根据path获取.tres后缀的资源文件
    /// path支持itemA.itemB.itemC的点分隔格式，自动转换为res://itemA/itemB/itemC.tres
    /// </summary>
    /// <typeparam name="T">资源类型，必须继承自Resource</typeparam>
    /// <param name="path">点分隔的资源路径，如"Data.Entity.PlayerData"</param>
    /// <param name="_default">加载失败时返回的默认值</param>
    public T GetData<T>(string path, T _default) where T : Resource
    {
        if (string.IsNullOrEmpty(path))
        {
            Log("GetData: path为空");
            return _default;
        }

        string convertedPath = Utils.Tools.Path(path, "res://") + ".tres";
        T resource = ResourceLoader.Load<T>(convertedPath);
        if (resource != null)
        {
            return resource;
        }

        Log($"数据资源加载失败: {convertedPath}");
        return _default;
    }

    /// <summary>
    /// 根据path获取.tscn后缀的预制体资源
    /// path支持itemA.itemB.itemC的点分隔格式，自动转换为res://itemA/itemB/itemC.tscn
    /// </summary>
    /// <param name="path">点分隔的资源路径，如"Scene.UI.MainMenu"</param>
    /// <param name="_default">加载失败时返回的默认预制体</param>
    public PackedScene GetPrefab(string path, PackedScene _default)
    {
        if (string.IsNullOrEmpty(path))
        {
            Log("GetPrefab: path为空");
            return _default;
        }

        string convertedPath = Utils.Tools.Path(path, "res://") + ".tscn";
        PackedScene resource = ResourceLoader.Load<PackedScene>(convertedPath);
        if (resource != null)
        {
            return resource;
        }

        Log($"预制体加载失败: {convertedPath}");
        return _default;
    }
}
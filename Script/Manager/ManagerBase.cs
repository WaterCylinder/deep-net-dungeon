using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

/// <summary>
/// 管理器基类 — 所有 Manager 的抽象基类。
/// 
/// 子类使用方式：
///   1. 继承 ManagerBase
///   2. 声明 static Instance 属性
///   3. 重写 _EnterTree/_ExitTree 设置单例
///   4. 实现 Init() 方法
/// 
/// 示例：
///   public partial class AssetManager : ManagerBase
///   {
///       public static AssetManager Instance { get; private set; }
///       public override void _EnterTree() { Instance = this; }
///       public override void _ExitTree() { Instance = null; }
///       public override void Init() { ... }
///   }
/// </summary>
public abstract partial class ManagerBase : Node
{
    private bool _isInitialized = false;

    /// <summary>
    /// 是否已完成初始化
    /// </summary>
    public bool IsInitialized => _isInitialized;

    /// <summary>
    /// 子类必须实现的初始化方法
    /// </summary>
    public abstract void Init();

    /// <summary>
    /// 清理资源（场景切换/退出时调用），默认空实现
    /// </summary>
    public virtual void Cleanup() { }

    /// <summary>
    /// 带管理器名前缀的统一日志
    /// </summary>
    protected void Log(string message)
    {
        GD.Print($"[{GetType().Name}] {message}");
    }

    /// <summary>
    /// 标记初始化完成（子类在 Init 末尾调用）
    /// </summary>
    protected void MarkInitialized()
    {
        _isInitialized = true;
    }

    /// <summary>
    /// 获取指定类型的管理器单例（通过反射读取目标类型的静态 Instance 属性）
    /// </summary>
    public static T Get<T>() where T : ManagerBase
    {
        var instanceProp = typeof(T).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        if (instanceProp != null)
        {
            return (T)instanceProp.GetValue(null);
        }
        return null;
    }

    /// <summary>
    /// 批量初始化所有管理器。
    /// 扫描程序集中所有继承自 ManagerBase 的非抽象类，
    /// 在 parent 节点下创建对应单例节点并依次执行 Init。
    /// 通常在 GameManager._Ready() 中调用：ManagerBase.InitAllManager(this);
    /// 
    /// 节点结构：
    ///   GameManager (由 Godot 初始化脚本/场景加载)
    ///     ├── AssetManager
    ///     ├── ...
    /// </summary>
    public static void InitAllManager(Node parent)
    {
        if (parent == null)
        {
            GD.PrintErr("[ManagerBase] InitAllManager: parent 节点为空");
            return;
        }

        // 通过反射查找所有 ManagerBase 的非抽象子类
        var managerTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(ManagerBase)))
            .ToList();

        GD.Print($"[ManagerBase] 发现 {managerTypes.Count} 个管理器类型");

        foreach (var type in managerTypes)
        {
            string nodeName = type.Name;

            // 跳过已存在的节点
            if (parent.HasNode(nodeName))
            {
                GD.Print($"[ManagerBase] {nodeName} 节点已存在，跳过");
                continue;
            }

            // 创建实例并添加到 parent 下
            var instance = (Node)Activator.CreateInstance(type);
            instance.Name = nodeName;
            parent.AddChild(instance);

            // 调用 Init（AddChild 后 _EnterTree 已设置 Instance）
            if (instance is ManagerBase manager)
            {
                manager.Init();
                manager.MarkInitialized();
                GD.Print($"[ManagerBase] {nodeName} 初始化完成");
            }
        }

        GD.Print($"[ManagerBase] 全部管理器初始化完成");
    }
}
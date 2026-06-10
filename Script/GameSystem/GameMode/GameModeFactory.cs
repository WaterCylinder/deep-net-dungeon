using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// 游戏模式工厂 - 负责根据模式名称创建对应的GameModeBase子类实例
/// 
/// 使用方式：
///   1. 在游戏启动时调用 RegisterAllModes() 注册所有模式
///   2. 调用 CreateMode(modeName) 创建对应模式实例
/// </summary>
public static class GameModeFactory
{
    /// <summary>
    /// 模式注册表：模式名称 -> 创建函数
    /// </summary>
    private static Dictionary<string, Func<GameModeBase>> _modeRegistry = new();

    /// <summary>
    /// 注册一个游戏模式
    /// </summary>
    /// <typeparam name="T">GameModeBase的子类</typeparam>
    /// <param name="modeName">模式名称（用于匹配）</param>
    public static void Register<T>(string modeName) where T : GameModeBase, new()
    {
        if (_modeRegistry.ContainsKey(modeName))
        {
            GD.PrintErr($"[GameModeFactory] 模式已注册: {modeName}");
            return;
        }

        _modeRegistry[modeName] = () => new T();
        GD.Print($"[GameModeFactory] 注册模式: {modeName}");
    }

    /// <summary>
    /// 根据名称创建游戏模式实例
    /// </summary>
    /// <param name="modeName">模式名称</param>
    /// <returns>对应的GameModeBase子类实例，如果未找到则返回null</returns>
    public static GameModeBase CreateMode(string modeName)
    {
        if (string.IsNullOrEmpty(modeName))
        {
            GD.PrintErr("[GameModeFactory] 模式名称为空");
            return null;
        }

        if (_modeRegistry.TryGetValue(modeName, out var creator))
        {
            var mode = creator();
            GD.Print($"[GameModeFactory] 创建模式: {modeName} -> {mode.GetType().Name}");
            return mode;
        }

        GD.PrintErr($"[GameModeFactory] 未找到模式: {modeName}");
        return null;
    }

    /// <summary>
    /// 检查模式是否已注册
    /// </summary>
    /// <param name="modeName">模式名称</param>
    /// <returns>是否已注册</returns>
    public static bool HasMode(string modeName)
    {
        return _modeRegistry.ContainsKey(modeName);
    }

    /// <summary>
    /// 获取所有已注册的模式名称
    /// </summary>
    /// <returns>模式名称列表</returns>
    public static List<string> GetAllModeNames()
    {
        return new List<string>(_modeRegistry.Keys);
    }

    /// <summary>
    /// 清空注册表（主要用于测试）
    /// </summary>
    public static void Clear()
    {
        _modeRegistry.Clear();
    }
}
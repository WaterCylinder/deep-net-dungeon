using Godot;
using System.Collections.Generic;

/// <summary>
/// 场景管理器 - 管理场景切换和场景历史
/// 
/// 功能：
///   - SwitchScene(name): 切换到指定名称的场景
///   - GoNextScene(name): 前往下一场景（记录场景历史）
///   - GoBackScene(): 返回上一场景（历史为空则返回主菜单）
///   - ClearHistory(): 清空场景历史
/// </summary>
public partial class SceneManager : ManagerBase
{
    public static SceneManager Instance { get; private set; }

    /// <summary>
    /// 场景历史栈（存储已访问的场景名称）
    /// </summary>
    private Stack<string> _sceneHistory = new();

    /// <summary>
    /// 主菜单场景名称
    /// </summary>
    private const string MAIN_MENU_SCENE = "res://Scene/StartMenu.tscn";

    /// <summary>
    /// 当前场景名称
    /// </summary>
    public string CurrentScene { get; private set; } = "";

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
        Log("场景管理器初始化完成");
    }

    public override void Cleanup()
    {
        ClearHistory();
        Log("场景管理器已清理");
    }

    /// <summary>
    /// 切换到指定名称的场景（不记录历史）
    /// </summary>
    /// <param name="name">场景名称，如 "MainMenu" 或完整路径 "res://Scene/MainMenu.tscn"</param>
    public void SwitchScene(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            Log("SwitchScene: 场景名称为空");
            return;
        }

        string scenePath = ResolveScenePath(name);
        Log($"切换场景: {scenePath}");

        if (!ResourceLoader.Exists(scenePath))
        {
            Log($"场景不存在: {scenePath}");
            return;
        }

        // 获取当前场景树根节点并切换
        var tree = GetTree();
        if (tree != null)
        {
            Error err = tree.ChangeSceneToFile(scenePath);
            if (err != Error.Ok)
            {
                Log($"场景切换失败: {err}");
            }
            else
            {
                CurrentScene = name;
            }
        }
    }

    /// <summary>
    /// 前往下一个场景（记录当前场景到历史，然后切换）
    /// </summary>
    /// <param name="name">目标场景名称</param>
    public void GoNextScene(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            Log("GoNextScene: 场景名称为空");
            return;
        }

        // 如果有当前场景，将其压入历史栈
        if (!string.IsNullOrEmpty(CurrentScene))
        {
            _sceneHistory.Push(CurrentScene);
            Log($"记录场景历史: {CurrentScene}");
        }

        SwitchScene(name);
    }

    /// <summary>
    /// 返回上一个场景
    /// 如果场景历史为空，则返回主菜单
    /// </summary>
    public void GoBackScene()
    {
        if (_sceneHistory.Count > 0)
        {
            string previousScene = _sceneHistory.Pop();
            Log($"返回场景: {previousScene}，剩余历史数: {_sceneHistory.Count}");

            // 不将返回的场景再次压入历史
            SwitchSceneWithoutHistory(previousScene);
        }
        else
        {
            Log("场景历史为空，返回主菜单");
            GoBackToMainMenu();
        }
    }

    /// <summary>
    /// 直接返回主菜单（清空历史）
    /// </summary>
    public void GoBackToMainMenu()
    {
        Log("返回主菜单");
        ClearHistory();
        SwitchScene(MAIN_MENU_SCENE);
    }

    /// <summary>
    /// 清空场景历史
    /// </summary>
    public void ClearHistory()
    {
        _sceneHistory.Clear();
        Log("场景历史已清空");
    }

    /// <summary>
    /// 获取场景历史深度
    /// </summary>
    public int GetHistoryCount()
    {
        return _sceneHistory.Count;
    }

    /// <summary>
    /// 查看上一场景名称（不出栈）
    /// </summary>
    public string PeekPreviousScene()
    {
        if (_sceneHistory.Count > 0)
        {
            return _sceneHistory.Peek();
        }
        return null;
    }

    /// <summary>
    /// 获取完整场景历史列表（从旧到新）
    /// </summary>
    public List<string> GetHistoryList()
    {
        var list = new List<string>(_sceneHistory);
        list.Reverse();
        return list;
    }

    /// <summary>
    /// 解析场景路径
    /// 如果 name 已经是完整路径（以 res:// 开头），直接返回
    /// 否则拼接为 res://Scene/{name}.tscn
    /// </summary>
    private string ResolveScenePath(string name)
    {
        if (name.StartsWith("res://"))
        {
            return name;
        }

        // 如果已经包含 .tscn 后缀
        if (name.EndsWith(".tscn"))
        {
            return $"res://Scene/{name}";
        }

        return $"res://Scene/{name}.tscn";
    }

    /// <summary>
    /// 切换场景但不记录历史（内部使用）
    /// </summary>
    private void SwitchSceneWithoutHistory(string name)
    {
        string scenePath = ResolveScenePath(name);
        Log($"切换场景(无历史记录): {scenePath}");

        if (!ResourceLoader.Exists(scenePath))
        {
            Log($"场景不存在: {scenePath}");
            return;
        }

        var tree = GetTree();
        if (tree != null)
        {
            Error err = tree.ChangeSceneToFile(scenePath);
            if (err != Error.Ok)
            {
                Log($"场景切换失败: {err}");
            }
            else
            {
                CurrentScene = name;
            }
        }
    }
}
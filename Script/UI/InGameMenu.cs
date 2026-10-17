using Godot;
using System;

/// <summary>
/// 游戏内菜单 - 按下菜单键（PC为ESC）打开
/// 打开时暂停游戏，可继续游戏或返回主菜单
/// </summary>
public partial class InGameMenu : Control
{
    /// <summary>
    /// 菜单面板
    /// </summary>
    [Export] public Control menuPanel;

    public override void _Ready()
    {
        //暂停时仍需响应输入
        ProcessMode = ProcessModeEnum.Always;

        Button returnButton = GetNode<Button>("%ReturnButton");
        Button mainMenuButton = GetNode<Button>("%MainMenuButton");

        returnButton.Pressed += CloseMenu;
        mainMenuButton.Pressed += OnMainMenuButtonPressed;

        //默认隐藏，且不暂停游戏
        if (menuPanel != null)
        {
            menuPanel.Visible = false;
        }
        GetTree().Paused = false;
    }

    public override void _ExitTree()
    {
        //离开场景时确保不残留暂停状态
        SceneTree tree = GetTree();
        if (tree != null)
        {
            tree.Paused = false;
        }
    }

    public override void _Process(double delta)
    {
        InputManager input = InputManager.Instance;
        if (input == null)
        {
            return;
        }

        //菜单键开关菜单：已打开则关闭，未打开则打开
        if (input.IsActionJustPressed(InputManager.Actions.Menu))
        {
            if (IsMenuOpen())
            {
                CloseMenu();
            }
            else
            {
                OpenMenu();
            }
        }
    }

    /// <summary>
    /// 菜单是否已打开
    /// </summary>
    public bool IsMenuOpen()
    {
        return menuPanel != null && menuPanel.Visible;
    }

    /// <summary>
    /// 打开菜单并暂停游戏
    /// </summary>
    public void OpenMenu()
    {
        if (menuPanel != null)
        {
            menuPanel.Visible = true;
        }
        GetTree().Paused = true;
    }

    /// <summary>
    /// 关闭菜单并继续游戏
    /// </summary>
    public void CloseMenu()
    {
        if (menuPanel != null)
        {
            menuPanel.Visible = false;
        }
        GetTree().Paused = false;
    }

    /// <summary>
    /// 返回主菜单
    /// </summary>
    private void OnMainMenuButtonPressed()
    {
        GetTree().Paused = false;
        SceneManager.Instance?.GoBackToMainMenu();
    }
}

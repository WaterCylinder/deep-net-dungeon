using Godot;
using System;

/// <summary>
/// 临时测试用初始界面
/// 包含：游戏标题、开始游戏按钮、设置按钮、退出游戏按钮
/// </summary>
public partial class TempStartMenu : Control
{
    [Export]
    public string gameTitle = "DeepNet Dungeon";

    public override void _Ready()
    {
        // 获取按钮并连接信号
        var startButton = GetNode<Button>("%StartButton");
        var settingButton = GetNode<Button>("%SettingButton");
        var quitButton = GetNode<Button>("%QuitButton");
        var titleLabel = GetNode<Label>("%TitleLabel");

        // 设置标题
        titleLabel.Text = gameTitle;

        // 连接按钮信号
        startButton.Pressed += OnStartButtonPressed;
        settingButton.Pressed += OnSettingButtonPressed;
        quitButton.Pressed += OnQuitButtonPressed;
    }

    /// <summary>
    /// 开始游戏 - 切换到Game.tscn
    /// </summary>
    private void OnStartButtonPressed()
    {
        GD.Print("开始游戏");
        SceneManager.Instance?.GoNextScene("Game");
    }

    /// <summary>
    /// 进入设置界面 - 切换到Setting.tscn
    /// </summary>
    private void OnSettingButtonPressed()
    {
        GD.Print("进入设置界面");
        SceneManager.Instance?.GoNextScene("Setting");
    }

    /// <summary>
    /// 退出游戏
    /// </summary>
    private void OnQuitButtonPressed()
    {
        GD.Print("退出游戏");
        GetTree().Quit();
    }
}
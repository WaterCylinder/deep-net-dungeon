using Godot;
using System;

/// <summary>
/// 临时测试用初始界面
/// 包含：游戏标题、开始探索按钮、虚构工厂按钮、设置按钮、退出游戏按钮
/// </summary>
public partial class TempStartMenu : Control
{
    [Export]
    public string gameTitle = "DeepNet Dungeon";

    public override void _Ready()
    {
        // 获取按钮并连接信号
        var startButton = GetNode<Button>("%StartButton");
        var incrementButton = GetNode<Button>("%IncrementButton");
        var settingButton = GetNode<Button>("%SettingButton");
        var quitButton = GetNode<Button>("%QuitButton");
        var titleLabel = GetNode<Label>("%TitleLabel");

        // 设置标题
        titleLabel.Text = gameTitle;

        // 连接按钮信号
        startButton.Pressed += OnStartButtonPressed;
        incrementButton.Pressed += OnIncrementButtonPressed;
        settingButton.Pressed += OnSettingButtonPressed;
        quitButton.Pressed += OnQuitButtonPressed;
    }

    /// <summary>
    /// 开始探索 - 切换到Game.tscn
    /// </summary>
    private void OnStartButtonPressed()
    {
        GD.Print("开始探索");
        SceneManager.Instance?.GoNextScene("Game");
    }

    /// <summary>
    /// 虚构工厂 - 切换到IncrementMode.tscn
    /// </summary>
    private void OnIncrementButtonPressed()
    {
        GD.Print("进入虚构工厂");
        SceneManager.Instance?.GoNextScene("IncrementMode");
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
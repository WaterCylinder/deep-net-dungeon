using Godot;
using System;

/// <summary>
/// 结局画面：按任意键或点击鼠标返回主菜单。
/// 脚本挂在 GameEnd 场景的 CanvasLayer 节点上（所以基类必须是 CanvasLayer）
/// </summary>
public partial class End : CanvasLayer
{
    /// <summary>
    /// 进入场景后忽略输入的时长（秒）。
    /// 避免玩家进结局时还按着攻击/移动键，结果直接跳过结局画面
    /// </summary>
    [Export] public float inputDelay = 0.3f;

    ulong _startTime;

    /// <summary>
    /// 结局画面的音乐
    /// </summary>
    AudioStreamPlayer musicPlayer;

    public override void _Ready()
    {
        base._Ready();
        _startTime = Time.GetTicksMsec();

        musicPlayer = new AudioStreamPlayer
        {
            Name = "EndMusic",
            Bus = "Music",
            VolumeDb = -80f,
        };
        AddChild(musicPlayer);

        AudioStream endMusic = AssetManager.Instance?.GetMusic("End");
        if (endMusic == null)
        {
            return;
        }

        musicPlayer.Stream = endMusic;
        musicPlayer.Play();

        //淡入
        CreateTween().TweenProperty(musicPlayer, "volume_db", 0f, 1.5f);
    }

    public override void _Input(InputEvent @event)
    {
        bool pressed = @event switch
        {
            InputEventKey key => key.Pressed && !key.Echo,
            InputEventMouseButton mouse => mouse.Pressed,
            _ => false,
        };

        if (!pressed)
        {
            return;
        }

        if (Time.GetTicksMsec() - _startTime < (ulong)(inputDelay * 1000f))
        {
            return;
        }

        GetViewport().SetInputAsHandled();
        SceneManager.Instance?.GoBackToMainMenu();
    }
}

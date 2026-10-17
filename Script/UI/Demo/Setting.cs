using Godot;

/// <summary>
/// 设置界面 — 调整总音量、音效、音乐三个音频总线的音量
/// 返回按钮使用BackButton，回到上一个场景（从主菜单进入则回到主菜单）
/// </summary>
public partial class Setting : Control
{
    /// <summary>
    /// 配置文件中音频配置所在的段落名
    /// </summary>
    private const string CONFIG_SECTION = "audio";

    /// <summary>
    /// 配置文件中三个音量的键名
    /// </summary>
    private const string KEY_MASTER = "masterVolume";
    private const string KEY_SOUND = "sfxVolume";
    private const string KEY_MUSIC = "musicVolume";

    [Export] public HSlider masterSlider;
    [Export] public HSlider soundSlider;
    [Export] public HSlider musicSlider;

    [Export] public Label masterValueLabel;
    [Export] public Label soundValueLabel;
    [Export] public Label musicValueLabel;

    public override void _Ready()
    {
        SetupSlider(masterSlider, masterValueLabel, AudioUtil.BUS_MASTER);
        SetupSlider(soundSlider, soundValueLabel, AudioUtil.BUS_SOUND);
        SetupSlider(musicSlider, musicValueLabel, AudioUtil.BUS_MUSIC);
    }

    public override void _ExitTree()
    {
        //离开界面时补一次写盘，覆盖只改值不触发DragEnded的情况
        SaveVolumeConfig();
    }

    /// <summary>
    /// 初始化单个音量条：取值范围、初始值、信号
    /// </summary>
    /// <param name="slider">音量条</param>
    /// <param name="valueLabel">百分比文本</param>
    /// <param name="busName">目标总线名称</param>
    private void SetupSlider(HSlider slider, Label valueLabel, string busName)
    {
        if (slider == null)
        {
            GD.PushError($"[Setting] 音量条未绑定: {busName}，检查Setting.tscn的node_paths");
            return;
        }

        slider.MinValue = 0;
        slider.MaxValue = 1;
        slider.Step = 0.01;

        //初始值取总线当前音量，避免打开界面时音量突变
        slider.Value = AudioUtil.GetVolume(busName);
        UpdateValueLabel(valueLabel, (float)slider.Value);

        //拖动过程中实时生效
        slider.ValueChanged += value =>
        {
            AudioUtil.SetVolume(busName, (float)value);
            UpdateValueLabel(valueLabel, (float)value);
        };

        //拖动结束后写盘，避免拖动途中反复写文件
        slider.DragEnded += valueChanged =>
        {
            if (valueChanged)
            {
                SaveVolumeConfig();
            }
        };
    }

    /// <summary>
    /// 刷新百分比文本
    /// </summary>
    /// <param name="valueLabel">文本节点</param>
    /// <param name="linear">线性音量</param>
    private static void UpdateValueLabel(Label valueLabel, float linear)
    {
        if (valueLabel == null)
        {
            return;
        }
        valueLabel.Text = $"{Mathf.RoundToInt(Mathf.Clamp(linear, 0f, 1f) * 100)}%";
    }

    /// <summary>
    /// 把三个音量条的当前值写入Config并落盘
    /// </summary>
    private void SaveVolumeConfig()
    {
        Config config = GameManager.Instance?.config;
        if (config == null)
        {
            return;
        }

        if (masterSlider != null)
        {
            config.SetConfig($"{CONFIG_SECTION}.{KEY_MASTER}", (float)masterSlider.Value);
        }
        if (soundSlider != null)
        {
            config.SetConfig($"{CONFIG_SECTION}.{KEY_SOUND}", (float)soundSlider.Value);
        }
        if (musicSlider != null)
        {
            config.SetConfig($"{CONFIG_SECTION}.{KEY_MUSIC}", (float)musicSlider.Value);
        }

        config.SaveConfig();
    }
}

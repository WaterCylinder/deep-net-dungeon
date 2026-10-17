using Godot;

/// <summary>
/// 音频工具 — 音频总线名称常量与线性音量(0-1)的读写
/// 总线默认布局见 res://default_bus_layout.tres
/// </summary>
public static class AudioUtil
{
    /// <summary>
    /// 总音量总线
    /// </summary>
    public const string BUS_MASTER = "Master";

    /// <summary>
    /// 音效总线
    /// </summary>
    public const string BUS_SOUND = "Sound";

    /// <summary>
    /// 音乐总线
    /// </summary>
    public const string BUS_MUSIC = "Music";

    /// <summary>
    /// 静音时使用的分贝值，避免Mathf.LinearToDb(0)得到负无穷
    /// </summary>
    private const float MUTE_DB = -80f;

    /// <summary>
    /// 低于该线性值视为静音
    /// </summary>
    private const float MUTE_THRESHOLD = 0.001f;

    /// <summary>
    /// 读取总线当前音量并换算为0-1的线性值
    /// </summary>
    /// <param name="busName">总线名称</param>
    /// <returns>线性音量，总线不存在或已静音时返回0</returns>
    public static float GetVolume(string busName)
    {
        int index = AudioServer.GetBusIndex(busName);
        if (index < 0)
        {
            GD.PushWarning($"[AudioUtil] 未找到音频总线: {busName}");
            return 0f;
        }

        if (AudioServer.IsBusMute(index))
        {
            return 0f;
        }

        return Mathf.Clamp(Mathf.DbToLinear(AudioServer.GetBusVolumeDb(index)), 0f, 1f);
    }

    /// <summary>
    /// 把0-1的线性音量应用到总线
    /// </summary>
    /// <param name="busName">总线名称</param>
    /// <param name="linear">线性音量，超出范围会被截断</param>
    public static void SetVolume(string busName, float linear)
    {
        int index = AudioServer.GetBusIndex(busName);
        if (index < 0)
        {
            GD.PushWarning($"[AudioUtil] 未找到音频总线: {busName}");
            return;
        }

        float value = Mathf.Clamp(linear, 0f, 1f);
        bool muted = value <= MUTE_THRESHOLD;
        AudioServer.SetBusMute(index, muted);
        AudioServer.SetBusVolumeDb(index, muted ? MUTE_DB : Mathf.LinearToDb(value));
    }
}

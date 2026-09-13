using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TextJitterEffect : RichTextEffect
{
    // BBCode 标签名，用 [jitter]...[/jitter] 包裹文字
    public string bbcode = "jitter";

    // 偏移强度（像素），0 表示无效果
    public float strength = 3f;

    // 抖动频率（每秒刷新次数），越高越"乱"
    public float frequency = 10f;

    // 每个字符的偏移缓存，key 用 relative_index 保证独立性
    private readonly Dictionary<int, Vector2> _offsets = new();

    // 缓存上次刷新时间，避免每帧重算
    private float _lastTick = -1f;

    public override bool _ProcessCustomFX(CharFXTransform charFx)
    {
        // 强度为 0 时直接返回，不产生偏移
        if (strength <= 0f)
        {
            charFx.Offset = Vector2.Zero;
            return true;
        }

        // 按频率分档，让抖动有节奏而非每帧乱跳
        float tick = (float)Mathf.Floor(charFx.ElapsedTime * frequency);
        if (tick != _lastTick)
        {
            _lastTick = tick;
            _offsets.Clear();
        }

        int key = charFx.RelativeIndex;
        if (!_offsets.ContainsKey(key))
        {
            // 生成该字符本 tick 内的随机偏移
            _offsets[key] = new Vector2(
                (float)GD.RandRange(-strength, strength),
                (float)GD.RandRange(-strength, strength)
            );
        }

        charFx.Offset = _offsets[key];
        return true;
    }
}
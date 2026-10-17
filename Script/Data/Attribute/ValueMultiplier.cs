using Godot;

/// <summary>
/// 数值乘区信息
/// 需要[GlobalClass]：否则在检查器里为_multipliers这类"带类型的字典/数组"新建元素时，
/// Godot无法实例化本类型，会退化成没有挂脚本的空Resource，读取时类型转换必然失败
/// </summary>
[GlobalClass]
public partial class ValueMultiplier : Resource
{
    /// <summary>
    /// 乘区ID（唯一标识，避免重复）
    /// </summary>
    [Export] public string id = "";

    /// <summary>
    /// 排序标识码，决定乘区在计算时的先后顺序，越小的Code会在计算的左边
    /// </summary>
    [Export] public int sortCode = 0;

    /// <summary>
    /// 来源数值
    /// </summary>
    [Export] public Value source;

    /// <summary>
    /// 加成值
    /// </summary>
    [Export] public float additive = 0f;

    /// <summary>
    /// 乘数（对结果的倍数影响）
    /// </summary>
    [Export] public float multiplier = 1f;

    /// <summary>
    /// 无参构造函数（Godot需要）
    /// </summary>
    public ValueMultiplier()
    {
    }

    public ValueMultiplier(string id = "", float additive = 0f, float multiplier = 1f, Value source = null, int sortCode = 0)
    {
        this.id = id;
        this.additive = additive;
        this.multiplier = multiplier;
        this.source = source;
        this.sortCode = sortCode;
    }

    public override string ToString()
    {
        return $"Multiplier[{id}] sort:{sortCode} source:{source} add:{additive} mul:{multiplier}";
    }
}
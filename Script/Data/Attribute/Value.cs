using System;
using Godot;
using Godot.Collections;

/// <summary>
/// 复杂数值类 - 支持多乘区、可回滚、可溯源
/// 继承自Resource，可在Godot Inspector中显示
/// </summary>
[GlobalClass]
public partial class Value : Resource
{
    /// <summary>
    /// 基础乘区ID
    /// </summary>
    private const string BASE_ID = "Base";

    /// <summary>
    /// 基础数值
    /// </summary>
    [Export] public float baseValue = 0f;

    /// <summary>
    /// 所有乘区字典（key为乘区ID）
    /// </summary>
    [Export] private Dictionary<string, ValueMultiplier> _multipliers = new();

    /// <summary>
    /// 添加乘区事件 (id, source, oldValue, newValue)
    /// </summary>
    public Action<Value> onMultiplierAdded;

    /// <summary>
    /// 修改乘区事件 (id, source, oldValue, newValue)
    /// </summary>
    public Action<Value> onMultiplierChanged;

    /// <summary>
    /// 获取数值事件 (finalValue)
    /// </summary>
    public Action<float> onValueGet;

    /// <summary>
    /// Value类的构造函数，用于初始化基础乘区
    /// </summary>
    public Value()
    {
        // 初始化基础乘区，将BASE_ID作为标识符，创建名为"Base"的ValueMultiplier实例
        // 初始最小值为0f，初始值为1f
        GetMultiplier(BASE_ID, null);
    }
    
    /// <summary>
    /// Value类的构造函数，允许直接设置基础数值，并自动初始化基础乘区
    /// </summary>
    /// <param name="baseValue"></param>
    public Value(float baseValue) : this()
    {
        this.baseValue = baseValue;
    }

    /// <summary>
    /// 获取乘区，没有则创建新的
    /// </summary>
    private ValueMultiplier GetMultiplier(string id, Value source = null)
    {
        if (_multipliers.TryGetValue(id, out var multiplier))
        {
            return multiplier;
        }

        var newMultiplier = new ValueMultiplier(id, 0f, 1f, source);
        _multipliers.Add(id, newMultiplier);
        onMultiplierAdded?.Invoke(this);
        return newMultiplier;
    }

    /// <summary>
    /// 修改乘区
    /// </summary>
    public void ModifyMultiplier(string id, float? additive = null, float? multiplier = null, Value source = null)
    {

        ValueMultiplier m = GetMultiplier(id, source);

        if (additive.HasValue)
            m.additive = additive.Value;
        if (multiplier.HasValue)
            m.multiplier = multiplier.Value;

        onMultiplierChanged?.Invoke(this);
    }

    /// <summary>
    /// 移除乘区
    /// </summary>
    public bool RemoveMultiplier(string id)
    {
        if (id == BASE_ID)
        {
            GD.PrintErr("无法移除基础乘区!");
            return false;
        }

        if (_multipliers.Remove(id))
        {
            return true;
        }

        GD.PrintErr($"乘区'{id}'不存在，移除失败");
        return false;
    }

    /// <summary>
    /// 检查乘区是否存在
    /// </summary>
    public bool HasMultiplier(string id)
    {
        return _multipliers.ContainsKey(id);
    }

    /// <summary>
    /// 设置基础数值
    /// </summary>
    public void SetBase(float value)
    {
        baseValue = value;
    }

    /// <summary>
    /// 增加基础数值
    /// </summary>
    public void AddBase(float value)
    {
        baseValue += value;
    }

    /// <summary>
    /// 快捷方法：增加基础乘区的加成值
    /// </summary>
    public void Add(float value)
    {
        Add(value, BASE_ID);
    }

    /// <summary>
    /// 快捷方法：添加或修改指定ID乘区的加成值
    /// </summary>
    public void Add(float value, string id)
    {
        ModifyMultiplier(id, additive:value);
    }

    /// <summary>
    /// 快捷方法：增加基础乘区的乘数
    /// </summary>
    public void Multi(float multiplier)
    {
        Multi(multiplier, BASE_ID);
    }

    /// <summary>
    /// 快捷方法：添加或修改指定ID乘区的乘数
    /// </summary>
    public void Multi(float value, string id)
    {
        ModifyMultiplier(id, multiplier:value);
    }

    /// <summary>
    /// 获取最终计算值
    /// </summary>
    public float Get()
    {
        float result = baseValue;

        // 按照乘区顺序计算
        foreach (var multiplier in _multipliers.Values)
        {
            // 应用加成值
            result += multiplier.additive;
            // 应用乘数
            result *= multiplier.multiplier;
        }

        onValueGet?.Invoke(result);
        return result;
    }

    /// <summary>
    /// 打包所有乘区为新的基础数值，然后清除乘区
    /// </summary>
    public void Pack()
    {
        baseValue = Get();

        // 清除所有非基础乘区
        var idsToRemove = new System.Collections.Generic.List<string>();
        foreach (var id in _multipliers.Keys)
        {
            if (id != BASE_ID)
            {
                idsToRemove.Add(id);
            }
        }

        foreach (var id in idsToRemove)
        {
            _multipliers.Remove(id);
        }

        // 重置基础乘区
        _multipliers[BASE_ID] = new ValueMultiplier(BASE_ID, 0f, 1f);
    }


    /// <summary>
    /// 获取当前乘区数量
    /// </summary>
    public int GetMultiplierCount()
    {
        return _multipliers.Count;
    }

    /// <summary>
    /// 清空所有乘区（除基础乘区）
    /// </summary>
    public void ClearMultipliers()
    {
        var idsToRemove = new System.Collections.Generic.List<string>();
        foreach (var id in _multipliers.Keys)
        {
            if (id != BASE_ID)
            {
                idsToRemove.Add(id);
            }
        }

        foreach (var id in idsToRemove)
        {
            _multipliers.Remove(id);
        }
    }

    #region 运算符重载

    /// <summary>
    /// Value + float 相当于 Add
    /// </summary>
    public static Value operator +(Value value, float num)
    {
        value?.Add(num);
        return value;
    }

    /// <summary>
    /// float + Value 相当于 Add
    /// </summary>
    public static Value operator +(float num, Value value)
    {
        value?.Add(num);
        return value;
    }

    /// <summary>
    /// Value - float 相当于 Add(-float)
    /// </summary>
    public static Value operator -(Value value, float num)
    {
        value?.Add(-num);
        return value;
    }

    /// <summary>
    /// Value * float 相当于 Multi
    /// </summary>
    public static Value operator *(Value value, float num)
    {
        value?.Multi(num);
        return value;
    }

    /// <summary>
    /// float * Value 相当于 Multi
    /// </summary>
    public static Value operator *(float num, Value value)
    {
        value?.Multi(num);
        return value;
    }

    /// <summary>
    /// Value / float 相当于 Multi(1/float)
    /// </summary>
    public static Value operator /(Value value, float num)
    {
        if (num != 0)
        {
            value?.Multi(1f / num);
        }
        return value;
    }

    /// <summary>
    /// Value 隐式转换为 float（获取最终值）
    /// </summary>
    public static implicit operator float(Value value)
    {
        return value?.Get() ?? 0f;
    }

    /// <summary>
    /// float 隐式转换为 Value
    /// </summary>
    public static implicit operator Value(float num)
    {
        return new Value(num);
    }

    /// <summary>
    /// Value 比较
    /// </summary>
    public static bool operator ==(Value a, Value b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        return Mathf.IsEqualApprox(a.Get(), b.Get());
    }

    /// <summary>
    /// Value 不等比较
    /// </summary>
    public static bool operator !=(Value a, Value b)
    {
        return !(a == b);
    }

    /// <summary>
    /// Value 大于
    /// </summary>
    public static bool operator >(Value a, Value b)
    {
        return (a?.Get() ?? 0f) > (b?.Get() ?? 0f);
    }

    /// <summary>
    /// Value 小于
    /// </summary>
    public static bool operator <(Value a, Value b)
    {
        return (a?.Get() ?? 0f) < (b?.Get() ?? 0f);
    }

    /// <summary>
    /// Value 大于等于
    /// </summary>
    public static bool operator >=(Value a, Value b)
    {
        return (a?.Get() ?? 0f) >= (b?.Get() ?? 0f);
    }

    /// <summary>
    /// Value 小于等于
    /// </summary>
    public static bool operator <=(Value a, Value b)
    {
        return (a?.Get() ?? 0f) <= (b?.Get() ?? 0f);
    }

    #endregion

    public override string ToString()
    {
        return $"Value({Get():F2})";
    }

    public override bool Equals(object obj)
    {
        if (obj is Value v)
        {
            return Mathf.IsEqualApprox(Get(), v.Get());
        }
        if (obj is float f)
        {
            return Mathf.IsEqualApprox(Get(), f);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return Get().GetHashCode();
    }
}


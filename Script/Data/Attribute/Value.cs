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
    /// 基础乘区的排序标识码，取最小整数保证基础值最先结算
    /// </summary>
    public const int BASE_SORT_CODE = int.MinValue;

    /// <summary>
    /// 基础数值
    /// </summary>
    [Export] public float baseValue = 0f;

    /// <summary>
    /// 所有乘区字典（key为乘区ID）
    /// </summary>
    [Export] private Dictionary<string, ValueMultiplier> _multipliers = new();

    /// <summary>
    /// 乘区计算顺序（按sortCode升序排列的乘区ID）
    /// Godot.Collections.Dictionary没有有序插入接口，因此单独维护顺序
    /// </summary>
    private System.Collections.Generic.List<string> _multiplierOrder = new();

    /// <summary>
    /// 缓存的计算结果
    /// </summary>
    private float _cachedValue = 0f;

    /// <summary>
    /// 缓存对应的修改版本号，与_cachedRevision不一致时需要重算
    /// </summary>
    private int _cachedRevision = -1;

    /// <summary>
    /// 缓存时使用的baseValue，用于检测绕过修改入口的直接改动
    /// </summary>
    private float _cachedBaseValue = 0f;

    /// <summary>
    /// 修改版本号，任何影响结果的改动都自增
    /// </summary>
    private int _revision = 0;

    /// <summary>
    /// 是否已经就"字典里存在非ValueMultiplier的坏条目"告警过（避免每帧/每次刷新刷屏）
    /// </summary>
    private bool _warnedBadMultiplier = false;

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
        // 排序标识码取最小值，保证基础乘区最先结算
        GetMultiplier(BASE_ID, null, BASE_SORT_CODE);
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
    /// 获取乘区，没有则创建新的并按sortCode插入到计算顺序中
    /// </summary>
    private ValueMultiplier GetMultiplier(string id, Value source = null, int sortCode = 0)
    {
        if (TryGetMultiplier(id, out ValueMultiplier multiplier))
        {
            return multiplier;
        }

        //键存在但值不是ValueMultiplier（坏数据）：先清掉再重建，否则Add会因为键重复而失败
        if (_multipliers.ContainsKey(id))
        {
            _multipliers.Remove(id);
            _multiplierOrder.Remove(id);
        }

        var newMultiplier = new ValueMultiplier(id, 0f, 1f, source, sortCode);
        _multipliers.Add(id, newMultiplier);
        InsertOrder(id, sortCode);
        MarkDirty();
        onMultiplierAdded?.Invoke(this);
        return newMultiplier;
    }

    /// <summary>
    /// 安全地读取乘区。
    /// 不能直接调用_multipliers.TryGetValue：带类型的Dictionary包装会在取值时把原生对象转换成
    /// ValueMultiplier，一旦字典里存在没有挂ValueMultiplier脚本的坏条目（例如检查器里误建的空Resource），
    /// 就会抛InvalidCastException并打断整条计算链（Get/Recalculate/调试输出全部失效）。
    /// 这里捕获该异常并按"该项不存在"处理，只告警一次，避免刷屏。
    /// </summary>
    private bool TryGetMultiplier(string id, out ValueMultiplier multiplier)
    {
        multiplier = null;

        try
        {
            return _multipliers.TryGetValue(id, out multiplier);
        }
        catch (InvalidCastException)
        {
            if (!_warnedBadMultiplier)
            {
                _warnedBadMultiplier = true;
                GD.PushWarning($"[Value] 乘区'{id}'的值不是ValueMultiplier（存在没有挂ValueMultiplier脚本的坏条目），已跳过。请检查该Value的_multipliers");
            }
            multiplier = null;
            return false;
        }
    }

    /// <summary>
    /// 按sortCode升序把乘区ID插入计算顺序，sortCode相同时保持插入先后。优先计算小的sortCode。
    /// </summary>
    private void InsertOrder(string id, int sortCode)
    {
        int index = _multiplierOrder.Count;
        for (int i = 0; i < _multiplierOrder.Count; i++)
        {
            if(TryGetMultiplier(_multiplierOrder[i], out ValueMultiplier exist) && exist.sortCode > sortCode)
            {
                index = i;
                break;
            }
        }
        _multiplierOrder.Insert(index, id);
    }

    /// <summary>
    /// 标记结果需要重算
    /// </summary>
    private void MarkDirty()
    {
        _revision++;

        //通知检查器/监听者该资源已变化
        EmitChanged();
    }

    /// <summary>
    /// 计算顺序是否与乘区集合一致，不一致则按sortCode重建
    /// （用于兼容Inspector中对乘区的直接增删）
    /// </summary>
    private void EnsureOrder()
    {
        bool consistent = _multiplierOrder.Count == _multipliers.Count;
        if (consistent)
        {
            foreach (var id in _multiplierOrder)
            {
                if (!_multipliers.ContainsKey(id))
                {
                    consistent = false;
                    break;
                }
            }
        }
        if (consistent)
        {
            return;
        }

        _multiplierOrder.Clear();
        //只遍历"键"：泛型包装的枚举器与索引器都会转换"值"，遇到坏条目会抛InvalidCastException
        foreach (string id in _multipliers.Keys)
        {
            _multiplierOrder.Add(id);
        }
        _multiplierOrder.Sort((a, b) => GetMultiplierSortCode(a).CompareTo(GetMultiplierSortCode(b)));
        MarkDirty();
    }

    /// <summary>
    /// 读取指定乘区的排序码，坏条目按0处理（只读，不抛异常）
    /// </summary>
    private int GetMultiplierSortCode(string id)
    {
        return TryGetMultiplier(id, out ValueMultiplier multiplier) ? multiplier.sortCode : 0;
    }

    /// <summary>
    /// 修改乘区
    /// </summary>
    /// <param name="id">乘区ID</param>
    /// <param name="sortCode">排序标识码，决定乘区在计算时的先后顺序</param>
    /// <param name="additive">加成值，null表示不修改</param>
    /// <param name="multiplier">乘数，null表示不修改</param>
    /// <param name="source">来源数值</param>
    public void ModifyMultiplier(string id, int sortCode, float? additive = null, float? multiplier = null, Value source = null)
    {
        ValueMultiplier m = GetMultiplier(id, source, sortCode);

        bool changed = false;
        if (additive.HasValue && !Mathf.IsEqualApprox(m.additive, additive.Value))
        {
            m.additive = additive.Value;
            changed = true;
        }
        if (multiplier.HasValue && !Mathf.IsEqualApprox(m.multiplier, multiplier.Value))
        {
            m.multiplier = multiplier.Value;
            changed = true;
        }
        if (m.sortCode != sortCode)
        {
            m.sortCode = sortCode;
            changed = true;
        }

        if (!changed)
        {
            return;
        }

        _multiplierOrder.Remove(id);
        InsertOrder(id, m.sortCode);
        MarkDirty();
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
            _multiplierOrder.Remove(id);
            MarkDirty();
            return true;
        }
        
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
    /// 获取乘区的数值
    /// </summary>
    /// <param name="id"></param>
    /// <param name="isAdd"></param>
    /// <returns></returns>
    public float GetMultiplierValue(string id, bool isAdd = true)
    {
        ValueMultiplier multiplier = GetMultiplier(id);
        if (isAdd)
        {
            return multiplier.additive;
        }
        else
        {
            return multiplier.multiplier;
        }
    }

    /// <summary>
    /// 设置基础数值
    /// </summary>
    public void SetBase(float value)
    {
        if (Mathf.IsEqualApprox(baseValue, value))
        {
            return;
        }
        baseValue = value;
        MarkDirty();
    }

    /// <summary>
    /// 增加基础数值
    /// </summary>
    public void AddBase(float value)
    {
        if (Mathf.IsZeroApprox(value))
        {
            return;
        }
        baseValue += value;
        MarkDirty();
    }

    /// <summary>
    /// 快捷方法：增加基础乘区的加成值
    /// </summary>
    public void Add(float value)
    {
        ModifyMultiplier(BASE_ID,  BASE_SORT_CODE, additive: value);
    }

    /// <summary>
    /// 快捷方法：添加或修改指定ID乘区的加成值
    /// 会自动创建乘区
    /// </summary>
    /// <param name="value">加成值</param>
    /// <param name="id">乘区ID</param>
    /// <param name="sortCode">排序标识码，乘区已存在时用于调整其计算顺序</param>
    public void Add(float value, string id, int sortCode)
    {
        ModifyMultiplier(id, sortCode, additive: value);
    }

    /// <summary>
    /// 快捷方法：增减乘区数值
    /// </summary>
    /// <param name="value"></param>
    /// <param name="id"></param>
    public void AddModify(float value, string id)
    {
        ModifyMultiplier(id, GetMultiplierSortCode(id), additive: GetMultiplierValue(id,true) + value);
    }

    /// <summary>
    /// 快捷方法：增加基础乘区的乘数
    /// </summary>
    public void Multi(float multiplier)
    {
        ModifyMultiplier(BASE_ID,  BASE_SORT_CODE, multiplier: multiplier);
    }

    /// <summary>
    /// 快捷方法：添加或修改指定ID乘区的乘数
    /// 会自动创建乘区
    /// </summary>
    /// <param name="value">乘数</param>
    /// <param name="id">乘区ID</param>
    /// <param name="sortCode">排序标识码，乘区已存在时用于调整其计算顺序</param>
    public void Multi(float value, string id, int sortCode)
    {
        ModifyMultiplier(id, sortCode, multiplier: value);
    }

    /// <summary>
    /// 快捷方法：增减乘区数值
    /// </summary>
    /// <param name="value"></param>
    /// <param name="id"></param>
    public void MultiModify(float value, string id)
    {
        
        ModifyMultiplier(id, GetMultiplierSortCode(id), multiplier: GetMultiplierValue(id,false) + value);
    }

    /// <summary>
    /// 获取最终计算值。
    /// 结果按_revision标脏缓存，未发生改动时直接返回缓存。
    /// 按sortCode升序依次结算：result = (result + additive) * multiplier
    /// </summary>
    public float Get()
    {
        EnsureOrder();

        //baseValue被直接改动（如Inspector）时_revision不会变化，需额外比对
        if (_cachedRevision != _revision || !Mathf.IsEqualApprox(_cachedBaseValue, baseValue))
        {
            _cachedValue = Recalculate();
            _cachedBaseValue = baseValue;
            _cachedRevision = _revision;
        }

        onValueGet?.Invoke(_cachedValue);
        return _cachedValue;
    }

    /// <summary>
    /// 按乘区顺序实际结算
    /// </summary>
    private float Recalculate()
    {
        float result = baseValue;

        foreach (var id in _multiplierOrder)
        {
            if(!TryGetMultiplier(id, out ValueMultiplier multiplier))
            {
                continue;
            }
            result += multiplier.additive;
            result *= multiplier.multiplier;
        }

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
        _multipliers[BASE_ID] = new ValueMultiplier(BASE_ID, 0f, 1f, null, BASE_SORT_CODE);
        _multiplierOrder.RemoveAll(id => id != BASE_ID);
        MarkDirty();
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

        if (idsToRemove.Count > 0)
        {
            _multiplierOrder.RemoveAll(id => id != BASE_ID);
            MarkDirty();
        }
    }

    #region 运算符重载

    /// <summary>
    /// Value + float 相当于 Add
    /// </summary>
    public static Value operator +(Value value, float num)
    {
        value?.AddBase(num);
        return value;
    }

    /// <summary>
    /// float + Value 相当于 Add
    /// </summary>
    public static Value operator +(float num, Value value)
    {
        value?.AddBase(num);
        return value;
    }

    /// <summary>
    /// Value - float 相当于 Add(-float)
    /// </summary>
    public static Value operator -(Value value, float num)
    {
        value?.AddBase(-num);
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


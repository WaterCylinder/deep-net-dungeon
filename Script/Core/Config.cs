using Godot;
using Godot.Collections;

public class Config
{
    private const string DEFAULT_CONFIG_PATH = "res://Asset/Default/default_config.json";
    private const string USER_CONFIG_PATH = "user://config.json";

    /// <summary>
    /// 以嵌套 Dictionary/Array 形式保存的完整配置数据。
    /// </summary>
    private Dictionary _configData = new();

    /// <summary>
    /// 通过路径字符串获取配置值，支持点号和索引访问。
    /// 示例路径："item1.item2[1].item3"
    /// </summary>
    public Variant GetConfig(string path, Variant defaultValue = default)
    {
        if (string.IsNullOrEmpty(path))
        {
            return defaultValue;
        }

        string[] segments = path.Split('.');
        Variant current = Variant.From(_configData);

        foreach (string segment in segments)
        {
            if (!TryNavigate(ref current, segment))
            {
                return defaultValue;
            }
        }

        return current;
    }

    /// <summary>
    /// 通过路径字符串设置配置值，支持点号和索引访问。
    /// 示例路径："item1.item2[1].item3"
    /// 如果路径中的中间节点不存在则自动创建。
    /// </summary>
    public void SetConfig(string path, Variant value)
    {
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        string[] segments = path.Split('.');
        // 遍历到倒数第二层，确保中间节点存在
        Variant current = Variant.From(_configData);
        for (int i = 0; i < segments.Length - 1; i++)
        {
            EnsureNode(ref current, segments[i]);
        }

        // 在最终节点上设置值
        string lastSegment = segments[^1];
        SetNodeValue(ref current, lastSegment, value);
    }

    /// <summary>
    /// 加载用户配置文件。如果不存在或结构不一致，则从默认配置复制。
    /// </summary>
    public void LoadConfig()
    {
        Variant defaultData = LoadJsonFile(DEFAULT_CONFIG_PATH);
        if (defaultData.VariantType == Variant.Type.Nil)
        {
            GD.PrintErr("GameConfig: 无法加载默认配置文件: " + DEFAULT_CONFIG_PATH);
            return;
        }

        Dictionary defaultDict = defaultData.As<Dictionary>();
        Variant userData = LoadJsonFile(USER_CONFIG_PATH);

        if (userData.VariantType == Variant.Type.Nil)
        {
            // 用户配置不存在，复制默认配置
            GD.Print("GameConfig: 用户配置不存在，从默认配置创建。");
            _configData = DeepCopyDict(defaultDict);
            SaveConfig();
            return;
        }

        Dictionary userDict = userData.As<Dictionary>();

        if (!StructurallyEqual(defaultDict, userDict))
        {
            // 结构不一致，用默认配置覆盖
            GD.Print("GameConfig: 用户配置结构与默认不一致，重建用户配置。");
            _configData = DeepCopyDict(defaultDict);
            SaveConfig();
            return;
        }

        _configData = userDict;
        GD.Print("GameConfig: 用户配置加载成功。");
    }

    /// <summary>
    /// 保存当前配置到用户配置文件。
    /// </summary>
    public void SaveConfig()
    {
        string json = Json.Stringify(_configData, "\t");
        using var file = FileAccess.Open(USER_CONFIG_PATH, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            GD.PrintErr("GameConfig: 无法写入用户配置文件: " + USER_CONFIG_PATH);
            return;
        }
        file.StoreString(json);
        GD.Print("GameConfig: 配置已保存。");
    }

    /// <summary>
    /// 重置为默认配置并保存。
    /// </summary>
    public void ResetToDefault()
    {
        Variant defaultData = LoadJsonFile(DEFAULT_CONFIG_PATH);
        if (defaultData.VariantType != Variant.Type.Nil)
        {
            _configData = DeepCopyDict(defaultData.As<Dictionary>());
        }
        else
        {
            _configData.Clear();
        }
        SaveConfig();
    }

    # region 辅助方法

    /// <summary>
    /// 从指定路径加载 JSON 文件，返回解析后的 Variant。
    /// </summary>
    private static Variant LoadJsonFile(string path)
    {
        if (!FileAccess.FileExists(path))
        {
            return default;
        }

        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            return default;
        }

        string text = file.GetAsText();
        var json = new Json();
        Error error = json.Parse(text);
        if (error != Error.Ok)
        {
            GD.PrintErr($"GameConfig: JSON 解析失败 ({path}): {json.GetErrorMessage()}");
            return default;
        }

        return json.Data;
    }

    /// <summary>
    /// 深度比较两个 Dictionary 的结构（键名及嵌套结构），不比较值。
    /// </summary>
    private static bool StructurallyEqual(Dictionary a, Dictionary b)
    {
        if (a.Count != b.Count)
        {
            return false;
        }

        foreach (var key in a.Keys)
        {
            if (!b.ContainsKey(key))
            {
                return false;
            }

            Variant va = a[key];
            Variant vb = b[key];

            if (va.VariantType != vb.VariantType)
            {
                return false;
            }

            if (va.VariantType == Variant.Type.Dictionary)
            {
                if (!StructurallyEqual(va.As<Dictionary>(), vb.As<Dictionary>()))
                {
                    return false;
                }
            }
            else if (va.VariantType == Variant.Type.Array)
            {
                // 对于数组，比较第一个元素的类型（如果存在）
                Array arrA = va.As<Array>();
                Array arrB = vb.As<Array>();
                if (arrA.Count > 0 && arrB.Count > 0)
                {
                    if (arrA[0].VariantType != arrB[0].VariantType)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Dictionary 深拷贝。
    /// </summary>
    private static Dictionary DeepCopyDict(Dictionary source)
    {
        var copy = new Dictionary();
        foreach (var key in source.Keys)
        {
            copy[key] = DeepCopyVariant(source[key]);
        }
        return copy;
    }

    /// <summary>
    /// Variant 深拷贝（支持 Dictionary 和 Array 的递归拷贝）。
    /// </summary>
    private static Variant DeepCopyVariant(Variant value)
    {
        if (value.VariantType == Variant.Type.Dictionary)
        {
            return Variant.From(DeepCopyDict(value.As<Dictionary>()));
        }
        if (value.VariantType == Variant.Type.Array)
        {
            Array sourceArray = value.As<Array>();
            var copyArray = new Array();
            foreach (var item in sourceArray)
            {
                copyArray.Add(DeepCopyVariant(item));
            }
            return Variant.From(copyArray);
        }
        return value; // 基本类型直接赋值
    }

    /// <summary>
    /// 在当前节点上按一个路径段导航。返回 false 表示路径无效。
    /// </summary>
    private static bool TryNavigate(ref Variant current, string segment)
    {
        (string key, int index) = ParseSegment(segment);

        if (index >= 0)
        {
            // 数组索引访问
            if (!TryGetValueByKey(current, key, out Variant container))
            {
                return false;
            }
            if (container.VariantType != Variant.Type.Array)
            {
                return false;
            }
            Array arr = container.As<Array>();
            if (index >= arr.Count)
            {
                return false;
            }
            current = arr[index];
            return true;
        }

        // 字典键访问
        return TryGetValueByKey(current, key, out current);
    }

    /// <summary>
    /// 确保路径中间节点存在，不存在则自动创建 Dictionary。
    /// </summary>
    private static void EnsureNode(ref Variant current, string segment)
    {
        (string key, int index) = ParseSegment(segment);

        if (index >= 0)
        {
            // 数组索引：确保容器为 Array 且有足够长度
            if (!TryGetValueByKey(current, key, out Variant container))
            {
                return;
            }
            if (container.VariantType != Variant.Type.Array)
            {
                return;
            }
            Array arr = container.As<Array>();
            while (arr.Count <= index)
            {
                arr.Add(Variant.From(new Dictionary()));
            }
            current = arr[index];
            return;
        }

        // 字典键：确保嵌套 Dictionary 存在
        if (current.VariantType != Variant.Type.Dictionary)
        {
            return;
        }
        Dictionary dict = current.As<Dictionary>();
        if (!dict.ContainsKey(key) || dict[key].VariantType != Variant.Type.Dictionary)
        {
            dict[key] = Variant.From(new Dictionary());
        }
        current = dict[key];
    }

    /// <summary>
    /// 在最终节点上设置值。
    /// </summary>
    private static void SetNodeValue(ref Variant current, string segment, Variant value)
    {
        (string key, int index) = ParseSegment(segment);

        if (current.VariantType != Variant.Type.Dictionary)
        {
            return;
        }

        Dictionary dict = current.As<Dictionary>();

        if (index >= 0)
        {
            // 数组索引设值
            if (!dict.TryGetValue(key, out var container) || container.VariantType != Variant.Type.Array)
            {
                return;
            }
            Array arr = container.As<Array>();
            if (index < arr.Count)
            {
                arr[index] = value;
            }
            return;
        }

        // 字典键设值
        dict[key] = value;
    }

    /// <summary>
    /// 从当前 Variant（必须为 Dictionary）中通过键取值。
    /// </summary>
    private static bool TryGetValueByKey(Variant current, string key, out Variant value)
    {
        if (current.VariantType != Variant.Type.Dictionary)
        {
            value = default;
            return false;
        }
        Dictionary dict = current.As<Dictionary>();
        if (dict.TryGetValue(key, out value))
        {
            return true;
        }
        value = default;
        return false;
    }

    /// <summary>
    /// 解析路径段："key" 或 "key[index]"。
    /// 返回 (键名, 索引)，无索引时索引为 -1。
    /// </summary>
    private static (string key, int index) ParseSegment(string segment)
    {
        int bracketStart = segment.IndexOf('[');
        if (bracketStart > 0)
        {
            string key = segment[..bracketStart];
            int bracketEnd = segment.IndexOf(']', bracketStart);
            if (bracketEnd > bracketStart + 1)
            {
                string indexStr = segment.Substring(bracketStart + 1, bracketEnd - bracketStart - 1);
                if (int.TryParse(indexStr, out int index))
                {
                    return (key, index);
                }
            }
        }
        return (segment, -1);
    }

    # endregion
}

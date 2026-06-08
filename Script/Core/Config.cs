using Godot;
using Godot.Collections;

/// <summary>
/// 通用配置管理器。
/// 
/// 支持多个非同名配置文件，提供统一的加载/保存/存取接口。
/// 配置文件命名规则：
///   默认模板：res://Asset/Default/default_{name}.json
///   用户配置：user://{name}.json
/// 
/// 加载逻辑：
///   1. 用户配置存在且结构与默认一致 → 使用用户配置
///   2. 否则 → 从默认模板复制到 user:// 目录
/// </summary>
public class Config
{
    private const string DEFAULT_CONFIG_DIR = "res://Asset/Default/";
    private const string USER_CONFIG_DIR = "user://";

    /// <summary>
    /// 以配置名称为 key，存储各配置的 Dictionary 数据
    /// </summary>
    private Dictionary<string, Dictionary> _configs = new();

    /// <summary>
    /// 默认配置名称（用于无参重载）
    /// </summary>
    private const string DEFAULT_CONFIG_NAME = "config";

    // ========== 便捷方法（使用默认配置名 "config"） ==========

    /// <summary>
    /// 从默认配置（config.json）获取值，支持点号和索引访问。
    /// </summary>
    public Variant GetConfig(string path, Variant defaultValue = default)
    {
        return GetConfig(DEFAULT_CONFIG_NAME, path, defaultValue);
    }

    /// <summary>
    /// 向默认配置（config.json）设置值，支持点号和索引访问。
    /// </summary>
    public void SetConfig(string path, Variant value)
    {
        SetConfig(DEFAULT_CONFIG_NAME, path, value);
    }

    /// <summary>
    /// 加载默认配置（config.json）。如果用户目录不存在则从 default_config.json 复制。
    /// </summary>
    public void LoadConfig()
    {
        LoadConfig(DEFAULT_CONFIG_NAME);
    }

    /// <summary>
    /// 保存默认配置到 user://config.json。
    /// </summary>
    public void SaveConfig()
    {
        SaveConfig(DEFAULT_CONFIG_NAME);
    }

    /// <summary>
    /// 重置默认配置为默认模板。
    /// </summary>
    public void ResetToDefault()
    {
        ResetToDefault(DEFAULT_CONFIG_NAME);
    }

    // ========== 核心方法（支持多配置） ==========

    /// <summary>
    /// 加载指定名称的配置。
    /// 如果 user://{name}.json 不存在或结构不一致，则从 res://Asset/Default/default_{name}.json 复制。
    /// </summary>
    /// <param name="name">配置名称（不含路径和后缀），如 "config"、"key_mapper"</param>
    public void LoadConfig(string name)
    {
        string defaultPath = $"{DEFAULT_CONFIG_DIR}default_{name}.json";
        string userPath = $"{USER_CONFIG_DIR}{name}.json";

        Variant defaultData = LoadJsonFile(defaultPath);
        if (defaultData.VariantType == Variant.Type.Nil)
        {
            GD.PrintErr($"[Config] 无法加载默认配置模板: {defaultPath}");
            return;
        }

        Dictionary defaultDict = defaultData.As<Dictionary>();
        Variant userData = LoadJsonFile(userPath);

        if (userData.VariantType == Variant.Type.Nil)
        {
            // 用户配置不存在，复制默认配置
            GD.Print($"[Config] '{name}' 用户配置不存在，从默认模板创建。");
            _configs[name] = DeepCopyDict(defaultDict);
            SaveConfig(name);
            return;
        }

        Dictionary userDict = userData.As<Dictionary>();

        if (!StructurallyEqual(defaultDict, userDict))
        {
            // 结构不一致，用默认配置覆盖
            GD.Print($"[Config] '{name}' 用户配置结构与默认不一致，重建用户配置。");
            _configs[name] = DeepCopyDict(defaultDict);
            SaveConfig(name);
            return;
        }

        _configs[name] = userDict;
        GD.Print($"[Config] '{name}' 用户配置加载成功。");
    }

    /// <summary>
    /// 从指定名称的配置中获取值，支持点号和索引访问。
    /// 示例路径："item1.item2[1].item3"
    /// </summary>
    /// <param name="name">配置名称</param>
    /// <param name="path">点号分隔的路径</param>
    /// <param name="defaultValue">路径无效时的默认返回值</param>
    public Variant GetConfig(string name, string path, Variant defaultValue = default)
    {
        if (string.IsNullOrEmpty(path))
        {
            return defaultValue;
        }

        if (!_configs.TryGetValue(name, out var configData))
        {
            return defaultValue;
        }

        string[] segments = path.Split('.');
        Variant current = Variant.From(configData);

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
    /// 向指定名称的配置中设置值，支持点号和索引访问。
    /// 示例路径："item1.item2[1].item3"
    /// 如果路径中的中间节点不存在则自动创建。
    /// </summary>
    /// <param name="name">配置名称</param>
    /// <param name="path">点号分隔的路径</param>
    /// <param name="value">要设置的值</param>
    public void SetConfig(string name, string path, Variant value)
    {
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        if (!_configs.TryGetValue(name, out var configData))
        {
            // 配置未加载，自动初始化空字典
            configData = new Dictionary();
            _configs[name] = configData;
        }

        string[] segments = path.Split('.');
        // 遍历到倒数第二层，确保中间节点存在
        Variant current = Variant.From(configData);
        for (int i = 0; i < segments.Length - 1; i++)
        {
            EnsureNode(ref current, segments[i]);
        }

        // 在最终节点上设置值
        string lastSegment = segments[^1];
        SetNodeValue(ref current, lastSegment, value);
    }

    /// <summary>
    /// 保存指定名称的配置到 user://{name}.json。
    /// </summary>
    /// <param name="name">配置名称</param>
    public void SaveConfig(string name)
    {
        if (!_configs.TryGetValue(name, out var configData))
        {
            GD.PrintErr($"[Config] 配置 '{name}' 未加载，无法保存。");
            return;
        }

        string userPath = $"{USER_CONFIG_DIR}{name}.json";
        string json = Json.Stringify(configData, "\t");
        using var file = FileAccess.Open(userPath, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            GD.PrintErr($"[Config] 无法写入用户配置文件: {userPath}");
            return;
        }
        file.StoreString(json);
        GD.Print($"[Config] '{name}' 配置已保存。");
    }

    /// <summary>
    /// 将指定名称的配置重置为默认模板。
    /// </summary>
    /// <param name="name">配置名称</param>
    public void ResetToDefault(string name)
    {
        string defaultPath = $"{DEFAULT_CONFIG_DIR}default_{name}.json";
        Variant defaultData = LoadJsonFile(defaultPath);
        if (defaultData.VariantType != Variant.Type.Nil)
        {
            _configs[name] = DeepCopyDict(defaultData.As<Dictionary>());
        }
        else
        {
            _configs.Remove(name);
        }
        SaveConfig(name);
    }

    /// <summary>
    /// 检查指定名称的配置是否已加载。
    /// </summary>
    public bool IsConfigLoaded(string name)
    {
        return _configs.ContainsKey(name);
    }

    #region 辅助方法

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
            GD.PrintErr($"[Config] JSON 解析失败 ({path}): {json.GetErrorMessage()}");
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

    #endregion
}
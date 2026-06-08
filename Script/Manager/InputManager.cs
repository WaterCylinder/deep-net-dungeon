using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// 输入管理器 - 管理全局输入和输入键位，支持多平台
/// 
/// 配置：
///   config.json 中的 inputMode 控制操作模式：
///     PC平台: "KeyboardMouse" / "Gamepad"
///     移动端: "Touch"（暂未实现，预留功能）
///   key_mapper.json 存储在 user:// 目录下，定义具体键位映射
/// </summary>
public partial class InputManager : ManagerBase
{
    public static InputManager Instance { get; private set; }

    /// <summary>
    /// 输入模式枚举
    /// </summary>
    public enum InputMode
    {
        KeyboardMouse,  // 键鼠模式
        Gamepad,        // 手柄模式
        Touch           // 触屏模式（预留）
    }

    /// <summary>
    /// 输入动作名称常量
    /// </summary>
    public static class Actions
    {
        public const string MoveUp = "move_up";
        public const string MoveDown = "move_down";
        public const string MoveLeft = "move_left";
        public const string MoveRight = "move_right";
        public const string ActiveSkill = "active_skill";
        public const string Menu = "menu";
        public const string Confirm = "confirm";
    }

    /// <summary>
    /// 当前输入模式
    /// </summary>
    public InputMode CurrentMode { get; private set; } = InputMode.KeyboardMouse;

    /// <summary>
    /// 当前平台的默认输入模式
    /// </summary>
    public InputMode DefaultMode { get; private set; } = InputMode.KeyboardMouse;

    /// <summary>
    /// 键位映射配置字典（动作名 -> 键位列表）
    /// </summary>
    private Dictionary<string, List<Key>> _keyMapping = new();

    /// <summary>
    /// 手柄按钮映射配置字典（动作名 -> 按钮列表）
    /// </summary>
    private Dictionary<string, List<JoyButton>> _gamepadMapping = new();

    /// <summary>
    /// key_mapper.json 配置文件路径
    /// </summary>
    private const string KEY_MAPPER_PATH = "user://key_mapper.json";

    /// <summary>
    /// config.json 配置文件路径
    /// </summary>
    private const string CONFIG_PATH = "user://config.json";

    /// <summary>
    /// 默认键位映射（PC键鼠）
    /// </summary>
    private static readonly Dictionary<string, List<Key>> DefaultKeyMapping = new()
    {
        { Actions.MoveUp, new List<Key> { Key.W, Key.Up } },
        { Actions.MoveDown, new List<Key> { Key.S, Key.Down } },
        { Actions.MoveLeft, new List<Key> { Key.A, Key.Left } },
        { Actions.MoveRight, new List<Key> { Key.D, Key.Right } },
        { Actions.ActiveSkill, new List<Key> { (Key)1 } },
        { Actions.Menu, new List<Key> { Key.Escape } },
        { Actions.Confirm, new List<Key> { Key.Enter, Key.KpEnter } }
    };

    /// <summary>
    /// 默认手柄按钮映射
    /// </summary>
    private static readonly Dictionary<string, List<JoyButton>> DefaultGamepadMapping = new()
    {
        { Actions.MoveUp, new List<JoyButton> { JoyButton.DpadUp } },
        { Actions.MoveDown, new List<JoyButton> { JoyButton.DpadDown } },
        { Actions.MoveLeft, new List<JoyButton> { JoyButton.DpadLeft } },
        { Actions.MoveRight, new List<JoyButton> { JoyButton.DpadRight } },
        { Actions.ActiveSkill, new List<JoyButton> { JoyButton.RightShoulder } },
        { Actions.Menu, new List<JoyButton> { JoyButton.Start } },
        { Actions.Confirm, new List<JoyButton> { JoyButton.A } }
    };

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    public override void Init()
    {
        // 自动检测平台默认模式
        DefaultMode = DetectPlatformDefaultMode();

        // 从 config.json 加载 inputMode
        LoadInputModeFromConfig();

        // 加载键位配置
        LoadKeyMapping();

        // 注册 Godot InputMap
        RegisterInputActions();

        Log($"输入管理器初始化完成，当前模式: {CurrentMode}");
    }

    public override void Cleanup()
    {
        Log("输入管理器已清理");
    }

    /// <summary>
    /// 检测当前平台默认的输入模式
    /// </summary>
    private InputMode DetectPlatformDefaultMode()
    {
        string osName = OS.GetName();

        if (osName == "Android" || osName == "iOS")
        {
            return InputMode.Touch;
        }

        return InputMode.KeyboardMouse;
    }

    /// <summary>
    /// 从 config.json 加载 inputMode 配置
    /// </summary>
    private void LoadInputModeFromConfig()
    {
        if (!FileAccess.FileExists(CONFIG_PATH))
        {
            CurrentMode = DefaultMode;
            Log($"config.json 不存在，使用默认模式: {DefaultMode}");
            return;
        }

        try
        {
            using var file = FileAccess.Open(CONFIG_PATH, FileAccess.ModeFlags.Read);
            if (file == null)
            {
                CurrentMode = DefaultMode;
                return;
            }

            string jsonText = file.GetAsText();
            var configData = Json.ParseString(jsonText);
            if (configData.VariantType == Variant.Type.Dictionary)
            {
                var configDict = configData.AsGodotDictionary();
                if (configDict.ContainsKey("inputMode"))
                {
                    string modeStr = configDict["inputMode"].AsString();
                    if (Enum.TryParse(modeStr, true, out InputMode parsedMode))
                    {
                        CurrentMode = parsedMode;
                        Log($"从 config.json 加载输入模式: {CurrentMode}");
                        return;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Log($"加载 config.json 失败: {e.Message}");
        }

        CurrentMode = DefaultMode;
    }

    /// <summary>
    /// 加载键位映射配置
    /// 优先从 user://key_mapper.json 加载，不存在则使用默认配置
    /// </summary>
    private void LoadKeyMapping()
    {
        if (FileAccess.FileExists(KEY_MAPPER_PATH))
        {
            try
            {
                using var file = FileAccess.Open(KEY_MAPPER_PATH, FileAccess.ModeFlags.Read);
                if (file != null)
                {
                    string jsonText = file.GetAsText();
                    var data = Json.ParseString(jsonText);
                    if (data.VariantType == Variant.Type.Dictionary)
                    {
                        var dict = data.AsGodotDictionary();
                        ParseKeyMappingFromJson(dict);
                        ParseGamepadMappingFromJson(dict);
                        Log("从 key_mapper.json 加载键位配置");
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Log($"加载 key_mapper.json 失败: {e.Message}，使用默认配置");
            }
        }

        // 使用默认配置
        _keyMapping = new Dictionary<string, List<Key>>(DefaultKeyMapping);
        _gamepadMapping = new Dictionary<string, List<JoyButton>>(DefaultGamepadMapping);
        SaveKeyMapping();
        Log("使用默认键位配置");
    }

    /// <summary>
    /// 从 JSON 字典解析键盘映射
    /// </summary>
    private void ParseKeyMappingFromJson(Godot.Collections.Dictionary dict)
    {
        _keyMapping = new Dictionary<string, List<Key>>();
        if (dict.ContainsKey("keyboard"))
        {
            var keyboardDict = dict["keyboard"].AsGodotDictionary();
            foreach (var kvp in keyboardDict)
            {
                string action = kvp.Key.AsString();
                var keyList = new List<Key>();
                foreach (var keyVar in kvp.Value.AsGodotArray())
                {
                    if (int.TryParse(keyVar.AsString(), out int keyCode))
                    {
                        keyList.Add((Key)keyCode);
                    }
                    else if (Enum.TryParse(keyVar.AsString(), true, out Key parsedKey))
                    {
                        keyList.Add(parsedKey);
                    }
                }
                if (keyList.Count > 0)
                {
                    _keyMapping[action] = keyList;
                }
            }
        }

        // 填充缺失的默认值
        foreach (var kvp in DefaultKeyMapping)
        {
            if (!_keyMapping.ContainsKey(kvp.Key))
            {
                _keyMapping[kvp.Key] = new List<Key>(kvp.Value);
            }
        }
    }

    /// <summary>
    /// 从 JSON 字典解析手柄映射
    /// </summary>
    private void ParseGamepadMappingFromJson(Godot.Collections.Dictionary dict)
    {
        _gamepadMapping = new Dictionary<string, List<JoyButton>>();
        if (dict.ContainsKey("gamepad"))
        {
            var gamepadDict = dict["gamepad"].AsGodotDictionary();
            foreach (var kvp in gamepadDict)
            {
                string action = kvp.Key.AsString();
                var buttonList = new List<JoyButton>();
                foreach (var btnVar in kvp.Value.AsGodotArray())
                {
                    if (int.TryParse(btnVar.AsString(), out int btnCode))
                    {
                        buttonList.Add((JoyButton)btnCode);
                    }
                    else if (Enum.TryParse(btnVar.AsString(), true, out JoyButton parsedBtn))
                    {
                        buttonList.Add(parsedBtn);
                    }
                }
                if (buttonList.Count > 0)
                {
                    _gamepadMapping[action] = buttonList;
                }
            }
        }

        // 填充缺失的默认值
        foreach (var kvp in DefaultGamepadMapping)
        {
            if (!_gamepadMapping.ContainsKey(kvp.Key))
            {
                _gamepadMapping[kvp.Key] = new List<JoyButton>(kvp.Value);
            }
        }
    }

    /// <summary>
    /// 保存键位映射到 user://key_mapper.json
    /// </summary>
    public void SaveKeyMapping()
    {
        try
        {
            var rootDict = new Godot.Collections.Dictionary();

            // 序列化键盘映射
            var keyboardDict = new Godot.Collections.Dictionary();
            foreach (var kvp in _keyMapping)
            {
                var keyArray = new Godot.Collections.Array();
                foreach (var key in kvp.Value)
                {
                    keyArray.Add(((int)key).ToString());
                }
                keyboardDict[kvp.Key] = keyArray;
            }
            rootDict["keyboard"] = keyboardDict;

            // 序列化手柄映射
            var gamepadDict = new Godot.Collections.Dictionary();
            foreach (var kvp in _gamepadMapping)
            {
                var btnArray = new Godot.Collections.Array();
                foreach (var btn in kvp.Value)
                {
                    btnArray.Add(((int)btn).ToString());
                }
                gamepadDict[kvp.Key] = btnArray;
            }
            rootDict["gamepad"] = gamepadDict;

            string jsonText = Json.Stringify(rootDict, "\t");
            using var file = FileAccess.Open(KEY_MAPPER_PATH, FileAccess.ModeFlags.Write);
            if (file != null)
            {
                file.StoreString(jsonText);
                Log("键位配置已保存");
            }
        }
        catch (Exception e)
        {
            Log($"保存键位配置失败: {e.Message}");
        }
    }

    /// <summary>
    /// 将当前键位映射注册到 Godot InputMap
    /// </summary>
    private void RegisterInputActions()
    {
        var fields = typeof(Actions).GetFields(
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        foreach (var field in fields)
        {
            string actionName = (string)field.GetValue(null);

            if (!InputMap.HasAction(actionName))
            {
                InputMap.AddAction(actionName);
            }

            // 清空原有按键绑定
            InputMap.ActionEraseEvents(actionName);

            // 注册键盘按键
            if (_keyMapping.TryGetValue(actionName, out var keys))
            {
                foreach (var key in keys)
                {
                    var inputEvent = new InputEventKey();
                    inputEvent.Keycode = key;
                    InputMap.ActionAddEvent(actionName, inputEvent);
                }
            }

            // 注册手柄按钮
            if (_gamepadMapping.TryGetValue(actionName, out var buttons))
            {
                foreach (var btn in buttons)
                {
                    var inputEvent = new InputEventJoypadButton();
                    inputEvent.ButtonIndex = btn;
                    InputMap.ActionAddEvent(actionName, inputEvent);
                }
            }
        }
    }

    /// <summary>
    /// 设置输入模式
    /// </summary>
    public void SetInputMode(InputMode mode)
    {
        if (CurrentMode == mode) return;

        // 移动端触摸模式暂未实现
        if (mode == InputMode.Touch)
        {
            Log("触摸模式暂未实现，保持当前模式");
            return;
        }

        CurrentMode = mode;
        Log($"切换输入模式为: {mode}");

        // 更新 config.json 中的 inputMode
        UpdateConfigInputMode();
    }

    /// <summary>
    /// 更新 config.json 中的 inputMode
    /// </summary>
    private void UpdateConfigInputMode()
    {
        try
        {
            var configDict = new Godot.Collections.Dictionary();
            if (FileAccess.FileExists(CONFIG_PATH))
            {
                using var file = FileAccess.Open(CONFIG_PATH, FileAccess.ModeFlags.Read);
                if (file != null)
                {
                    string jsonText = file.GetAsText();
                    var data = Json.ParseString(jsonText);
                    if (data.VariantType == Variant.Type.Dictionary)
                    {
                        configDict = data.AsGodotDictionary();
                    }
                }
            }

            configDict["inputMode"] = CurrentMode.ToString();

            using var writeFile = FileAccess.Open(CONFIG_PATH, FileAccess.ModeFlags.Write);
            if (writeFile != null)
            {
                writeFile.StoreString(Json.Stringify(configDict, "\t"));
            }
        }
        catch (Exception e)
        {
            Log($"保存 config.json 失败: {e.Message}");
        }
    }

    /// <summary>
    /// 检查指定动作是否被按下（当前帧）
    /// </summary>
    public bool IsActionPressed(string action)
    {
        return Input.IsActionPressed(action);
    }

    /// <summary>
    /// 检查指定动作是否刚刚被按下（按下瞬间）
    /// </summary>
    public bool IsActionJustPressed(string action)
    {
        return Input.IsActionJustPressed(action);
    }

    /// <summary>
    /// 检查指定动作是否被释放
    /// </summary>
    public bool IsActionJustReleased(string action)
    {
        return Input.IsActionJustReleased(action);
    }

    /// <summary>
    /// 获取指定动作的输入强度（0-1）
    /// </summary>
    public float GetActionStrength(string action)
    {
        return Input.GetActionStrength(action);
    }

    /// <summary>
    /// 获取移动方向向量（Vector2）
    /// </summary>
    public Vector2 GetMoveDirection()
    {
        Vector2 direction = Vector2.Zero;
        direction.Y += Input.GetActionStrength(Actions.MoveDown) - Input.GetActionStrength(Actions.MoveUp);
        direction.X += Input.GetActionStrength(Actions.MoveRight) - Input.GetActionStrength(Actions.MoveLeft);
        return direction.Normalized();
    }

    /// <summary>
    /// 重新绑定指定动作的键盘按键
    /// </summary>
    public void RebindKey(string action, List<Key> newKeys)
    {
        if (!_keyMapping.ContainsKey(action))
        {
            Log($"动作 '{action}' 不存在");
            return;
        }

        _keyMapping[action] = newKeys;
        RegisterInputActions();
        SaveKeyMapping();
        Log($"动作 '{action}' 键位已更新");
    }

    /// <summary>
    /// 重新绑定指定动作的手柄按钮
    /// </summary>
    public void RebindGamepadButton(string action, List<JoyButton> newButtons)
    {
        if (!_gamepadMapping.ContainsKey(action))
        {
            Log($"动作 '{action}' 不存在");
            return;
        }

        _gamepadMapping[action] = newButtons;
        RegisterInputActions();
        SaveKeyMapping();
        Log($"动作 '{action}' 手柄键位已更新");
    }

    /// <summary>
    /// 获取当前键位映射（深拷贝，用于UI显示）
    /// </summary>
    public Dictionary<string, List<Key>> GetKeyMapping()
    {
        var copy = new Dictionary<string, List<Key>>();
        foreach (var kvp in _keyMapping)
        {
            copy[kvp.Key] = new List<Key>(kvp.Value);
        }
        return copy;
    }

    /// <summary>
    /// 获取当前手柄映射（深拷贝，用于UI显示）
    /// </summary>
    public Dictionary<string, List<JoyButton>> GetGamepadMapping()
    {
        var copy = new Dictionary<string, List<JoyButton>>();
        foreach (var kvp in _gamepadMapping)
        {
            copy[kvp.Key] = new List<JoyButton>(kvp.Value);
        }
        return copy;
    }

    /// <summary>
    /// 处理输入更新（每帧在 _Process 或 _PhysicsProcess 中调用）
    /// 在此处进行触屏等模式的输入处理（预留）
    /// </summary>
    public override void _Process(double delta)
    {
        switch (CurrentMode)
        {
            case InputMode.Touch:
                HandleTouchInput(delta);
                break;
        }
    }

    /// <summary>
    /// 处理触屏输入（预留，暂未实现）
    /// </summary>
    private void HandleTouchInput(double delta)
    {
        // TODO: 实现触摸屏虚拟摇杆和按钮的输入处理
        // - 虚拟摇杆控制移动方向
        // - 虚拟按钮映射各技能和操作
    }
}
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
///   键位映射通过 Config 加载 key_mapper 配置
///     默认模板：res://Asset/Default/default_key_mapper.json
///     用户配置：user://key_mapper.json（优先读取）
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
        public const string OtherSkill = "other_skill";
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
    /// Config 实例，由 GameManager 注入
    /// </summary>
    private Config _config;

    /// <summary>
    /// key_mapper 配置名称
    /// </summary>
    private const string KEY_MAPPER_CONFIG_NAME = "key_mapper";

    /// <summary>
    /// 默认键位映射（PC键鼠）—— 用于 Config 未加载时的后备
    /// </summary>
    private static readonly Dictionary<string, List<Key>> FallbackKeyMapping = new()
    {
        { Actions.MoveUp, new List<Key> { Key.W, Key.Up } },
        { Actions.MoveDown, new List<Key> { Key.S, Key.Down } },
        { Actions.MoveLeft, new List<Key> { Key.A, Key.Left } },
        { Actions.MoveRight, new List<Key> { Key.D, Key.Right } },
        { Actions.ActiveSkill, new List<Key> { (Key)1 } },   // 鼠标左键
        { Actions.OtherSkill, new List<Key> { (Key)2 } },    // 鼠标右键
        { Actions.Menu, new List<Key> { Key.Escape } },
        { Actions.Confirm, new List<Key> { Key.Enter, Key.KpEnter } }
    };

    /// <summary>
    /// 默认手柄按钮映射 —— 用于 Config 未加载时的后备
    /// </summary>
    private static readonly Dictionary<string, List<JoyButton>> FallbackGamepadMapping = new()
    {
        { Actions.MoveUp, new List<JoyButton> { JoyButton.DpadUp } },
        { Actions.MoveDown, new List<JoyButton> { JoyButton.DpadDown } },
        { Actions.MoveLeft, new List<JoyButton> { JoyButton.DpadLeft } },
        { Actions.MoveRight, new List<JoyButton> { JoyButton.DpadRight } },
        { Actions.ActiveSkill, new List<JoyButton> { JoyButton.RightShoulder } },
        { Actions.OtherSkill, new List<JoyButton> { JoyButton.B } },
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

        // 获取 Config 引用（通过 GameManager 持有或新建）
        InitializeConfig();

        // 从 config.json 加载 inputMode
        LoadInputModeFromConfig();

        // 加载键位映射（优先从 Config 读取）
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
    /// 获取或创建 Config 实例
    /// </summary>
    private void InitializeConfig()
    {
        // 优先从 GameManager 获取 Config
        if (GameManager.Instance != null)
        {
            _config = GameManager.Instance.config;
        }

        // 如果 GameManager 未提供，则新建并使用默认值
        if (_config == null)
        {
            _config = new Config();
            _config.LoadConfig();              // 加载 config 配置
            _config.LoadConfig(KEY_MAPPER_CONFIG_NAME);  // 加载 key_mapper 配置
        }
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
    /// 从 config.json 加载 inputMode 配置（通过 Config）
    /// </summary>
    private void LoadInputModeFromConfig()
    {
        if (_config == null || !_config.IsConfigLoaded("config"))
        {
            CurrentMode = DefaultMode;
            return;
        }

        Variant modeVar = _config.GetConfig("inputMode");
        if (modeVar.VariantType == Variant.Type.String)
        {
            string modeStr = modeVar.AsString();
            if (Enum.TryParse(modeStr, true, out InputMode parsedMode))
            {
                CurrentMode = parsedMode;
                Log($"从 config.json 加载输入模式: {CurrentMode}");
                return;
            }
        }

        CurrentMode = DefaultMode;
    }

    /// <summary>
    /// 加载键位映射配置
    /// 优先从 Config(key_mapper) 读取，未加载则使用后备默认值
    /// </summary>
    private void LoadKeyMapping()
    {
        if (_config != null && _config.IsConfigLoaded(KEY_MAPPER_CONFIG_NAME))
        {
            ParseKeyMappingFromConfig();
            ParseGamepadMappingFromConfig();
            Log("从 Config(key_mapper) 加载键位配置");
            return;
        }

        // 后备：使用硬编码默认值
        _keyMapping = CloneKeyMapping(FallbackKeyMapping);
        _gamepadMapping = CloneGamepadMapping(FallbackGamepadMapping);
        Log("使用后备默认键位配置");
    }

    /// <summary>
    /// 从 Config 解析键盘映射
    /// 格式: keyboard.{action} → Variant.Array of keycode strings
    /// </summary>
    private void ParseKeyMappingFromConfig()
    {
        _keyMapping = new Dictionary<string, List<Key>>();

        foreach (var kvp in FallbackKeyMapping)
        {
            string action = kvp.Key;
            Variant varKeys = _config.GetConfig(KEY_MAPPER_CONFIG_NAME, $"keyboard.{action}");

            if (varKeys.VariantType == Variant.Type.Array)
            {
                var list = new List<Key>();
                foreach (var k in varKeys.AsGodotArray())
                {
                    if (int.TryParse(k.AsString(), out int keyCode))
                    {
                        list.Add((Key)keyCode);
                    }
                }
                if (list.Count > 0)
                {
                    _keyMapping[action] = list;
                    continue;
                }
            }

            // 配置中缺失该动作，使用后备值
            _keyMapping[action] = new List<Key>(kvp.Value);
        }
    }

    /// <summary>
    /// 从 Config 解析手柄映射
    /// 格式: gamepad.{action} → Variant.Array of button code strings
    /// </summary>
    private void ParseGamepadMappingFromConfig()
    {
        _gamepadMapping = new Dictionary<string, List<JoyButton>>();

        foreach (var kvp in FallbackGamepadMapping)
        {
            string action = kvp.Key;
            Variant varBtns = _config.GetConfig(KEY_MAPPER_CONFIG_NAME, $"gamepad.{action}");

            if (varBtns.VariantType == Variant.Type.Array)
            {
                var list = new List<JoyButton>();
                foreach (var b in varBtns.AsGodotArray())
                {
                    if (int.TryParse(b.AsString(), out int btnCode))
                    {
                        list.Add((JoyButton)btnCode);
                    }
                }
                if (list.Count > 0)
                {
                    _gamepadMapping[action] = list;
                    continue;
                }
            }

            // 配置中缺失该动作，使用后备值
            _gamepadMapping[action] = new List<JoyButton>(kvp.Value);
        }
    }

    /// <summary>
    /// 保存键位映射到 Config(key_mapper) 并写入 user://key_mapper.json
    /// </summary>
    public void SaveKeyMapping()
    {
        if (_config == null) return;

        // 确保 key_mapper 配置已加载
        if (!_config.IsConfigLoaded(KEY_MAPPER_CONFIG_NAME))
        {
            _config.LoadConfig(KEY_MAPPER_CONFIG_NAME);
        }

        // 写入键盘映射
        foreach (var kvp in _keyMapping)
        {
            var arr = new Godot.Collections.Array();
            foreach (var key in kvp.Value)
            {
                arr.Add(((int)key).ToString());
            }
            _config.SetConfig(KEY_MAPPER_CONFIG_NAME, $"keyboard.{kvp.Key}", arr);
        }

        // 写入手柄映射
        foreach (var kvp in _gamepadMapping)
        {
            var arr = new Godot.Collections.Array();
            foreach (var btn in kvp.Value)
            {
                arr.Add(((int)btn).ToString());
            }
            _config.SetConfig(KEY_MAPPER_CONFIG_NAME, $"gamepad.{kvp.Key}", arr);
        }

        _config.SaveConfig(KEY_MAPPER_CONFIG_NAME);
        Log("键位配置已保存");
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

            // 注册键盘/鼠标按键
            if (_keyMapping.TryGetValue(actionName, out var keys))
            {
                foreach (var key in keys)
                {
                    int keyCode = (int)key;
                    // 鼠标按键 (Key 值 1-9 对应鼠标按键)
                    if (keyCode >= 1 && keyCode <= 9)
                    {
                        var inputEvent = new InputEventMouseButton();
                        inputEvent.ButtonIndex = (MouseButton)keyCode;
                        InputMap.ActionAddEvent(actionName, inputEvent);
                    }
                    else
                    {
                        var inputEvent = new InputEventKey();
                        inputEvent.Keycode = key;
                        InputMap.ActionAddEvent(actionName, inputEvent);
                    }
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

        // 更新 config.json 中的 inputMode（通过 Config）
        if (_config != null)
        {
            _config.SetConfig("inputMode", CurrentMode.ToString());
            _config.SaveConfig();
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
    /// 获取从指定位置到鼠标位置的向量（仅键鼠模式有效，世界坐标系）
    /// </summary>
    /// <param name="fromPosition">起始位置（世界坐标）</param>
    /// <returns>从起始位置指向鼠标世界位置的向量，非键鼠模式返回Vector2.Zero</returns>
    public Vector2 GetDirectionToMouse(Vector2 fromPosition)
    {
        if (CurrentMode != InputMode.KeyboardMouse)
        {
            return Vector2.Zero;
        }

        Vector2 mouseWorldPosition = GetMouseWorldPosition();
        return (mouseWorldPosition - fromPosition).Normalized();
    }

    /// <summary>
    /// 获取鼠标在屏幕上的位置（仅键鼠模式有效）
    /// </summary>
    /// <returns>鼠标屏幕位置，非键鼠模式返回Vector2.Zero</returns>
    public Vector2 GetMousePosition()
    {
        if (CurrentMode != InputMode.KeyboardMouse)
        {
            return Vector2.Zero;
        }

        return GetViewport().GetMousePosition();
    }

    /// <summary>
    /// 获取鼠标在世界坐标系中的位置（仅键鼠模式有效）
    /// </summary>
    /// <returns>鼠标世界坐标位置，非键鼠模式返回Vector2.Zero</returns>
    public Vector2 GetMouseWorldPosition()
    {
        if (CurrentMode != InputMode.KeyboardMouse)
        {
            return Vector2.Zero;
        }

        // 获取屏幕坐标
        Vector2 screenPos = GetViewport().GetMousePosition();
        // 使用CanvasTransform将屏幕坐标转换为世界坐标
        Transform2D canvasTransform = GetViewport().CanvasTransform;
        return canvasTransform.AffineInverse() * screenPos;
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
        return CloneKeyMapping(_keyMapping);
    }

    /// <summary>
    /// 获取当前手柄映射（深拷贝，用于UI显示）
    /// </summary>
    public Dictionary<string, List<JoyButton>> GetGamepadMapping()
    {
        return CloneGamepadMapping(_gamepadMapping);
    }

    #region 辅助方法

    private static Dictionary<string, List<Key>> CloneKeyMapping(Dictionary<string, List<Key>> source)
    {
        var copy = new Dictionary<string, List<Key>>();
        foreach (var kvp in source)
        {
            copy[kvp.Key] = new List<Key>(kvp.Value);
        }
        return copy;
    }

    private static Dictionary<string, List<JoyButton>> CloneGamepadMapping(Dictionary<string, List<JoyButton>> source)
    {
        var copy = new Dictionary<string, List<JoyButton>>();
        foreach (var kvp in source)
        {
            copy[kvp.Key] = new List<JoyButton>(kvp.Value);
        }
        return copy;
    }

    #endregion

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
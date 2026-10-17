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
/// 
/// 输入流程：
///   所有动作的按下/松开状态统一由本管理器对外提供（IsActionPressed 等接口），
///   内部按输入源区分处理：
///     鼠标按键 —— 通过 _UnhandledInput 派发，Control 的 mouse_filter 为 Stop 时会截停事件，
///                 因此点击 UI 不会误触发游戏内动作；Pass / Ignore 的 Control 不会截停。
///     键盘/手柄 —— 同样通过 _UnhandledInput 记录按下，再与原始按键状态校验，
///                 既能被 UI（如输入框）截停，也不会因丢失松开事件导致长按状态卡死。
///   动作状态判定 = 收到过未被截停的按下事件 && 绑定输入源当前仍处于按下状态。
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
        public const string Zoom = "zoom";
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
    /// 动作绑定的输入源缓存（动作名 -> 输入源），与 InputMap 注册的键位保持一致
    /// </summary>
    private readonly Dictionary<string, ActionSources> _actionSources = new();

    /// <summary>
    /// 已收到未被 UI 截停的按下事件的动作集合（由 _UnhandledInput 维护）
    /// </summary>
    private readonly HashSet<string> _unhandledPressedActions = new();

    /// <summary>
    /// 动作最近一次按下所在的主循环帧号（用于 JustPressed 判断）
    /// </summary>
    private readonly Dictionary<string, long> _actionPressFrame = new();

    /// <summary>
    /// 动作最近一次松开所在的主循环帧号（用于 JustReleased 判断）
    /// </summary>
    private readonly Dictionary<string, long> _actionReleaseFrame = new();

    /// <summary>
    /// Config 实例，由 GameManager 注入
    /// </summary>
    private Config _config;

    /// <summary>
    /// key_mapper 配置名称
    /// </summary>
    private const string KEY_MAPPER_CONFIG_NAME = "key_mapper";

    /// <summary>
    /// Godot 3 特殊键编码区间。
    /// Godot 4 重新定义了这批编码，落在该区间的数值必然是旧版遗留，不能直接当Key使用。
    /// </summary>
    private const int GODOT3_KEYCODE_BEGIN = 0x01000000;
    private const int GODOT3_KEYCODE_END = 0x01FFFFFF;

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
        { Actions.Zoom, new List<Key> { (Key)4, (Key)5 } },  // 滚轮上/下
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

        // 确保未被 UI（Control）截停的输入事件会派发到本管理器
        SetProcessUnhandledInput(true);
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    #region 未截停输入处理（_UnhandledInput）

    /// <summary>
    /// 单个动作绑定的输入源集合，用于在收到「按下」事件后与原始输入状态做校验
    /// </summary>
    private sealed class ActionSources
    {
        /// <summary>
        /// 键盘键位
        /// </summary>
        public readonly List<Key> Keys = new();

        /// <summary>
        /// 鼠标按键
        /// </summary>
        public readonly List<MouseButton> MouseButtons = new();

        /// <summary>
        /// 手柄按钮（含实际产生输入的设备号）
        /// </summary>
        public readonly List<(int Device, JoyButton Button)> JoyButtons = new();

        /// <summary>
        /// 是否未绑定任何输入源
        /// </summary>
        public bool IsEmpty => Keys.Count == 0 && MouseButtons.Count == 0 && JoyButtons.Count == 0;
    }

    /// <summary>
    /// 处理未被 UI（Control）截停的输入事件。
    /// 
    /// Control 的 mouse_filter 为 Stop 时会截停事件，其按下不会在这里被记录，
    /// 因此点击 UI 不会误触发游戏内动作；Pass / Ignore 的 Control 不会截停事件。
    /// </summary>
    public override void _UnhandledInput(InputEvent @event)
    {
        // 忽略键盘自动重复产生的回声事件
        if (@event.IsEcho())
        {
            return;
        }

        switch (@event)
        {
            case InputEventMouseButton mouseButton:
                if (mouseButton.Pressed)
                    HandleSourcePressed(mouseButton);
                else
                    HandleSourceReleased(mouseButton);
                break;

            case InputEventKey key:
                if (key.Pressed)
                    HandleSourcePressed(key);
                else
                    HandleSourceReleased(key);
                break;

            case InputEventJoypadButton joyButton:
                if (joyButton.Pressed)
                    HandleSourcePressed(joyButton);
                else
                    HandleSourceReleased(joyButton);
                break;
        }
    }

    /// <summary>
    /// 记录被按下事件命中的动作
    /// </summary>
    private void HandleSourcePressed(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton)
        {
            // 滚轮等瞬时按键无法保持按下，只记录为「刚刚按下」
            bool holdable = IsHoldableMouseButton(mouseButton.ButtonIndex);
            // 记录滚轮方向，供GetZoomDirection查询
            if (mouseButton.ButtonIndex == MouseButton.WheelUp)
            {
                _zoomDirection = 1;
            }
            else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
            {
                _zoomDirection = -1;
            }
            foreach (string action in FindMouseActions(mouseButton.ButtonIndex))
            {
                MarkActionPressed(action, holdable);
            }
            return;
        }

        if (@event is InputEventKey key)
        {
            foreach (string action in FindKeyActions(key))
            {
                MarkActionPressed(action, true);
            }
            return;
        }

        if (@event is InputEventJoypadButton joyButton)
        {
            foreach (string action in FindJoyActions(joyButton))
            {
                MarkActionPressed(action, true);
            }
        }
    }

    /// <summary>
    /// 记录被松开事件命中的动作
    /// </summary>
    private void HandleSourceReleased(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton)
        {
            foreach (string action in FindMouseActions(mouseButton.ButtonIndex))
            {
                MarkActionReleased(action);
            }
            return;
        }

        if (@event is InputEventKey key)
        {
            foreach (string action in FindKeyActions(key))
            {
                MarkActionReleased(action);
            }
            return;
        }

        if (@event is InputEventJoypadButton joyButton)
        {
            foreach (string action in FindJoyActions(joyButton))
            {
                MarkActionReleased(action);
            }
        }
    }

    /// <summary>
    /// 标记动作被按下。holdable 为 false 表示瞬时输入（如滚轮），只影响 JustPressed
    /// </summary>
    private void MarkActionPressed(string action, bool holdable)
    {
        if (holdable)
        {
            _unhandledPressedActions.Add(action);
        }
        _actionPressFrame[action] = (long)Engine.GetProcessFrames();
    }

    /// <summary>
    /// 标记动作被松开
    /// </summary>
    private void MarkActionReleased(string action)
    {
        _actionReleaseFrame[action] = (long)Engine.GetProcessFrames();

        // 仅当所有绑定输入源都已松开时才清除按下标记
        // （同一动作可能绑定多个输入源，或部分输入源被 UI 截停后收不到松开事件）
        if (_actionSources.TryGetValue(action, out ActionSources sources) && !IsAnySourceRawPressed(sources))
        {
            _unhandledPressedActions.Remove(action);
        }
    }

    /// <summary>
    /// 查找该鼠标按键绑定的所有动作
    /// </summary>
    private List<string> FindMouseActions(MouseButton button)
    {
        var actions = new List<string>();
        foreach (var kvp in _actionSources)
        {
            if (kvp.Value.MouseButtons.Contains(button))
            {
                actions.Add(kvp.Key);
            }
        }
        return actions;
    }

    /// <summary>
    /// 查找该键盘事件绑定的所有动作（同时比较逻辑键与物理键）
    /// </summary>
    private List<string> FindKeyActions(InputEventKey key)
    {
        var actions = new List<string>();
        foreach (var kvp in _actionSources)
        {
            foreach (Key boundKey in kvp.Value.Keys)
            {
                bool matched = (key.Keycode != Key.None && key.Keycode == boundKey)
                    || (key.PhysicalKeycode != Key.None && key.PhysicalKeycode == boundKey);

                if (matched)
                {
                    actions.Add(kvp.Key);
                    break;
                }
            }
        }
        return actions;
    }

    /// <summary>
    /// 查找该手柄按钮事件绑定的所有动作，并补全实际产生输入的设备号
    /// </summary>
    private List<string> FindJoyActions(InputEventJoypadButton joyButton)
    {
        var actions = new List<string>();
        foreach (var kvp in _actionSources)
        {
            bool matched = false;
            bool deviceKnown = false;
            foreach ((int device, JoyButton button) in kvp.Value.JoyButtons)
            {
                if (button != joyButton.ButtonIndex)
                {
                    continue;
                }
                matched = true;
                deviceKnown |= device == joyButton.Device;
            }

            if (!matched)
            {
                continue;
            }

            if (!deviceKnown)
            {
                kvp.Value.JoyButtons.Add((joyButton.Device, joyButton.ButtonIndex));
            }
            actions.Add(kvp.Key);
        }
        return actions;
    }

    /// <summary>
    /// 校验动作绑定的输入源当前是否仍处于按下状态（原始状态不受 UI 截停影响），
    /// 用于修正因 UI 截停 / 窗口失焦而丢失的松开事件
    /// </summary>
    private static bool IsAnySourceRawPressed(ActionSources sources)
    {
        foreach (Key key in sources.Keys)
        {
            if (Input.IsKeyPressed(key) || Input.IsPhysicalKeyPressed(key))
            {
                return true;
            }
        }

        foreach (MouseButton button in sources.MouseButtons)
        {
            if (IsHoldableMouseButton(button) && Input.IsMouseButtonPressed(button))
            {
                return true;
            }
        }

        foreach ((int device, JoyButton button) in sources.JoyButtons)
        {
            if (Input.IsJoyButtonPressed(device, button))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 判断鼠标按键是否为可保持按下状态的按键（左 / 右 / 中键），滚轮等瞬时按键返回 false
    /// </summary>
    private static bool IsHoldableMouseButton(MouseButton button)
    {
        return button >= MouseButton.Left && button <= MouseButton.Middle;
    }

    #endregion

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
    /// 鼠标按键的配置名到MouseButton的映射。
    /// 鼠标按键不使用数字，否则"1"会被OS.FindKeycodeFromString解析成数字键1
    /// </summary>
    private static readonly Dictionary<string, MouseButton> MouseButtonNames = new()
    {
        { "MouseLeft", MouseButton.Left },
        { "MouseRight", MouseButton.Right },
        { "MouseMiddle", MouseButton.Middle },
        { "MouseWheelUp", MouseButton.WheelUp },
        { "MouseWheelDown", MouseButton.WheelDown },
        { "MouseWheelLeft", MouseButton.WheelLeft },
        { "MouseWheelRight", MouseButton.WheelRight },
        { "MouseXButton1", MouseButton.Xbutton1 },
        { "MouseXButton2", MouseButton.Xbutton2 }
    };

    /// <summary>
    /// 尝试把配置项解析为鼠标按键
    /// </summary>
    private static bool TryParseMouseButton(string raw, out MouseButton button)
    {
        button = MouseButton.None;
        if (string.IsNullOrEmpty(raw))
        {
            return false;
        }

        if (MouseButtonNames.TryGetValue(raw, out button))
        {
            return true;
        }

        //兼容旧配置：1-9按鼠标按键处理（Godot的MouseButton枚举值1-9即为各鼠标按键）
        if (int.TryParse(raw, out int code) && code >= 1 && code <= 9)
        {
            button = (MouseButton)code;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 解析单个键位配置项。
    /// 支持键名（如"Escape"、"Up"、"W"）与纯数字键码两种格式。
    /// 键名通过OS.FindKeycodeFromString解析，因此配置不依赖引擎版本的键码编码；
    /// 纯数字保留为向后兼容，但仅对ASCII键有效（Godot 3到4的特殊键编码已变化）。
    /// </summary>
    /// <param name="raw">配置项原始文本</param>
    /// <param name="key">解析结果</param>
    /// <returns>是否解析成功</returns>
    private static bool TryParseKey(string raw, out Key key)
    {
        key = Key.None;
        if (string.IsNullOrEmpty(raw))
        {
            return false;
        }

        //优先按键名解析
        Key byName = OS.FindKeycodeFromString(raw);
        if (byName != Key.None)
        {
            key = byName;
            return true;
        }

        //回退：按数字键码解析
        if (int.TryParse(raw, out int keyCode) && keyCode != 0)
        {
            //拒绝Godot 3遗留的特殊键编码区间，避免配置中的旧值静默失效
            if (keyCode >= GODOT3_KEYCODE_BEGIN && keyCode <= GODOT3_KEYCODE_END)
            {
                GD.PushWarning($"[InputManager] 键位配置项'{raw}'是Godot 3遗留键码，已忽略并改用默认值，请改为键名（如\"Escape\"）");
                return false;
            }

            key = (Key)keyCode;
            return true;
        }

        GD.PushWarning($"[InputManager] 无法解析键位配置项：{raw}");
        return false;
    }

    /// <summary>
    /// 把键位转换为配置文件中的文本。
    /// 鼠标按键写回MouseLeft这类名字，其余按键写回OS提供的键名，
    /// 从而使配置既不依赖引擎版本的键码编码，也不会与数字键混淆。
    /// </summary>
    private static string KeyToConfigString(Key key)
    {
        int code = (int)key;

        //鼠标按键区间，写回名字
        if (code >= 1 && code <= 9)
        {
            MouseButton button = (MouseButton)code;
            foreach (var kvp in MouseButtonNames)
            {
                if (kvp.Value == button)
                {
                    return kvp.Key;
                }
            }
        }

        //键盘按键，写回键名
        string keyName = OS.GetKeycodeString(key);
        if (!string.IsNullOrEmpty(keyName))
        {
            return keyName;
        }

        return code.ToString();
    }

    /// <summary>
    /// 从 Config 解析键盘映射
    /// 格式: keyboard.{action} → Variant.Array of 键名或键码字符串
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
                    string raw = k.AsString();
                    //鼠标按键优先判定：其配置名为MouseLeft等，
                    //若交给OS.FindKeycodeFromString会把旧格式的"1"误解析为数字键1
                    if (TryParseMouseButton(raw, out MouseButton button))
                    {
                        list.Add((Key)(int)button);
                        continue;
                    }
                    if (TryParseKey(raw, out Key key))
                    {
                        list.Add(key);
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
                arr.Add(KeyToConfigString(key));
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

        // 键位可能已变更，重建输入源缓存与按下状态
        _actionSources.Clear();
        _unhandledPressedActions.Clear();
        _actionPressFrame.Clear();
        _actionReleaseFrame.Clear();

        foreach (var field in fields)
        {
            string actionName = (string)field.GetValue(null);

            // 记录该动作绑定的输入源，供 _UnhandledInput 匹配与状态校验使用
            ActionSources sources = new();
            _actionSources[actionName] = sources;

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
                        // 记录为鼠标按键输入源
                        sources.MouseButtons.Add((MouseButton)keyCode);

                        var inputEvent = new InputEventMouseButton();
                        inputEvent.ButtonIndex = (MouseButton)keyCode;
                        // 事件必须标记为按下状态，否则Godot的动作匹配不会命中实际输入
                        inputEvent.Pressed = true;
                        InputMap.ActionAddEvent(actionName, inputEvent);
                    }
                    else
                    {
                        // 记录为键盘输入源
                        sources.Keys.Add(key);

                        var inputEvent = new InputEventKey();
                        inputEvent.Keycode = key;
                        // 事件必须标记为按下状态，否则Godot的动作匹配不会命中实际输入
                        inputEvent.Pressed = true;
                        InputMap.ActionAddEvent(actionName, inputEvent);
                    }
                }
            }

            // 注册手柄按钮
            if (_gamepadMapping.TryGetValue(actionName, out var buttons))
            {
                foreach (var btn in buttons)
                {
                    // 记录为手柄按钮输入源（设备号在收到实际按下事件时补全）
                    sources.JoyButtons.Add((0, btn));

                    var inputEvent = new InputEventJoypadButton();
                    inputEvent.ButtonIndex = btn;
                    // 事件必须标记为按下状态，否则Godot的动作匹配不会命中实际输入
                    inputEvent.Pressed = true;
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
    /// 检查指定动作是否被按下（保持按下状态）。
    /// 
    /// 只有按下事件通过了 UI 层（未被 mouse_filter = Stop 的 Control 截停）才会返回 true，
    /// 因此点击 UI 不会误触发游戏内动作；同时会与原始输入状态校验，避免长按状态卡死。
    /// </summary>
    public bool IsActionPressed(string action)
    {
        if (!_actionSources.TryGetValue(action, out ActionSources sources) || sources.IsEmpty)
        {
            return false;
        }

        if (!_unhandledPressedActions.Contains(action))
        {
            return false;
        }

        if (IsAnySourceRawPressed(sources))
        {
            return true;
        }

        // 输入源已全部松开却没收到松开事件（例如松开时鼠标位于 UI 上），在此清除标记
        _unhandledPressedActions.Remove(action);
        return false;
    }

    /// <summary>
    /// 检查指定动作当前是否按住，与IsActionPressed的区别是不判断是否在UI上。
    /// <para>
    /// IsActionPressed要求按下事件曾经通过UI层（未被mouse_filter=Stop的Control截停），
    /// 因此"从UI上按下"或"鼠标位于UI上"时恒为false；
    /// 本方法只查原始按键/鼠标状态，无论按下发生在UI还是游戏区域都会返回true。
    /// </para>
    /// <para>
    /// 适用场景：从UI发起的拖拽（例如从面板拖出建筑后放置）。
    /// 注意：点击UI按钮时本方法也会返回true，因此不可用于"点击UI不触发游戏动作"的判定，
    /// 那种情况请用IsActionPressed。
    /// </para>
    /// </summary>
    /// <param name="action">动作名称</param>
    /// <returns>原始输入源是否处于按下状态</returns>
    public bool IsActionPressedRaw(string action)
    {
        if (!_actionSources.TryGetValue(action, out ActionSources sources) || sources.IsEmpty)
        {
            return false;
        }

        return IsAnySourceRawPressed(sources);
    }

    /// <summary>
    /// 上一帧各动作的原始按下状态，用于JustPressedRaw/JustReleasedRaw的边沿判定
    /// </summary>
    private readonly Dictionary<string, bool> _rawPressedLast = new();

    /// <summary>
    /// 当前帧各动作的原始按下状态
    /// </summary>
    private readonly Dictionary<string, bool> _rawPressedNow = new();

    /// <summary>
    /// 上次更新原始按下状态的主循环帧号，保证同一帧内多次查询结果一致
    /// </summary>
    private long _rawStateFrame = -1;

    /// <summary>
    /// 每帧刷新一次原始按下状态快照，供JustPressedRaw/JustReleasedRaw判定边沿
    /// </summary>
    private void UpdateRawPressedState()
    {
        long frame = (long)Engine.GetProcessFrames();
        if (frame == _rawStateFrame)
        {
            return;
        }

        _rawStateFrame = frame;
        _rawPressedLast.Clear();
        foreach (var kvp in _rawPressedNow)
        {
            _rawPressedLast[kvp.Key] = kvp.Value;
        }

        foreach (var action in _actionSources.Keys)
        {
            _rawPressedNow[action] = IsActionPressedRaw(action);
        }
    }

    /// <summary>
    /// 检查指定动作是否刚刚从未按下变为按下（忽略UI截停）。
    /// 以主循环帧号为基准，同一帧内多次查询结果一致。
    /// </summary>
    /// <param name="action">动作名称</param>
    /// <returns>是否刚刚按下</returns>
    public bool IsActionJustPressedRaw(string action)
    {
        UpdateRawPressedState();
        _rawPressedNow.TryGetValue(action, out bool now);
        _rawPressedLast.TryGetValue(action, out bool last);
        return now && !last;
    }

    /// <summary>
    /// 检查指定动作是否刚刚从按下变为未按下（忽略UI截停）。
    /// 以主循环帧号为基准，同一帧内多次查询结果一致。
    /// </summary>
    /// <param name="action">动作名称</param>
    /// <returns>是否刚刚松开</returns>
    public bool IsActionJustReleasedRaw(string action)
    {
        UpdateRawPressedState();
        _rawPressedNow.TryGetValue(action, out bool now);
        _rawPressedLast.TryGetValue(action, out bool last);
        return !now && last;
    }

    /// <summary>
    /// 检查指定动作是否刚刚被按下（按下当帧有效）。
    /// 以主循环帧号为基准，同一帧内在 _Process / _PhysicsProcess 中查询结果一致。
    /// </summary>
    public bool IsActionJustPressed(string action)
    {
        return _actionSources.ContainsKey(action)
            && _actionPressFrame.TryGetValue(action, out long frame)
            && frame == (long)Engine.GetProcessFrames();
    }

    /// <summary>
    /// 检查指定动作是否刚刚被松开（松开当帧有效，以主循环帧号为基准）。
    /// 若松开事件本身被 UI 截停（例如拖到 UI 上再松开），此方法不会返回 true。
    /// </summary>
    public bool IsActionJustReleased(string action)
    {
        return _actionSources.ContainsKey(action)
            && _actionReleaseFrame.TryGetValue(action, out long frame)
            && frame == (long)Engine.GetProcessFrames();
    }

    /// <summary>
    /// 获取指定动作的输入强度。
    /// 
    /// 当前所有动作均绑定为数字输入（键盘 / 鼠标按键 / 手柄按钮），因此只返回 0 或 1；
    /// 若后续需要支持摇杆轴等模拟输入，请在此扩展。
    /// </summary>
    public float GetActionStrength(string action)
    {
        return IsActionPressed(action) ? 1.0f : 0.0f;
    }

    /// <summary>
    /// 获取移动方向向量（Vector2）
    /// 
    /// 走本管理器的动作状态（会受 UI 截停影响），而非 Godot 原始输入状态，
    /// 保证与 IsActionPressed 等接口行为一致。
    /// </summary>
    public Vector2 GetMoveDirection()
    {
        Vector2 direction = Vector2.Zero;
        direction.Y += GetActionStrength(Actions.MoveDown) - GetActionStrength(Actions.MoveUp);
        direction.X += GetActionStrength(Actions.MoveRight) - GetActionStrength(Actions.MoveLeft);
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
    /// 由视口鼠标位置推导的上一帧鼠标屏幕坐标
    /// </summary>
    private Vector2 _lastMousePos = Vector2.Zero;

    /// <summary>
    /// 本帧的鼠标屏幕位移
    /// </summary>
    private Vector2 _mouseDelta = Vector2.Zero;

    /// <summary>
    /// 上次更新鼠标位移时的主循环帧号，保证同一帧内多次查询得到相同结果
    /// </summary>
    private long _mouseDeltaFrame = -1;

    /// <summary>
    /// 最近一次滚轮方向：1为向上，-1为向下，0为未滚动
    /// </summary>
    private int _zoomDirection = 0;

    /// <summary>
    /// 获取滚轮方向，读取后重置为0，因此每次滚动只会被取走一次
    /// </summary>
    /// <returns>1为滚轮向上，-1为滚轮向下，0为本次无滚动</returns>
    public int GetZoomDirection()
    {
        int direction = _zoomDirection;
        _zoomDirection = 0;
        return direction;
    }

    /// <summary>
    /// 本帧的鼠标屏幕位移（仅键鼠模式有效）
    /// 由视口鼠标位置逐帧差分得到，同一帧内重复调用返回同一结果
    /// </summary>
    /// <returns>鼠标屏幕位移，非键鼠模式返回Vector2.Zero</returns>
    public Vector2 GetMouseDelta()
    {
        if (CurrentMode != InputMode.KeyboardMouse)
        {
            return Vector2.Zero;
        }

        long frame = (long)Engine.GetProcessFrames();
        if (frame == _mouseDeltaFrame)
        {
            return _mouseDelta;
        }

        Vector2 pos = GetViewport().GetMousePosition();
        if (_mouseDeltaFrame < 0)
        {
            //首次调用只记录位置
            _lastMousePos = pos;
            _mouseDelta = Vector2.Zero;
            _mouseDeltaFrame = frame;
            return _mouseDelta;
        }

        _mouseDelta = pos - _lastMousePos;
        _lastMousePos = pos;
        _mouseDeltaFrame = frame;
        return _mouseDelta;
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
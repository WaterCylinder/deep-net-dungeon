using Godot;
using System;
using System.Collections.Generic;
using Utils;

public partial class PUIHealthBar : UIHealthBar
{
    [Export] public Control smallTextPoint;

    [Export] public Label scoreLabel;

    [Export] public PackedScene buffLayoutItemPrefab;
    [Export] public Control buffNode;
    [Export] public Control buffIntroNode;
    [Export] public RichTextLabel buffIntroLabel;

    #region Buff显示参数

    /// <summary>
    /// 层数文本的字号（显示在图标右下角）
    /// </summary>
    [Export] public int buffStackFontSize = 14;

    /// <summary>
    /// 层数文本颜色
    /// </summary>
    [Export] public Color buffStackColor = Colors.White;

    /// <summary>
    /// 层数文本描边颜色（图标偏亮时保证可读）
    /// </summary>
    [Export] public Color buffStackOutlineColor = new(0f, 0f, 0f, 0.9f);

    /// <summary>
    /// 层数文本描边宽度
    /// </summary>
    [Export] public int buffStackOutlineSize = 4;

    #endregion

    Game game => GameManager.Instance.game;
    Player player => game.player;

    /// <summary>
    /// 当前图标栏里显示的Buff（与_shownItems一一对应），用于判断结构是否变化
    /// </summary>
    private readonly List<Buff> _shownBuffs = new();

    /// <summary>
    /// 当前图标栏里每个Buff对应的图标控件
    /// </summary>
    private readonly List<Control> _shownItems = new();

    public override void _Ready()
    {
        SetTarget(player);

        player.OnHurtSignal += (e, dmg) =>
        {
            string text = dmg.ToString("F1");
            SmallText smt = SmallText.CreateSmallText(
                SmallTextType.QuickShow, 
                Vector2.Zero,
                "-" + text,
                Colors.Red,
                smallTextPoint
            );
        };

        //Buff显示刷新：
        //OnAttachBuffSignal只在挂载新Buff时发，层数增减/移除都不会发，
        //所以要同时订阅OnBuffsChangedSignal（挂载、叠层、层数增减、移除、刷新时间都会发）
        player.OnAttachBuffSignal += (e, b) => UpdateBuffs(b);
        player.OnBuffsChangedSignal += (e, b) => UpdateBuffs(b);

        //清掉编辑器里摆着的占位图标，并按当前Buff初始化一次
        foreach (var child in buffNode.GetChildren())
        {
            buffNode.RemoveChild(child);
            child.QueueFree();
        }
        _shownBuffs.Clear();
        _shownItems.Clear();
        UpdateBuffs();
    }

    #region Buff图标

    /// <summary>
    /// 刷新Buff图标栏。
    /// Buff的增删会重建图标节点，只有层数/时间变化时只刷新内容（不重建节点，避免频繁刷新的抖动）
    /// </summary>
    /// <param name="_buff">发生变化的Buff（可为null，仅用于信号回调签名匹配）</param>
    public void UpdateBuffs(Buff _buff = null)
    {
        if (!Game.IsAble(player) || buffLayoutItemPrefab == null || buffNode == null)
        {
            return;
        }

        List<Buff> buffs = new();
        foreach (var buff in player.buffs)
        {
            if (buff != null)
            {
                buffs.Add(buff);
            }
        }

        //Buff数量或顺序变了才重建节点
        if (!IsSameBuffList(buffs))
        {
            RebuildBuffItems(buffs);
        }

        RefreshBuffItems();
    }

    /// <summary>
    /// 判断显示中的Buff列表是否与图标栏一致
    /// </summary>
    private bool IsSameBuffList(List<Buff> buffs)
    {
        if (buffs.Count != _shownBuffs.Count)
        {
            return false;
        }
        for (int i = 0; i < buffs.Count; i++)
        {
            if (_shownBuffs[i] != buffs[i])
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 按Buff列表重建图标节点
    /// </summary>
    private void RebuildBuffItems(List<Buff> buffs)
    {
        //先摘掉再销毁：QueueFree是延迟的，不同时移除的话同一帧里新旧图标会一起参与HBox排版
        foreach (var child in buffNode.GetChildren())
        {
            buffNode.RemoveChild(child);
            child.QueueFree();
        }
        _shownBuffs.Clear();
        _shownItems.Clear();

        foreach (var buff in buffs)
        {
            Control layoutItem = buffLayoutItemPrefab.Instantiate<Control>();
            buffNode.AddChild(layoutItem);

            //层数文本：铺满图标、右下角对齐，不挡鼠标（否则悬浮提示会断）
            Label stackLabel = new()
            {
                Name = "StackLabel",
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            stackLabel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            stackLabel.AddThemeFontSizeOverride("font_size", buffStackFontSize);
            stackLabel.AddThemeColorOverride("font_color", buffStackColor);
            stackLabel.AddThemeColorOverride("font_outline_color", buffStackOutlineColor);
            stackLabel.AddThemeConstantOverride("outline_size", buffStackOutlineSize);
            layoutItem.AddChild(stackLabel);

            //悬浮提示：内容在显示时现取，保证层数/剩余时间是最新的
            layoutItem.MouseEntered += () =>
            {
                buffIntroNode.Visible = true;
                buffIntroNode.Position = GetLocalMousePosition();
                buffIntroLabel.Text = BuildBuffIntro(buff);
            };
            layoutItem.MouseExited += () =>
            {
                buffIntroNode.Visible = false;
            };

            _shownBuffs.Add(buff);
            _shownItems.Add(layoutItem);
        }
    }

    /// <summary>
    /// 刷新图标内容（贴图与层数），不重建节点
    /// </summary>
    private void RefreshBuffItems()
    {
        for (int i = 0; i < _shownItems.Count && i < _shownBuffs.Count; i++)
        {
            Control layoutItem = _shownItems[i];
            Buff buff = _shownBuffs[i];
            if (layoutItem == null || buff == null || !IsInstanceValid(layoutItem))
            {
                continue;
            }

            TextureRect textureRect = layoutItem.GetNodeOrNull<TextureRect>("TextureRect");
            if (textureRect != null)
            {
                textureRect.Texture = buff.sprite;
            }

            Label stackLabel = layoutItem.GetNodeOrNull<Label>("StackLabel");
            if (stackLabel != null)
            {
                //只有可叠层的Buff才显示层数，1层的Buff保持图标干净
                bool showStack = buff.maxStack > 1;
                stackLabel.Visible = showStack;
                stackLabel.Text = showStack ? buff.currentStack.ToString() : "";
            }
        }
    }

    /// <summary>
    /// 拼接Buff悬浮提示：介绍 + 层数 + 剩余时间
    /// </summary>
    private static string BuildBuffIntro(Buff buff)
    {
        string text = buff.intro ?? "";

        if (buff.maxStack > 1)
        {
            string max = buff.maxStack == int.MaxValue ? "∞" : buff.maxStack.ToString();
            text += $"\n层数：{buff.currentStack}/{max}";
        }

        if (buff.totalDuration > 0)
        {
            float remaining = Mathf.Max(0f, buff.remainingDuration);
            text += $"\n剩余：{remaining:0.0}秒";
        }

        return text;
    }

    #endregion

    protected override void OnUpdate(double delta)
    {
        base.OnUpdate(delta);
        if (Game.IsAble(player))
        {
            scoreLabel.Text = player.score.ToString();
        }
    }

}

using Godot;
using System;

public enum SmallTextType
{
    /// <summary>
    /// 快速显示文本，用于伤害显示等
    /// </summary>
    QuickShow
}

public partial class SmallText : Node2D
{
    #region 工厂模式

    public static PackedScene _prefab = null;

    public static PackedScene Prefab
    {
        get
        {
            if(_prefab == null)
            {
                _prefab = AssetManager.Instance.GetPrefab("Asset.Object.small_text", null);
            }
            return _prefab;
        }
    }

    public static SmallText CreateSmallText(SmallTextType _type, Vector2 pos, string text)
    {
        SmallText smallText = Prefab.Instantiate<SmallText>();

        smallText.textType = _type;
        smallText.GlobalPosition = pos;
        smallText.SetText(text);

        GameManager.Instance.game.smallObjectNode.AddChild(smallText);

        return smallText;
    }

    #endregion

    [Export] public SmallTextType textType;
    [Export] public Label label;

    public delegate void OnUpdateEventHandler(float delta);
    public delegate void OnFinishEventHandler(); 
    /// <summary>
    /// 文本行为代理
    /// </summary>
    public OnUpdateEventHandler onUpdate;
    /// <summary>
    /// 文本结束时行为
    /// </summary>
    public OnFinishEventHandler onFinish;

    public override void _Ready()
    {
        Tween tween = CreateTween();

        switch (textType)
        {
            case SmallTextType.QuickShow:
            tween.TweenMethod( 
                Callable.From((float v) =>
                {
                    Vector2 scale = label.Scale;
                    scale.X = v;
                    label.Scale = scale;
                }),
                0f, 1f, 0.2
            );
            tween.TweenInterval(0.64);
            tween.TweenMethod(
                Callable.From((float v) =>
                {
                    Vector2 scale = label.Scale;
                    scale.X = v;
                    label.Scale = scale;
                }),
                1f, 0f, 0.2
            );
            tween.Finished += () => onFinish?.Invoke();
            tween.Finished += QueueFree;
            break;
        }
    }

    public void SetText(string text)
    {
        label.Text = text;
    }

    public void SetColor(Color color)
    {
        label.LabelSettings.FontColor = color;
    }


    public override void _Process(double delta)
    {
        onUpdate?.Invoke((float)delta);
    }
}

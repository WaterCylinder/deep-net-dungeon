using Godot;
using System;

/// <summary>
/// 制作人员名单：把 label 从屏幕最下方开始缓慢往上推，
/// 直到 label 的下边界到达屏幕中心位置为止（到点后停住并发 Finished 信号）。
///
/// 用法：挂在 Asset/ListOfProducers.tscn 的根节点上，label 指向里面的 RichTextLabel
/// </summary>
public partial class ListOfProducers : Control
{
    /// <summary>
    /// 要滚动的那段名单
    /// </summary>
    [Export] public RichTextLabel label;

    /// <summary>
    /// 上移速度（像素/秒），越大越快
    /// </summary>
    [Export] public float speed = 60f;

    /// <summary>
    /// 停止位置：label 下边界最终对齐到屏幕高度的这个比例（0.5 = 屏幕中心）
    /// </summary>
    [Export(PropertyHint.Range, "0,1,0.01")] public float stopAtScreenRatio = 0.5f;

    /// <summary>
    /// 滚到终点时触发
    /// </summary>
    [Signal] public delegate void FinishedEventHandler();

    bool _finished;

    public override void _Ready()
    {
        base._Ready();

        //没在检查器里指定时，退回到同名子节点
        if (label == null)
        {
            label = GetNodeOrNull<RichTextLabel>("RichTextLabel");
        }

        if (label == null)
        {
            GD.PushWarning("[ListOfProducers] 没有指定要滚动的 label");
            return;
        }

        //不让内容反过来把控件撑高（fit_content会让最小尺寸等于内容高度，
        //纵向锚点是拉伸时会把顶边往上挤，和下面按Position移动的逻辑打架）
        label.FitContent = false;

        //初始放在屏幕最下方：label 的顶边贴住屏幕底边，此时整段内容都在屏幕外
        Vector2 screen = GetViewportRect().Size;
        label.Position = new Vector2(label.Position.X, screen.Y);
    }

    public override void _Process(double delta)
    {
        if (_finished || label == null)
        {
            return;
        }

        //等RichTextLabel把内容排版完（第一帧可能还是0）
        float contentHeight = label.GetContentHeight();
        if (contentHeight <= 0f)
        {
            return;
        }

        Vector2 screen = GetViewportRect().Size;

        //下边界到达屏幕中心时，label 的顶边应该在 stopY
        float stopY = screen.Y * stopAtScreenRatio - contentHeight;

        float y = label.Position.Y - speed * (float)delta;
        if (y <= stopY)
        {
            y = stopY;
            _finished = true;
        }

        label.Position = new Vector2(label.Position.X, y);

        if (_finished)
        {
            EmitSignal(SignalName.Finished);
        }
    }
}

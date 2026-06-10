using Godot;

/// <summary>
/// 返回按钮 - 点击后返回上一个场景
/// 将此脚本直接挂载到Button节点上
/// </summary>
public partial class BackButton : Button
{
    public override void _Ready()
    {
        Pressed += OnBackPressed;
    }

    /// <summary>
    /// 点击返回按钮 - 返回上一个场景
    /// </summary>
    private void OnBackPressed()
    {
        GD.Print("返回上一个场景");
        SceneManager.Instance?.GoBackScene();
    }
}
using Godot;
using Godot.Collections;

/// <summary>
/// 主动道具数据（Resource）
/// </summary>
[GlobalClass]
public partial class ActiveItemData : ItemDataBase
{
    [Export] public float cooldown = 5f;
    [Export] public float duration = 3f;
    [Export] public Array<Resource> activationEffects = new();

    public override void ApplyEffect(Node player)
    {
        // 留空：应用主动效果逻辑
    }
}

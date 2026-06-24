using Godot;

/// <summary>
/// 敌人实体类
/// </summary>
public partial class Enemy : Entity
{
    /// <summary>
    /// 敌人碰撞层（第2层）
    /// </summary>
    private const int LAYER_ENEMY = 2;
    
    /// <summary>
    /// 敌人碰撞掩码
    /// 检测：玩家(Layer1)、障碍物(Layer4)
    /// 不包含子弹层(Layer3)，避免与子弹产生物理碰撞
    /// </summary>
    private const int MASK_ENEMY = (1 << 0) | (1 << 3); // Player | Obstacle

    protected override void OnEnterTree()
    {
        base.OnEnterTree();
        
        // 设置敌人标签
        AddTag(EntityTag.Enemy);
        
        // 设置碰撞层和掩码
        CollisionLayer = LAYER_ENEMY;
        CollisionMask = MASK_ENEMY;
    }
}
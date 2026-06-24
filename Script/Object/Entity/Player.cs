using Godot;

/// <summary>
/// 玩家实体类
/// </summary>
public partial class Player : Entity
{
    /// <summary>
    /// 玩家碰撞层（第1层）
    /// </summary>
    private const int LAYER_PLAYER = 1;
    
    /// <summary>
    /// 玩家碰撞掩码
    /// 检测：敌人(Layer2)、障碍物(Layer4)
    /// 不包含子弹层(Layer3)，避免与子弹产生物理碰撞
    /// </summary>
    private const int MASK_PLAYER = (1 << 1) | (1 << 3); // Enemy | Obstacle

    protected override void OnEnterTree()
    {
        base.OnEnterTree();
        
        // 设置玩家标签
        AddTag(EntityTag.Player);
        
        // 设置碰撞层和掩码
        CollisionLayer = LAYER_PLAYER;
        CollisionMask = MASK_PLAYER;
    }

    protected override void OnStart()
    {
        smoothFactor = 0.1f;
    }

    /// <summary>
    /// 重写物理更新方法，用于处理每帧的物理相关逻辑
    /// </summary>
    /// <param name="delta">自上一帧以来经过的时间（秒）</param>
    protected override void OnPhysicsUpdate(double delta)
    {
        // 获取输入管理器实例
        InputManager input = InputManager.Instance;
        // 检查是否未按下其他技能键
        if (! input.IsActionPressed(InputManager.Actions.OtherSkill))
        {
            // 如果未按下技能键，则向鼠标世界位置移动
            MoveTarget(input.GetMouseWorldPosition());
        }
        else
        {
            // 如果按下技能键，则停止移动
            MoveStop();
        }
    }
}
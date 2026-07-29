using Godot;

/// <summary>
/// 玩家实体类
/// </summary>
public partial class Player : Entity
{
    [Export] Trigger enemyTrigger;

    # region 玩家即时属性

    public Enemy targetEnemy = null;

    public Vector2 targetShotToward = Vector2.Zero;

    # endregion 

    protected override void OnEnterTree()
    {
        base.OnEnterTree();
        
        // 设置玩家标签
        AddTag(EntityTag.Player);

        GameManager.Instance.game.player = this;
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

    protected override void OnUpdate(double delta)
    {
        base.OnUpdate(delta);
        //默认状态获取触发器的实例，自动攻击
        targetEnemy = enemyTrigger.latestEntity as Enemy;
        TargetAttack();
    }

    /// <summary>
    /// 朝着目标攻击
    /// </summary>
    void TargetAttack()
    {
        if (targetEnemy == null)
        {
            return;
        }
        targetShotToward = targetEnemy.Position - Position;
        targetShotToward = targetShotToward.Normalized();
        TryAttack();
    }

    /// <summary>
    /// 朝着指定方向攻击
    /// </summary>
    /// <param name="toward"></param>
    void TryAttack()
    {
        if (targetShotToward == Vector2.Zero)
        {
            return;
        }
        Attack();
    }

    protected override void OnAttack()
    {
        Bullet but = Bullet.Create(
            this,
            Position,
            targetShotToward * 500f,
            null
        );
        GD.Print(but);
    }
}
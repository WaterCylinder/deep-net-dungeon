using Godot;
using Utils;

/// <summary>
/// 玩家实体类
/// </summary>
public partial class Player : Entity
{
    [Export] public Trigger enemyTrigger;
    [Export] public Trigger energyTrigger;

    [Export] public AnimatedSprite2D animeNode;

    [Export] public Node2D shotPoint;
    [Export] public AudioStream hitSound;

    [ExportCategory("局内属性")]
    [Export] public int score;

    #region 玩家即时属性

    public int opera = 0;
    public float bulletSpeed = 10f;

    public Enemy targetEnemy = null;

    public Vector2 targetShotToward = Vector2.Zero;

    #endregion 

    Game game => GameManager.Instance.game;

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
        //设置不检测特定标签的敌怪
        enemyTrigger.beforeEntityEntered += e =>
        {
            if(e is Enemy enemy)
            {
                if (enemy.CheckTag(EnemyTag.TriggerDisable))
                {
                    return false;
                }
            }
            return true;
        };

        energyTrigger.beforeEntityEntered += e =>
        {
            if(e is EnergyPoint ep)
            {
                ep.target = this;
            }
            return true;
        };
    }

    /// <summary>
    /// 重写物理更新方法，用于处理每帧的物理相关逻辑
    /// </summary>
    /// <param name="delta">自上一帧以来经过的时间（秒）</param>
    protected override void OnPhysicsUpdate(double delta)
    {
        HandleMoveInput();

        if(opera > 0)return;

        //更新动画朝向

        if(GetVelocity().Length() > 100)
        {
            animeNode.Play("move");
            animeNode.FlipH = GetVelocity().X < 0;
        }
        else
        {
            animeNode.Play("idle");
        }
    }

    /// <summary>
    /// 处理移动输入。
    /// 
    /// 键鼠模式：按住主动技能键（默认鼠标左键）时朝鼠标位置移动；
    /// 其他情况：使用方向输入（键盘 WASD / 方向键、手柄十字键）。
    /// 
    /// 输入源是否被 UI 截停（Control 的 mouse_filter 为 Stop）已由 InputManager 内部处理，
    /// 这里只调用 InputManager 的接口，无需再自行判断是否悬停在 UI 上。
    /// </summary>
    private void HandleMoveInput()
    {
        if(opera > 0)return;

        InputManager input = InputManager.Instance;
        if (input == null)
        {
            MoveStop();
            return;
        }

        // 键鼠模式：按住主动技能键（默认鼠标左键）朝鼠标位置移动
        // 非键鼠模式下 GetMouseWorldPosition 会返回 Vector2.Zero，因此需要按模式区分
        if (input.CurrentMode == InputManager.InputMode.KeyboardMouse
            && input.IsActionPressed(InputManager.Actions.ActiveSkill))
        {
            MoveTarget(input.GetMouseWorldPosition());
            return;
        }

        // 键盘 / 手柄方向输入
        Vector2 direction = input.GetMoveDirection();
        if (direction != Vector2.Zero)
        {
            MoveToward(direction);
            return;
        }

        MoveStop();
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
        //简单预判
        if(!Tools.TryGetInterceptDirection(shotPoint.GlobalPosition, bulletSpeed * Bullet.BASE_SHOT_SPEED_FECTOR, targetEnemy.GlobalPosition, targetEnemy.GetVelocity(), out targetShotToward, out _))
        {
            targetShotToward = targetEnemy.GlobalPosition - GlobalPosition;
        }
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
        Shot(targetShotToward * bulletSpeed, shotPoint.GlobalPosition);
    }

    protected override void OnHurt(Damage damage, float finalDamage)
    {
        base.OnHurt(damage, finalDamage);
        ShaderMaterial mat = animeNode.Material as ShaderMaterial;
        Tween tween = CreateTween();
        // 变白：0.05 秒
        tween.TweenMethod(
            Callable.From((float v) => mat.SetShaderParameter("flash_modifier", v)),
            0.0f, 1.0f, 0.05f
        );
        PlaySoundGlobal(hitSound);
        // 保持一瞬间
        tween.TweenInterval(0.05f);
        // 恢复：0.2 秒
        tween.TweenMethod(
            Callable.From((float v) => mat.SetShaderParameter("flash_modifier", v)),
            1.0f, 0.0f, 0.2f
        );
    }

    protected override void OnDeath(Damage damage, Entity other)
    {
        base.OnDeath(damage, other);
        ShaderMaterial mat = animeNode.Material as ShaderMaterial;
        mat.SetShaderParameter("flash_modifier", 0);
        game.NotifyPlayerDeath();
    }

    #region 工具方法

    #endregion
}
using Godot;
using Godot.Collections;

public enum EnergyType
{
    /// <summary>
    /// 金币（能量点）
    /// </summary>
    Coin,
    /// <summary>
    /// 回血
    /// </summary>
    Health,
    /// <summary>
    /// 护盾
    /// </summary>
    Shield
}

public partial class EnergyPoint : Entity
{
    #region 工厂方法

    // 工厂方法区域，用于创建和生成物品实例
    /// <summary>
    /// 物品的预制体
    /// </summary>
    private static PackedScene _energyPrefab = null;

    /// <summary>
    /// 获取物品预制体的属性
    /// 使用单例模式，确保只加载一次
    /// </summary>
    public static PackedScene EnergyPrefab
    {
        get
        {
            if (_energyPrefab == null)
            {
                // 从资源管理器加载预制体
                _energyPrefab = AssetManager.Instance.GetPrefab("Asset.Object.energy_point", null);
            }
            return _energyPrefab;
        }
    }

    /// <summary>
    /// 创建count个能量点
    /// </summary>
    /// <param name="type"></param>
    /// <param name="pos"></param>
    /// <param name="count"></param>
    /// <param name="parent"></param>
    /// <returns></returns>
    public static Array<EnergyPoint> CreateEnergyPoint(EnergyType type, Vector2 pos, int count = 1, float value = 1, float radius = 0, Node parent = null)
    {

        if(parent == null)
        {
            parent = GameManager.Instance.game.energyNode;
            parent ??= GameManager.Instance.game;
        }
        var points = new Array<Vector2>();
        if(count > 1)
        {
            float dropRadius = 4f;

            float radiusMin = 32f;
            float radiusMax = 128f;
            if(radius == 0)
                radius = Mathf.Clamp(dropRadius * Mathf.Sqrt(count) * 2.5f, radiusMin, radiusMax);

            for (int i = 0; i < count; i++)
            {
                // sqrt 保证面积均匀分布
                float r = radius * Mathf.Sqrt(GD.Randf());
                float angle = GD.Randf() * Mathf.Tau;
                Vector2 candidate = pos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
                points.Add(candidate);
            }
        }
        else
        {
            points.Add(pos);
        }

        Array<EnergyPoint> result = new();

        for (int i = 0; i < points.Count; i++)
        {
            EnergyPoint ep = EnergyPrefab.Instantiate() as EnergyPoint;
            var dropPos = points[i];
            Callable.From(() =>
            {
                parent.AddChild(ep);
                ep.GlobalPosition = dropPos;
            }).CallDeferred();
            
            ep.value = value;
            ep.Init(type);
        }

        return result;
    }

    #endregion

    [Export] public Sprite2D spriteNode;
    [Export] public EnergyType energyType;
    [Export] public float value;
    [Export] public Entity target;
    [Export] public AudioStream pickSound;
    
    [Signal] public delegate void CloseToTargetEventHandler();

    public override void _Ready()
    {
        base._Ready();
        tags.Add(EntityTag.Energy);
    }

    public void Pick(Entity entity)
    {
        switch (energyType)
        {
            case EnergyType.Coin:
                if(entity is Player player)
                {
                    player.score += (int)value;
                }
            break;
            case EnergyType.Health:
                entity.Heal(value);
            break;
            case EnergyType.Shield:
                //护盾点：拾取获得 value × 2 点护盾（AddShield内部会封顶到最大生命）
                entity.AddShield(value * 2);
            break;
        }
        entity.EmitSignal(Entity.SignalName.OnPickEnergy, entity, this);
    }

    public void Init(EnergyType type)
    {
        energyType = type;
        switch (energyType)
        {
            case EnergyType.Coin:
                spriteNode.SelfModulate = Colors.Purple;
            break;
            case EnergyType.Health:
                spriteNode.SelfModulate = Colors.Green;
            break;
            case EnergyType.Shield:
                spriteNode.SelfModulate = Colors.Gold;
            break;
        }

        if(value < 5)
        {
            spriteNode.Scale = new(1,1);
        }else if(value < 10)
        {
            spriteNode.Scale = new(1.2f, 1.2f);
        }else if(value < 30)
        {
            spriteNode.Scale = new(1.5f, 1.5f);
        }else if(value < 100)
        {
            spriteNode.Scale = new(2f, 2f);
        }else
        {
            spriteNode.Scale = new(3f, 3f);
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if(Game.IsAble(target))
        {
            MoveTarget(target.GlobalPosition);
            ///靠近目标实体并在最近的时候触发pick
            if((target.GlobalPosition - GlobalPosition).Length() <= 16)
            {
                EmitSignal(SignalName.CloseToTarget);
                Pick(target);
                target.PlaySound(pickSound);
                QueueFree();
            }
        }
    }


}

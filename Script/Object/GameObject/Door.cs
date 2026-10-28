using Godot;
using Godot.Collections;

public enum DoorType
{
    /// <summary>
    /// 通往下一个房间
    /// </summary>
    Next,
    /// <summary>
    /// 回到上一个房间
    /// </summary>
    Back,
    /// <summary>
    /// 其他（将不会被自动赋值目标）
    /// </summary>
    Other,
    /// <summary>
    /// 前往下一层
    /// </summary>
    NextLevel,
}

/// <summary>
/// 房间标签 → 颜色。
/// 门和小地图（MapUI）共用这张表，改这里两边一起生效。
/// 取色原则：统一饱和度 54.3%、明度 62%~72%——比高饱和的"积木色"柔和，
/// 又比低明度的暗色更容易看清；普通房间保持紫色系
/// </summary>
public partial class Door : Area2D
{
    public static Dictionary<RoomTag, Color> RoomTagColorMapper = new Dictionary<RoomTag, Color>
    {
        // 商店：偏棕色（比资源房的金色更暗更浊，避免和金色撞色）
        {RoomTag.Shop, new Color("8b5a2b")},
        // 普通：紫（数量最多，饱和度统一后不会糊成一片）
        {RoomTag.Normal, new Color("8252b2")},
        // 资源：金
        {RoomTag.Treasure, new Color("b89a54")},
        // 起始：翡翠绿
        {RoomTag.Start, new Color("4da86b")},
        // BOSS：绯红
        {RoomTag.Boss, new Color("b85459")},
        // 事件：青
        {RoomTag.Event, new Color("52a7b2")},
    };
    public static Color GetColorByRoomTag(Room room)
    {
        foreach(var tag in room.tags)
        {
            if (RoomTagColorMapper.ContainsKey(tag))
            {
                return RoomTagColorMapper[tag];
            }
        }
        return Colors.White;
    }

    /// <summary>
    /// 门的类型
    /// </summary>
    [Export] public DoorType doorType = DoorType.Next;
    [Export] public ColorRect colorRect;
    ShaderMaterial shader
    {
        get
        {
            return colorRect.Material as ShaderMaterial;
        }
    }
    [Export] public Room room;
    [Export] public bool isOpen = false;
    [Export] public bool isTrans = true;
    [Export] public Room targetRoom = null;
    [Export] public Door targetDoor = null;
    [Export] public Array<EntityTag> ignoreTags = new()
    {
        EntityTag.Bullet,
        EntityTag.Enemy
    };
    
    [Signal] public delegate void OnEnterEventHandler(Entity entity);

    /// <summary>
    /// 待重新武装的门 -> 到期物理帧号
    /// </summary>
    private static readonly System.Collections.Generic.Dictionary<Door, ulong> _pendingRearm = new();

    private const ulong REARM_DELAY_FRAMES = 2;
    private const ulong TRANS_GUARD_FRAMES = 5;

    /// <summary>
    /// 本门收到传送实体的物理帧号，用于抑制落点门的反向传送
    /// </summary>
    private ulong _transFrame = 0;

    /// <summary>
    /// 是否已经绑定了有效目标。
    /// 只有绑定了目标房间或目标门的门才会被打开：没有目标的门保持关闭且不可见，
    /// 这样玩家不会走进去却没有任何反应（Trans里那条"未绑定任何目标"的警告也就不会出现）
    /// </summary>
    public bool IsBound
    {
        get
        {
            bool hasDoor = targetDoor != null && GodotObject.IsInstanceValid(targetDoor);
            bool hasRoom = targetRoom != null && GodotObject.IsInstanceValid(targetRoom);
            return hasDoor || hasRoom;
        }
    }

    public void Initialize()
    {
        Close();
        BodyEntered += body =>
        {
            if(body is Entity entity && CheckEntityTag(entity))
            {
                if (isOpen)
                {
                    //开启状态处理碰撞事件
                    EmitSignal(SignalName.OnEnter, entity);
                }
                if(CanTriggerTrans())
                {
                    Trans(entity);
                }
            }
        };
        BodyExited += body =>
        {
            if(body is Entity entity && CheckEntityTag(entity))
            {
                RearmAfterExitOrTeleport();
            }
        };
    }

    /// <summary>
    /// 请求重新武装本门
    /// </summary>
    private void RearmAfterExitOrTeleport()
    {
        _pendingRearm[this] = Engine.GetPhysicsFrames() + REARM_DELAY_FRAMES;
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if(!_pendingRearm.TryGetValue(this, out ulong dueFrame))
        {
            return;
        }
        if(Engine.GetPhysicsFrames() < dueFrame)
        {
            return;
        }
        _pendingRearm.Remove(this);

        isTrans = true;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        _pendingRearm.Remove(this);
    }

    /// <summary>
    /// 标记有实体被传送进入本门（由传送方调用）
    /// </summary>
    public void MarkTrans()
    {
        _transFrame = Engine.GetPhysicsFrames();
        isTrans = false;
    }

    /// <summary>
    /// 是否允许本门触发传送。
    /// 刚被传送进入的实体不触发，避免落点门立刻反向传送造成来回弹
    /// </summary>
    private bool CanTriggerTrans()
    {
        if(!isOpen || !isTrans || doorType == DoorType.NextLevel)
        {
            return false;
        }
        if(_transFrame != 0)
        {
            ulong now = Engine.GetPhysicsFrames();
            //用比较而非相减，避免帧号回退时无符号下溢
            if(now >= _transFrame && now - _transFrame < TRANS_GUARD_FRAMES)
            {
                GD.Print($"[门] {room?.Name}.{Name} 抑制反向触发（距落入{now - _transFrame}帧）");
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 返回实体标签是否不在忽略标签里
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    protected bool CheckEntityTag(Entity entity)
    {
        foreach(EntityTag tag in entity.tags)
        {
            if(ignoreTags.Contains(tag))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 传送：落点只由targetDoor与targetRoom决定
    /// 1.targetDoor有对应房间：普通房间/Boss房间且尚未清理则落房间坐标（首次进入的非清理房间统一从房间位置开始），
    ///   否则落targetDoor
    /// 2.targetDoor没有对应房间：落targetDoor
    /// 3.没有targetDoor：落targetRoom坐标
    /// </summary>
    protected void Trans(Entity entity)
    {
        if(!Game.IsAble(entity))
        {
            return;
        }

        isTrans = false;

        if(targetDoor != null)
        {
            GD.Print($"传送实体{entity}到目标门{targetDoor}");
            if(targetDoor.room != null)
            {
                Room targetRoom  = targetDoor.room;
                if ((targetRoom.CheckTag(RoomTag.Normal) || targetRoom.CheckTag(RoomTag.Boss)) && !targetRoom.isCleared)
                {
                    GD.Print($"目标门处于未清理的普通/Boss房间，传送实体{entity}到目标房间坐标{targetRoom.Name}");
                    entity.TransTo(targetRoom.GlobalPosition);
                }
                else
                {
                    entity.TransTo(targetDoor.GlobalPosition);
                    targetDoor.MarkTrans();
                }
                targetRoom.Enter(entity);
            }
            else
            {
                entity.TransTo(targetDoor.GlobalPosition);
                targetDoor.MarkTrans();
            }
        }
        else if(targetRoom != null)
        {
            GD.Print($"传送实体{entity}到目标房间坐标{targetRoom}");
            entity.TransTo(targetRoom.GlobalPosition);
            targetRoom.Enter(entity);
        }
        else
        {
            GD.PushWarning($"传送门 {room?.Name}.{Name} 未绑定任何目标，本次传送被忽略");
            return;
        }

        //实体可能落在目标门碰撞范围外，那样收不到BodyExited，需主动武装两端
        RearmAfterExitOrTeleport();
        targetDoor?.RearmAfterExitOrTeleport();
    }

    /// <summary>
    /// 与另一扇门双向配对：两扇门互相记录对方的门与房间。
    ///
    /// 地图生成时用它建立"这扇出口门 ↔ 那扇入口门"的明确连接。
    /// 因为是门对门的独立配对，一个房间有多条入口/出口时各连各的，不会互相覆盖
    /// （旧的Room.returnDoor方案只能让所有入口共用同一个落点门）
    /// </summary>
    /// <param name="other">对端的门</param>
    public void PairWith(Door other)
    {
        if (other == null)
        {
            return;
        }

        targetDoor = other;
        targetRoom = other.room;

        other.targetDoor = this;
        other.targetRoom = room;
    }

    public void Open()
    {
        Visible = true;
        isOpen = true;
        Tween tween = CreateTween();
        tween.TweenMethod(Callable.From<int>(v =>
        {
            shader.SetShaderParameter("open_progress", (float)v / 10);
        }), 0, 10, 0.5f);
        if(targetRoom != null)
        {
            colorRect.SelfModulate = GetColorByRoomTag(targetRoom);
        }
        else
        {
            if(targetDoor != null && targetDoor.room != null)
            {
                colorRect.SelfModulate = GetColorByRoomTag(targetDoor.room);
            }
            else
            {
                colorRect.SelfModulate = Colors.Purple;
            }
        }
    }

    public void Close()
    {
        Visible = false;
        isOpen = false;
        shader.SetShaderParameter("open_progress", 0);
    }
}

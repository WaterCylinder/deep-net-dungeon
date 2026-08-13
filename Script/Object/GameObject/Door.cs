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
    Other
}

public partial class Door : Area2D
{
    /// <summary>
    /// 门的类型
    /// </summary>
    [Export] public DoorType doorType = DoorType.Next;
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

    public void Initialize()
    {
        Close();
        BodyEntered += body =>
        {
            if(body is Entity entity && CheckEntityTag(entity))
            {
                if(isOpen && isTrans)
                {
                    Trans(entity);
                }
            }
        };
        BodyExited += body =>
        {
            if(body is Entity entity && CheckEntityTag(entity))
            {
                isTrans = true;
            }
        };
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

    protected void Trans(Entity entity)
    {
        isTrans = false;

        if(targetDoor != null)
        {
            GD.Print($"传送实体{entity}到目标门{targetDoor}【房间{targetDoor.room}】");
            targetDoor.isTrans = false;
            entity.TransTo(targetDoor.GlobalPosition);
            if(entity.room != null)
            {
                entity.room.Exit(entity);
            }
            if(targetDoor.room != null)
            {
                targetDoor.room.Enter(entity);
            }
            return;
        }
        if(targetRoom != null)
        {
            GD.Print($"传送实体{entity}到目标房间{targetDoor.room}");
            entity.TransTo(targetRoom.Position);
            if(entity.room != null)
            {
                entity.room.Exit(entity);
            }
            targetRoom.Enter(entity);
            return;
        }
        GD.PushWarning($"传送门未绑定任何目标!");
    }

    public void Open()
    {
        Visible = true;
        isOpen = true;
    }

    public void Close()
    {
        Visible = false;
        isOpen = false;
    }

}
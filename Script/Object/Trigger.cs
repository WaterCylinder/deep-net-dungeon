using Godot;
using Godot.Collections;
using System.Collections.Generic;

/// <summary>
/// 触发器 - 记录进入碰撞箱的实体
/// 支持历史记录和标签过滤
/// </summary>
public partial class Trigger : Area2D
{
    #region 配置

    /// <summary>
    /// 忽略的EntityTag列表，这些标签的实体不会触发记录，黑名单，优先级大于白名单
    /// </summary>
    [Export] public Array<EntityTag> ignoreTags = new Array<EntityTag> { EntityTag.Bullet };

    /// <summary>
    /// 包含的EntityTag列表，只有这些标签的实体才会触发记录，白名单，优先级小于黑名单
    /// </summary>
    [Export] public Array<EntityTag> includeTags = new Array<EntityTag>();

    /// <summary>
    /// 历史记录最大长度
    /// </summary>
    private const int HISTORY_MAX_SIZE = 256;

    #endregion

    #region 数据

    /// <summary>
    /// 当前在碰撞箱内的实体列表
    /// </summary>
    [Export] public Array<Entity> entityList { get; private set; } = new ();

    /// <summary>
    /// 历史记录列表（固定大小256，记录进入过的实体）
    /// </summary>
    public LinkedList<Entity> history_list { get; private set; } = new();

    #endregion

    #region 属性

    /// <summary>
    /// 获取最新的实体
    /// </summary>
    public Entity latestEntity => entityList.Count > 0 ? entityList[entityList.Count - 1] : null;

    #endregion

    #region 信号

    /// <summary>
    /// 实体进入时触发
    /// </summary>
    [Signal]
    public delegate void OnEntityEnteredEventHandler(Entity entity);

    /// <summary>
    /// 实体离开时触发
    /// </summary>
    [Signal]
    public delegate void OnEntityExitedEventHandler(Entity entity);

    #endregion

    #region 生命周期

    public override void _Ready()
    {
        base._Ready();

        // 连接碰撞信号
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        // 每帧更新实体列表排序
        if (entityList.Count > 1)
        {
            SortEntityList();
        }
    }

    #endregion

    #region 碰撞处理

    /// <summary>
    /// 碰撞体进入时触发
    /// </summary>
    private void OnBodyEntered(Node body)
    {
        if (body is Entity entity)
        {
            // 检查是否应该忽略该实体
            if (ShouldIgnore(entity))
            {
                return;
            }

            // 如果已经在列表中，不重复添加
            if (entityList.Contains(entity))
            {
                return;
            }

            // 添加到当前实体列表（按距离排序插入）
            InsertEntityByDistance(entity);

            // 添加到历史记录
            AddToHistory(entity);

            // 触发信号
            EmitSignal(SignalName.OnEntityEntered, entity);
        }
    }

    /// <summary>
    /// 碰撞体离开时触发
    /// </summary>
    private void OnBodyExited(Node body)
    {
        if (body is Entity entity)
        {
            // 检查是否应该忽略该实体
            if (ShouldIgnore(entity))
            {
                return;
            }

            // 从当前实体列表移除
            entityList.Remove(entity);

            // 触发信号
            EmitSignal(SignalName.OnEntityExited, entity);
        }
    }

    /// <summary>
    /// 检查是否应该忽略该实体
    /// </summary>
    private bool ShouldIgnore(Entity entity)
    {
        foreach (var tag in ignoreTags)
        {
            if (entity.CheckTag(tag))
            {
                return true; // 包含标签，忽略
            }
        }
        foreach (var tag in includeTags)
        {
            if (entity.CheckTag(tag))
            {
                return false; // 包含标签，允许
            }
        }
        if (includeTags.Count > 0)
        {
            return true; // 有包含标签但实体不包含，忽略
        }
        return false;
    }

    #endregion

    #region 实体列表排序

    /// <summary>
    /// 按距离将实体插入到正确位置
    /// </summary>
    private void InsertEntityByDistance(Entity entity)
    {
        float entityDistance = GetDistanceToEntity(entity);
        
        for (int i = 0; i < entityList.Count; i++)
        {
            if (entityDistance < GetDistanceToEntity(entityList[i]))
            {
                entityList.Insert(i, entity);
                return;
            }
        }
        
        // 距离最远，添加到末尾
        entityList.Add(entity);
    }

    /// <summary>
    /// 获取实体到Trigger的距离
    /// </summary>
    private float GetDistanceToEntity(Entity entity)
    {
        return GlobalPosition.DistanceTo(entity.GlobalPosition);
    }

    /// <summary>
    /// 重新排序实体列表（按距离从小到大）
    /// </summary>
    private void SortEntityList()
    {
        // 使用冒泡排序，因为列表通常较小且需要频繁更新
        for (int i = 0; i < entityList.Count - 1; i++)
        {
            for (int j = 0; j < entityList.Count - 1 - i; j++)
            {
                if (GetDistanceToEntity(entityList[j]) > GetDistanceToEntity(entityList[j + 1]))
                {
                    Entity temp = entityList[j];
                    entityList[j] = entityList[j + 1];
                    entityList[j + 1] = temp;
                }
            }
        }
    }

    #endregion

    #region 历史记录管理

    /// <summary>
    /// 添加实体到历史记录
    /// </summary>
    private void AddToHistory(Entity entity)
    {
        // 检查实体是否已在历史记录中，如果在则移动到末尾
        var existingNode = FindInHistory(entity);
        if (existingNode != null)
        {
            history_list.Remove(existingNode);
            history_list.AddLast(entity);
            return;
        }

        // 如果历史记录已满，移除最早的
        if (history_list.Count >= HISTORY_MAX_SIZE)
        {
            // 移除时清理无效实体
            CleanInvalidHistory();

            // 如果清理后仍然满，移除最早的
            if (history_list.Count >= HISTORY_MAX_SIZE)
            {
                history_list.RemoveFirst();
            }
        }

        // 添加到末尾
        history_list.AddLast(entity);
    }

    /// <summary>
    /// 在历史记录中查找实体
    /// </summary>
    private LinkedListNode<Entity> FindInHistory(Entity entity)
    {
        var node = history_list.First;
        while (node != null)
        {
            if (node.Value == entity)
            {
                return node;
            }
            node = node.Next;
        }
        return null;
    }

    /// <summary>
    /// 清理历史记录中的无效实体
    /// </summary>
    public void CleanInvalidHistory()
    {
        var node = history_list.First;
        while (node != null)
        {
            var next = node.Next;
            // 如果实体已被销毁或不合法，移除
            if (node.Value == null || node.Value.IsQueuedForDeletion() || !IsInstanceValid(node.Value))
            {
                history_list.Remove(node);
            }
            node = next;
        }
    }

    /// <summary>
    /// 获取历史记录的数组形式
    /// </summary>
    public Entity[] GetHistoryArray()
    {
        var array = new Entity[history_list.Count];
        history_list.CopyTo(array, 0);
        return array;
    }

    /// <summary>
    /// 检查实体是否在历史记录中
    /// </summary>
    public bool IsInHistory(Entity entity)
    {
        return FindInHistory(entity) != null;
    }

    /// <summary>
    /// 检查实体是否当前在碰撞箱内
    /// </summary>
    public bool IsInTrigger(Entity entity)
    {
        return entityList.Contains(entity);
    }

    #endregion
}
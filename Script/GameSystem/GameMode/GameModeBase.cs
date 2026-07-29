using Godot;
using System;

/// <summary>
/// 游戏模式基类
/// 
/// 所有游戏模式都应继承此类，通过重写方法来修改游戏玩法。
/// Game类在初始化时根据GameManager中的gameMode字符串选择对应的子类加载。
/// 
/// 生命周期：
///   OnInit -> OnStart -> OnLoad -> (游戏循环: OnProcess) -> OnNextRoom/OnNextLevel/OnGetItem... -> OnEnd
/// </summary>
public abstract partial class GameModeBase : RefCounted
{
    /// <summary>
    /// 模式名称（用于日志和调试）
    /// </summary>
    public virtual string ModeName => "BaseMode";

    protected Game game => GameManager.Instance.game;
    Player player => game.player;
    Camera2D camera => game.camera;

    /// <summary>
    /// 初始化模式 - 最先调用，用于设置模式相关的基础数据
    /// </summary>
    public virtual void OnInit()
    {
        GD.Print($"[GameMode:{ModeName}] 初始化");
    }

    /// <summary>
    /// 加载数据 - 在OnInit之后调用,用于加载存档或初始化关卡数据
    /// </summary>
    public virtual void OnLoad()
    {
        GD.Print($"[GameMode:{ModeName}] 加载数据");
    }


    /// <summary>
    /// 开始游戏 - 在OnLoad之后调用，用于游戏启动时逻辑
    /// </summary>
    public virtual void OnStart()
    {
        GD.Print($"[GameMode:{ModeName}] 开始游戏");
    }


    /// <summary>
    /// 每帧处理 - 在游戏的_Process中调用，用于模式特有的实时更新逻辑
    /// </summary>
    /// <param name="delta">帧间隔时间</param>
    public virtual void OnProcess(double delta)
    {
    }

    /// <summary>
    /// 物理帧处理 - 在游戏的_PhysicsProcess中调用
    /// </summary>
    /// <param name="delta">物理帧间隔时间</param>
    public virtual void OnPhysicsProcess(double delta)
    {
    }

    /// <summary>
    /// 进入下一个房间时调用
    /// </summary>
    /// <param name="roomIndex">房间索引</param>
    public virtual void OnNextRoom(int roomIndex)
    {
        GD.Print($"[GameMode:{ModeName}] 进入房间: {roomIndex}");
    }

    /// <summary>
    /// 进入下一层时调用
    /// </summary>
    /// <param name="levelIndex">层索引</param>
    public virtual void OnNextLevel(int levelIndex)
    {
        GD.Print($"[GameMode:{ModeName}] 进入下一层: {levelIndex}");
    }

    /// <summary>
    /// 获得物品时调用
    /// </summary>
    /// <param name="itemId">物品ID</param>
    /// <param name="quantity">数量</param>
    public virtual void OnGetItem(string itemId, int quantity = 1)
    {
        GD.Print($"[GameMode:{ModeName}] 获得物品: {itemId} x{quantity}");
    }

    /// <summary>
    /// 使用物品时调用
    /// </summary>
    /// <param name="itemId">物品ID</param>
    public virtual void OnUseItem(string itemId)
    {
        GD.Print($"[GameMode:{ModeName}] 使用物品: {itemId}");
    }

    /// <summary>
    /// 丢弃物品时调用
    /// </summary>
    /// <param name="itemId">物品ID</param>
    public virtual void OnDropItem(string itemId)
    {
        GD.Print($"[GameMode:{ModeName}] 丢弃物品: {itemId}");
    }

    /// <summary>
    /// 玩家受伤时调用
    /// </summary>
    /// <param name="damage">伤害值</param>
    /// <param name="source">伤害来源</param>
    public virtual void OnPlayerDamaged(float damage, string source = "")
    {
        GD.Print($"[GameMode:{ModeName}] 玩家受伤: {damage} 来自: {source}");
    }

    /// <summary>
    /// 玩家治疗时调用
    /// </summary>
    /// <param name="amount">治疗量</param>
    public virtual void OnPlayerHealed(float amount)
    {
        GD.Print($"[GameMode:{ModeName}] 玩家治疗: {amount}");
    }

    /// <summary>
    /// 玩家死亡时调用
    /// </summary>
    public virtual void OnPlayerDeath()
    {
        GD.Print($"[GameMode:{ModeName}] 玩家死亡");
    }

    /// <summary>
    /// 敌人被击杀时调用
    /// </summary>
    /// <param name="enemyId">敌人ID</param>
    /// <param name="enemyType">敌人类型</param>
    public virtual void OnEnemyKilled(string enemyId, string enemyType)
    {
        GD.Print($"[GameMode:{ModeName}] 敌人被击杀: {enemyType}");
    }

    /// <summary>
    /// 房间被清理时调用
    /// </summary>
    /// <param name="roomIndex">房间索引</param>
    public virtual void OnRoomCleared(int roomIndex)
    {
        GD.Print($"[GameMode:{ModeName}] 房间已清理: {roomIndex}");
    }

    /// <summary>
    /// Boss战开始时调用
    /// </summary>
    /// <param name="bossId">Boss ID</param>
    public virtual void OnBossStart(string bossId)
    {
        GD.Print($"[GameMode:{ModeName}] Boss战开始: {bossId}");
    }

    /// <summary>
    /// Boss被击败时调用
    /// </summary>
    /// <param name="bossId">Boss ID</param>
    public virtual void OnBossDefeated(string bossId)
    {
        GD.Print($"[GameMode:{ModeName}] Boss被击败: {bossId}");
    }

    /// <summary>
    /// 触发事件时调用
    /// </summary>
    /// <param name="eventName">事件名称</param>
    /// <param name="data">事件数据</param>
    public virtual void OnEventTriggered(string eventName, Variant data = default)
    {
        GD.Print($"[GameMode:{ModeName}] 事件触发: {eventName}");
    }

    /// <summary>
    /// 游戏结束时调用
    /// </summary>
    /// <param name="isVictory">是否胜利</param>
    public virtual void OnEnd(bool isVictory)
    {
        GD.Print($"[GameMode:{ModeName}] 游戏结束: {(isVictory ? "胜利" : "失败")}");
    }

    /// <summary>
    /// 清理模式 - 最后调用，用于释放资源
    /// </summary>
    public virtual void OnCleanup()
    {
        GD.Print($"[GameMode:{ModeName}] 清理完成");
    }
    
    # region 通用方法

    /// <summary>
    /// 摄像机跟随玩家
    /// </summary>
    public void CameraFollowPlayer()
    {
        if (game != null && game.player != null && game.camera != null)
        {
            game.camera.SetTargetPosition(game.player.Position);
        }
    }

    # endregion
}
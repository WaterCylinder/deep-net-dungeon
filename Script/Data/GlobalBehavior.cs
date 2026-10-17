using Godot;
using System;

/// <summary>
/// 全局行为类 - 提供不依赖具体实体类型的通用行为方法
/// </summary>
[GlobalClass]
public partial class GlobalBehavior : Behavior
{
    protected Game game => GameManager.Instance.game;
    protected Player player => game.player;

    /// <summary>
    /// 玩家是否可用，用于Effect的条件判断
    /// </summary>
    /// <returns></returns>
    public bool IsPlayerAble()
    {
        return Game.IsAble(player);
    }

    /// <summary>
    /// 输出调试信息
    /// </summary>
    /// <param name="info">信息内容</param>
    public void DebugLog(string info)
    {
        GD.Print($"[GlobalBehavior] {info}");
    }
}

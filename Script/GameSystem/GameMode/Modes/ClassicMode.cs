using Godot;
using System;

/// <summary>
/// 经典模式 - 默认的游戏模式
/// 标准的地牢探索玩法
/// </summary>
public partial class ClassicMode : GameModeBase
{
    public override string ModeName => "Classic";

    public override void OnInit()
    {
        base.OnInit();
        // 初始化经典模式特有的数据
    }

    public override void OnStart()
    {
        base.OnStart();
        // 启动经典模式游戏逻辑
    }

    public override void OnNextRoom(int roomIndex)
    {
        base.OnNextRoom(roomIndex);
        // 经典模式：房间推进逻辑
    }

    public override void OnGetItem(string itemId, int quantity = 1)
    {
        base.OnGetItem(itemId, quantity);
        // 经典模式：物品获取处理
    }
}
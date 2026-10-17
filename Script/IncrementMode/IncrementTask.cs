using Godot;
using System;

public enum IncrementTaskType
{
    /// <summary>
    /// 在指定回合前完成累计资源点目标的任务
    /// </summary>
    resourceTarget
}

[GlobalClass]
public partial class IncrementTask : Resource
{
    [Export] public IncrementTaskType taskType;
    [Export] public string intro;
    [Export] public int targetTurn = 1;
    [Export] public int targetResourcePoint = 1;
    
}

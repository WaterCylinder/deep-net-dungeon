using Godot;
using System;

public partial class IncrementTaskLabel : Label
{
    [Export] public IncrementTask task;
    [Export] public int value;

    IncrementWorld world => GameManager.Instance.game.incrementWorld;

    [Signal] public delegate void OnSuccessEventHandler();
    [Signal] public delegate void OnFailEventHandler();

    public void Update()
    {
        if(task == null)return;
        switch (task.taskType)
        {
            case IncrementTaskType.resourceTarget:
                if(world.turn > task.targetTurn)
                {
                    if(world.cumulativePoint < task.targetResourcePoint)
                    {
                        Fail();
                    }
                    else
                    {
                        Success();
                    }
                }
            break;
        }
        Text = task.intro;
    }

    public void Success()
    {
        EmitSignal(SignalName.OnSuccess);
    }

    public void Fail()
    {
        EmitSignal(SignalName.OnFail);
    }
}

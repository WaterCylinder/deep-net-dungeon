using Godot;
using Godot.Collections;

public partial class TriggerDialogue : Area2D
{
    [Export]public Array<string> contents = new();
    [Export]public Node2D dialoguePoint;
    [Export]public int triggerCount = 0;
    public bool AlreadyTrigger => triggerCount > 1;
    [Export]public Dialogue dialogue;
    [Export]public Effect onDialogueEnd;
    public override void _Ready()
    {
        BodyEntered += body =>
        {
            if (body is Player)
            {
                if (!Game.IsAble(dialogue))
                {
                    Vector2 pos = GlobalPosition;
                    if(dialoguePoint != null)
                    {
                        pos = dialoguePoint.GlobalPosition;
                    }
                    dialogue = Dialogue.CreateDialogue(contents, pos,
                        jitterStrength: 1
                    );
                    dialogue.OnEnd += () =>
                    {
                        onDialogueEnd?.Run(this);  
                    };
                    triggerCount += 1;
                }
            }
        };
    }

}

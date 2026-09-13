using Godot;
using Godot.Collections;

public partial class TriggerDialogue : Area2D
{
    [Export]public Array<string> contents = new();
    [Export]public Node2D dialoguePoint;
    public Dialogue dialogue;
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
                }
            }
        };
    }

}

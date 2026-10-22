using Godot;
using System;

public partial class EndUI : Control
{
    [Export] public Button returnButton;
    [Export] public Button replayButton;
    [Export] public Label endText;

    public override void _Ready()
    {
        base._Ready();

        returnButton.Pressed += SceneManager.Instance.GoBackToMainMenu;
        replayButton.Pressed += () => SceneManager.Instance.SwitchScene("Game");
    }

}

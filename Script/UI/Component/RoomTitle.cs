using Godot;
using System;

public partial class RoomTitle : Control
{
    [Export] public RoomTileMap roomMap;
    [Export] public Label label;

    public override void _Process(double delta)
    {
        if(roomMap != null)
        {
            if(roomMap.room != null)
            {
                label.Text = roomMap.room.roomTitle;
            }
            else
            {
                label.Text = "No_Room";
            }
        }
        else
        {
            label.Text = "No_TileMap";
        }
    }

}

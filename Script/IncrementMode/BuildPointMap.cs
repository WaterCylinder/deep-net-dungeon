using System;
using Godot;
using Godot.Collections;

public partial class BuildPointMap : Node2D
{
    [Export] public Array<Array<BuildPoint>> pointMap = new();

    public Array<BuildPoint> allPoints = new();

    public override void _Ready()
    {
        base._Ready();
        var children = GetChildren();
        Vector2I pos = Vector2I.Zero;
        foreach(var line in children)
        {
            Array<BuildPoint> points = new();
            var nodes = line.GetChildren();
            foreach(var node in nodes)
            {
                BuildPoint point = node as BuildPoint;
                point.pos = pos;
                points.Add(point);
                allPoints.Add(point);
                pos.Y += 1;
            }
            pointMap.Add(points);
            pos.X += 1;
            pos.Y = 0;
        }
    }

    public BuildPoint Get(Vector2I pos)
    {
        if(pos.X < 0 || pos.X >= pointMap.Count)
        {
            return null;
        }
        if(pos.Y < 0 || pos.Y >= pointMap[pos.X].Count)
        {
            return null;
        }
        return pointMap[pos.X][pos.Y];
    }

}

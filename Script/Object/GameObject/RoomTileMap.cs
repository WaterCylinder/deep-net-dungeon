using Godot;
using System;

public partial class RoomTileMap : Node2D
{
    [Export] public TileMapLayer trrainLayer;
    [Export] public Node2D enemyGeneratorNode;
    [Export] public Node2D doorsNode;

    public Room room;

    public void Initialize(Room room)
    {
        this.room = room;
    }

    public void SpawnAllEnemy()
    {
        if(enemyGeneratorNode == null)
        {
            GD.Print("房间未设置敌人生成器节点");
            return;
        }

        //使房间里的怪物生成器进行怪物的生成。
        foreach(var child in enemyGeneratorNode.GetChildren())
        {
            if(child is EnemyGenerator generator)
            {
                generator.GenerateEnemy(room.enemyNode);
            }
        }
    }
    
    public void SpawnDoors()
    {
        
    }
}

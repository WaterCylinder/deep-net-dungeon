using Godot;
using Godot.Collections;
using Utils;

public partial class RoomTileMap : Node2D
{
    [Export] public TileMapLayer trrainLayer;
    [Export] public Node2D enemyGeneratorNode;
    [Export] public Node2D itemGeneratorNode;
    [Export] public Node2D doorsNode;

    public Room room;

    Array<Array<EnemyGenerator>> enemyGenerators = [];
    Array<ItemGenerator> itemGenerators = [];
    Array<EnemyGenerator> generatorNow = [];
    int enemyGeneratorIndex = 0;
    Array<Enemy> enemiesNow = [];

    public void Initialize(Room room)
    {
        this.room = room;
        //房间初始化时生成门
        SetDoors();
    }

    /// <summary>
    /// 获取所有的生成器节点
    /// </summary>
    void GetAllEnemyGenerators()
    {
        if(enemyGeneratorNode == null)
        {
            GD.Print("房间未设置敌人生成器节点");
            return;
        }
        GD.Print($"房间{room.Name}的敌人生成器节点{enemyGeneratorNode.Name}下有{enemyGeneratorNode.GetChildCount()}个子节点");
        if(enemyGenerators.Count > 0)
        {
            return;
        }
        foreach(Node child in enemyGeneratorNode.GetChildren())
        {
            var generators = Tools.GetAllChildren<EnemyGenerator>(child);
            enemyGenerators.Add(generators);
            GD.Print($"房间{room.Name}的生成器节点{child.Name}下有{generators.Count}个敌人生成器");
        }
    }

    /// <summary>
    /// 获取所有的物品生成器
    /// </summary>
    void GetAllItemGenerators()
    {
        if(itemGeneratorNode == null)
        {
            GD.Print("房间未设置物品生成器节点");
            return;
        }
        foreach(Node child in itemGeneratorNode.GetChildren())
        {
            if(child is ItemGenerator generator)
                itemGenerators.Add(generator);
        }
        
    }

    /// <summary>
    /// 生成第n个生成器节点下的所有敌人
    /// </summary>
    /// <param name="index"></param>
    public bool SpawnEnemy(int index)
    {
        GetAllEnemyGenerators();
        if(index < 0 || index >= enemyGenerators.Count)
        {
            return false;
        }
        int count = 0;
        enemiesNow.Clear();
        generatorNow.Clear();
        foreach(EnemyGenerator generator in enemyGenerators[index])
        {
            generatorNow.Add(generator);
            generator.AfterGenerate += enemiesNow.Add;
            generator.GenerateEnemy(room.enemyNode);
            count++;
        }
        GD.Print($"房间{room.Name}部署了{count}个敌怪生成器");
        return true;
    }

    /// <summary>
    /// 生成所有的物品
    /// </summary>
    /// <returns></returns>
    public bool SpawnItem()
    {
        GetAllItemGenerators();
        if(itemGenerators.Count <= 0)
        {
            return false;
        }
        int count = 0;
        foreach(ItemGenerator generator in itemGenerators)
        {
            generator.GenerateItem();
            count++;
        }
        GD.Print($"房间{room.Name}部署了{count}个物品生成器");
        return true;
    }

    /// <summary>
    /// 生成下一个生成器节点下的所有敌人
    /// </summary>
    public bool SpawnEnemyNext()
    {
        if(SpawnEnemy(enemyGeneratorIndex))
        {
            enemyGeneratorIndex++;
            return true;
        }
        else
        {
            GD.Print("没有更多的敌人生成器了");
            return false;
        }
    }

    /// <summary>
    /// 检查当前生成器是否结束其生成
    /// </summary>
    /// <returns></returns>
    public bool CheckGeneratorNowFinished()
    {
        foreach(EnemyGenerator g in generatorNow)
        {
            if(Game.IsAble(g) && !g.finished)
            {
                return false;
            }
        }
        return true;
    }
    /// <summary>
    /// 检查当前房间生成的敌人是否全部死亡
    /// </summary>
    /// <returns></returns>
    public bool CheckEnemiesAllDead()
    {
        //检查当前房间的敌人是否全部死亡
        bool allDead = true;

        foreach(Enemy enemy in enemiesNow)
        {
            if(Game.IsAble(enemy) && !enemy.IsDead)
            {
                allDead = false;
                break;
            }
        }

        if(allDead)
        {
            GD.Print($"房间{room.Name}的所有敌人已死亡");
            enemiesNow.Clear();
            return true;
        }

        return false;
    }
    
    public void SetDoors()
    {
        if(doorsNode == null)
        {
            GD.Print("房间未设置门节点");
            return;
        }
        //使房间里的怪物生成器进行怪物的生成。
        foreach(var child in doorsNode.GetChildren())
        {
            if(child is Door door)
            {
                door.Owner = null;
                door.Reparent(room.doorNode);
                door.room = room;
                //对门进行分类记录
                if(door.doorType == DoorType.Next)
                {
                    room.nextDoors.Add(door);
                }
                else if(door.doorType == DoorType.Back)
                {
                    room.returnDoor = door;
                }
                else if(door.doorType == DoorType.Other)
                {
                    room.otherDoors.Add(door);
                }
                door.Initialize();
            }
        }
    }
}

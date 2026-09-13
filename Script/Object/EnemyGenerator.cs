using Godot;
using System;
using System.Threading;

public partial class EnemyGenerator : Node2D
{
    /// <summary>
    /// 创建EnemyGenerator实例并挂载到父节点下
    /// </summary>
    /// <param name="enemyData">敌人数据</param>
    /// <param name="position">生成位置</param>
    /// <param name="parent">父节点</param>
    /// <param name="immediate">是否立即生成敌人</param>
    /// <returns>创建的EnemyGenerator实例</returns>
    public static EnemyGenerator Create(EnemyData enemyData, Vector2 position, Node parent, bool immediate = true)
    {
        EnemyGenerator generator = new()
        {
            enemyData = enemyData,
            Position = position,
            GenerateImmediately = immediate
        };

        parent.AddChild(generator);
        return generator;
    }
    [Export] public EnemyData enemyData;

    [Export] public bool GenerateImmediately = false;

    [Export] public bool GenerateAnimation = false;

    public override void _Ready()
    {
        //生成器在运行时不可见
        Visible = false;
        if (GenerateImmediately)
        {
            GenerateEnemy();
        }
    }

    public Enemy GenerateEnemy(Node2D parent = null, bool del = true)
    {
        //TODO: 生成器动画以及是否采用动画的判断条件实现

        //在当前位置生成敌人
        Enemy enemy = GameManager.Instance.game.SpawnEnemy(enemyData, Position, parent);
        if(del) QueueFree(); //生成后销毁生成器
        return enemy;
    }
}
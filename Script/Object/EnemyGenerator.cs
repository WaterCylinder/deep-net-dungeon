using System;
using System.ComponentModel;
using Godot;
using Godot.Collections;
using Utils;

/// <summary>
/// 敌怪生成器，用于生成敌怪，需要注意的是列表生成功能没有对玩家位置的判断，可能会导致敌怪生成在玩家位置。
/// </summary>
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
    [Export] public bool GenerateList = false;
    [Export] public bool GenerateListRandom = false;
    [Export] public Array<EnemyData> dataList;
    [Export] public Enemy enemyNow;

    [Export] public bool finished = false;

    public Array<EnemyData> listCache = null;
    protected Node2D generateParent;
    protected bool deleteAfterGenerate = true;

    [Signal]
    public delegate void BeforeGenerateEventHandler(EnemyData data);

    [Signal]
    public delegate void AfterGenerateEventHandler(Enemy enemy);

    public Random _random = null;
    public Random random
    {
        get
        {
            if(_random == null)
            {
                _random = GameManager.Instance.game.randomSystem.GetGenerator(null);
            }
            return _random;
        }
    }

    public override void _Ready()
    {
        //生成器在运行时不可见
        Visible = false;
        if (GenerateImmediately)
        {
            GenerateEnemy();
        }
    }

    /// <summary>
    /// 生成敌怪
    /// </summary>
    /// <param name="parent">生成的敌怪的父对象</param>
    /// <param name="del">是否生成完毕后移除生成器</param>
    /// <returns></returns>
    public virtual Enemy GenerateEnemy(Node2D parent = null, bool del = true)
    {
        finished = false;
        if (GenerateList)
        {
            //敌怪列表生成
            if(listCache == null)
            {
                listCache = dataList.Duplicate(false);
                if (GenerateListRandom)
                {
                    Tools.Shuffle(listCache, random);
                }
            }
            generateParent = parent;
            deleteAfterGenerate = del;
            return null;
        }
        else
        {
            //单个敌怪生成
            Enemy enemy = GenerateEnemy(enemyData, parent);
            deleteAfterGenerate = del;
            FinishGenerate();
            return enemy;
        }
    }

    public Enemy GenerateEnemy(EnemyData data, Node2D parent, Vector2 pos = default)
    {
        EmitSignal(nameof(BeforeGenerate), data);
        if(pos == default)
        {
            pos = GlobalPosition;
        }
        enemyNow = GameManager.Instance.game.SpawnEnemy(data, pos, parent);
        GD.Print($"生成敌怪 {enemyNow.Name}");
        EmitSignal(nameof(AfterGenerate), enemyNow);
        return enemyNow;
    }

    public override void _Process(double delta)
    {
        if(listCache != null)
        {
            
            if(listCache.Count != 0)
            {
                if (!Game.IsAble(enemyNow))
                {
                    enemyData = listCache[^1];
                    listCache.RemoveAt(listCache.Count - 1);
                    GenerateEnemy(enemyData, generateParent);
                }
            }
            else
            {
                FinishGenerate();
            }
        }
    }

    protected void FinishGenerate()
    {
        listCache = null;
        finished = true;
        if(deleteAfterGenerate)QueueFree();
    }

}
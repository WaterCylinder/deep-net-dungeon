using System.Linq;
using Godot;
using Godot.Collections;
using Utils;

/// <summary>
/// 集群式敌怪生成器，用于在房间中随机生成敌怪，自带避开其他实体的位置判断
/// </summary>
[Tool]
public partial class EnemyGeneratorCluster : EnemyGenerator
{
    [Export] public Area2D spawnArea;
    [Export] public Vector2I onceSpawnRange = new(1, 1);
    [Export] public float spawnTime = 3;
    [Export] public int spawnNum;
    [Export] public Dictionary<EnemyData,int> clusterData;
    private Shape2D _shape;
    [Export] public Shape2D areaShape
    {
        get => _shape;
        set
        {
            // 先断开旧资源的事件，避免重复连接和内存泄漏
            if (_shape != null)
                _shape.Changed -= OnShapeChanged;

            _shape = value;

            // 连接新资源的事件
            if (_shape != null)
                _shape.Changed += OnShapeChanged;

            QueueRedraw();
        }
    }
    private void OnShapeChanged()
    {
        QueueRedraw();
    }

    public int spawnNumNow = 0;
    public bool generateCluster = false;
    public System.Collections.Generic.Dictionary<EnemyData, int> cache = null;

    public override Enemy GenerateEnemy(Node2D parent = null, bool del = true)
    {
        spawnArea.GetChild<CollisionShape2D>(0).Shape = areaShape;
        spawnNumNow = 0;
        finished = false;
        generateCluster = true;
        generateParent = parent;
        deleteAfterGenerate = del;
        timer = 5;
        return null;
    }

    public void GenerateCluster(Node2D parent)
    {
        Game game = GameManager.Instance.game;
        
        if(cache == null)
        {
            cache = clusterData.ToDictionary();
        }
        if (!Game.IsAble(game.player))
        {
            return;
        }
        int spawnCount = (int)random.NextInt64(onceSpawnRange.X, onceSpawnRange.Y + 1);
        var posList = Tools.GetSpawnPoints(spawnArea, [game.player.GlobalPosition], spawnCount, random);
        foreach(Vector2 pos in posList)
        {
            var datas = Tools.PickWeighted(cache, 1, random);
            EnemyData data;
            if (datas.Count > 0)data = datas[0];
            else return;
            
            //GD.Print($"[集群随机生成] 生成敌怪{data.enemyName}在{pos}");

            GenerateEnemy(data, parent, pos);
        }
    }

    float timer = 0;

    public override void _Process(double delta)
    {
        // 检查是否需要生成集群
        if (generateCluster)
        {
            timer += (float)delta;
            if(timer > spawnTime)
            {
                GD.Print($"[集群随机生成] 生成第{spawnNumNow}波");
                GenerateCluster(generateParent);
                spawnNumNow++;
                timer = 0;
                if(spawnNumNow >= spawnNum)
                {
                    spawnNumNow = 0;
                    generateCluster = false;
                    cache = null;
                    FinishGenerate();
                }
            }
        }   
        else
        {
            timer = 0;
        }
    }

    public override void _Draw()
    {
        if (_shape != null)
        {
            // 第一个参数是画布项，第二个参数是绘制颜色
            _shape.Draw(GetCanvasItem(), new Color(0.7f, 0.1f, 0.1f, 0.3f));
        }
    }


}

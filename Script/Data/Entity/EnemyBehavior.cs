using Godot;
using System;

[GlobalClass]
public partial class EnemyBehavior : Behavior
{
    protected Game game => GameManager.Instance.game;
    protected Player player => game.player;
    public void Test(Enemy enemy)
    {
        GD.Print($"触发敌人行为: Enemy={enemy.Name}");
    }

    # region 通用方法

    public void Attack(Enemy enemy)
    {
        enemy.Attack();
    }

    public void FaceToPlayer(Enemy enemy)
    {
        if(player == null || !IsInstanceValid(player) || player.IsQueuedForDeletion())return;

        float diff = player.GlobalPosition.X - enemy.GlobalPosition.X;
        if(diff > 0)
            diff = 1;
        else if(diff < 0)
            diff = -1;
        enemy.ScaleLayout(new Vector2(diff, 1));
    }

    /// <summary>
    /// 朝向移动房间（仅左右）
    /// </summary>
    /// <param name="enemy"></param>
    public void FaceToMove(Enemy enemy)
    {
        float diff = enemy.GetVelocity().X;
        if(diff < 0)
            diff = -1;
        else
            diff = 1;
        enemy.ScaleLayout(new Vector2(diff, 1));
    }

    /// <summary>
    /// 朝向玩家移动：优先沿"敌怪所在房间的 tilemap 导航层"寻路，
    /// 没有房间 / 玩家不在 tilemap 范围内 / 找不到路径时，退化成直接朝玩家位置移动。
    /// 参数：
    /// - repath_interval：重新寻路的间隔（秒），默认 0.3
    /// - look_ahead：跳过路点的距离（像素），默认 24
    /// </summary>
    /// <param name="enemy"></param>
    public void UpdateCloseToPalyer(Enemy enemy)
    {
        if(!Game.IsAble(enemy) || !Game.IsAble(player))return;

        //玩家是否还在当前房间的tilemap范围内
        bool IsPlayerInTileMap(TileMapLayer tileLayer)
        {
            Rect2I usedRect = tileLayer.GetUsedRect();
            Vector2I cell = tileLayer.LocalToMap(tileLayer.ToLocal(player.GlobalPosition));
            return usedRect.HasPoint(cell);
        }

        TileMapLayer layer = enemy.room?.tileMap?.trrainLayer;

        //没有房间/没有tilemap/玩家不在tilemap里：直接朝玩家位置移动
        if(layer == null || !layer.IsInsideTree() || !IsPlayerInTileMap(layer))
        {
            enemy.MoveTarget(player.GlobalPosition);
            return;
        }

        Rid navMap = layer.GetNavigationMap();
        if(!navMap.IsValid)
        {
            enemy.MoveTarget(player.GlobalPosition);
            return;
        }

        float repathInterval = Arg("repath_interval", 0.3f);
        float lookAhead = Arg("look_ahead", 24f);

        //按间隔重新寻路；两次寻路之间沿用缓存的路点。
        //这里必须用 Add 初始化：args 里缺失的引用类型键第一次 Get 时存的是 null，
        //之后再 Get 会拿回 null（不是空数组），直接 .Count 就会空引用
        enemy.args.Add("nav_repath_timer", 0f);
        enemy.args.Add("nav_path", new Godot.Collections.Array());

        float repathTimer = enemy.args.Get<float>("nav_repath_timer") + (float)enemy.GetProcessDeltaTime();
        Godot.Collections.Array navPath = enemy.args.Get<Godot.Collections.Array>("nav_path") ?? new Godot.Collections.Array();

        if(repathTimer >= repathInterval || navPath.Count == 0)
        {
            repathTimer = 0f;

            navPath = new Godot.Collections.Array();
            foreach(Vector2 point in NavigationServer2D.MapGetPath(navMap, enemy.GlobalPosition, player.GlobalPosition, true))
            {
                navPath.Add(point);
            }
        }

        enemy.args.Set("nav_repath_timer", repathTimer);
        enemy.args.Set("nav_path", navPath);

        //没有可行路径（玩家在墙里/被门隔开等）：直接朝玩家走
        if(navPath.Count < 2)
        {
            enemy.MoveTarget(player.GlobalPosition);
            return;
        }

        //跳过离自己太近的路点，取第一个"够远"的点当目标，避免在一堆近点上来回抖
        int index = 1;
        while(index < navPath.Count - 1 && enemy.GlobalPosition.DistanceTo(navPath[index].As<Vector2>()) < lookAhead)
        {
            index++;
        }

        Vector2 waypoint = navPath[index].As<Vector2>();
        Vector2 towardWaypoint = waypoint - enemy.GlobalPosition;
        if(towardWaypoint.LengthSquared() < 0.0001f)
        {
            enemy.MoveTarget(waypoint);
            return;
        }
        towardWaypoint = towardWaypoint.Normalized();

        //墙体擦碰处理：导航多边形是贴着障碍物边缘的，路点/路径会压着墙角走，
        //而敌怪是有碰撞半径的刚体——直冲拐角就会把自己怼进墙里卡住。
        //这里在前进方向短距离探一条射线，撞墙就沿墙面切向滑过去，别继续往墙里顶。
        float wallProbe = Arg("wall_probe", 48f);
        PhysicsRayQueryParameters2D probe = PhysicsRayQueryParameters2D.Create(
            enemy.GlobalPosition,
            enemy.GlobalPosition + towardWaypoint * wallProbe,
            128);
        Godot.Collections.Dictionary wallHit = enemy.GetWorld2D().DirectSpaceState.IntersectRay(probe);

        if(wallHit.Count > 0)
        {
            Vector2 normal = (Vector2)wallHit["normal"];
            //去掉朝墙里的分量，只保留贴墙滑动方向
            Vector2 slide = towardWaypoint - normal * towardWaypoint.Dot(normal);
            if(slide.LengthSquared() > 0.0001f)
            {
                enemy.MoveToward(slide.Normalized());
                return;
            }
        }

        enemy.MoveTarget(waypoint);
    }

    /// <summary>
    /// 朝向玩家发射子弹
    /// </summary>
    /// <param name="enemy"></param>
    public void AttackShotPlayer(Enemy enemy)
    {
        Vector2 toward = player.GlobalPosition - enemy.GlobalPosition;

        float bulletSpeed = Arg("speed", 3);

        enemy.Shot(toward.Normalized() * bulletSpeed);
    }
    
    /// <summary>
    /// 切换弹幕
    /// </summary>
    /// <param name="enemy"></param>
    public void ChangeShotBullet(Enemy enemy)
    {
        BulletData data = enemy.args.Get<BulletData>("bullet_data");
        GD.Print($"{enemy.Name}设置弹幕{data.bulletName}");
        enemy.ChangeBullet(data);
    }

    /// <summary>
    /// 朝向玩家发射四方向弹幕：
    /// 以"敌人 → 玩家"的方向为基准，再分别旋转 90°、180°、270°，共四发呈十字形散开。
    /// 参数：
    /// - speed：弹速，默认 3（和 AttackShotPlayer 一致）
    /// - start_angle：整体起始偏移角（度），默认 0；填 45 就变成"×"形
    /// - bullet_data：可选，指定本次使用的弹幕数据；不填则用敌人当前弹幕
    /// </summary>
    /// <param name="enemy"></param>
    public void AttackShotPlayerFourDirection(Enemy enemy)
    {
        if(!Game.IsAble(enemy) || !Game.IsAble(player))return;

        Vector2 toward = player.GlobalPosition - enemy.GlobalPosition;
        //玩家和敌人完全重叠时Normalized()会得到零向量，四发弹幕会全部卡在原地，这里退化成朝右
        if(toward.LengthSquared() < 0.0001f)
            toward = Vector2.Right;

        float bulletSpeed = Arg("speed", 3);
        float startAngle = Arg("start_angle", 0f);
        BulletData data = Arg<BulletData>("bullet_data", null);

        for(int i = 0; i < 4; i++)
        {
            float angle = Mathf.DegToRad(startAngle + 90f * i);
            enemy.Shot(toward.Normalized().Rotated(angle) * bulletSpeed, default, data);
        }
    }

    # endregion

}

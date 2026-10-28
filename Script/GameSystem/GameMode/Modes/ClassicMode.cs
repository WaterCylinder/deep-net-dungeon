using Godot;
using System;

/// <summary>
/// 经典模式 - 默认的游戏模式
/// 标准的地牢探索玩法
/// </summary>
public partial class ClassicMode : GameModeBase
{
    public override string ModeName => "Classic";

    public int level = 1;
    public MapData mapData = null;

    bool firstGetUseableItem = true;

    public override void OnStart()
    {
        base.OnStart();
        //设置摄像机无限移动范围
        camera.posLimitX = new Vector2(-Mathf.Inf, Mathf.Inf);
        camera.posLimitY = new Vector2(-Mathf.Inf, Mathf.Inf);
        //传送时重置摄像机位置
        player.OnTrans += camera.SetPosition;
        //打开黑幕
        game.hud.SetBlack(0);
    }

    MapData GetMapData(int level)
    {
        this.level = level;

        var data = AssetManager.Instance.GetData<MapData>($"Resource.v1.MapData.level{level}", null);
        if(data == null)
        {
            return GetMapData(2);
        }
        else
        {
            return data;
        }
    }

    void Init()
    {
    }

    public override void OnProcess(double delta)
    {
        base.OnProcess(delta);
        switch (game.A)
        {
            case 0:
                //初始化生成地图
                Init();
                map.Initialize(GetMapData(1), game.randomSystem);
                UpdateBackground();   //回到第1层时把bg2/bg3收起来
                if(map.bossRoom != null)
                {
                    foreach(Door door in map.bossRoom.otherDoors)
                    {
                        door.OnEnter += (e) => NextLevel();
                    }
                }
                game.camera.Zoom = new Vector2(5,5);
                game.camera.targetZoom = 5;
                game.Next();
            break;
            case 100:
                //等待黑幕
                player.TransTo(map.startRoom.GlobalPosition);
                map.startRoom.Enter(player);
                player.opera += 1;
                player.animeNode.Play("prone");
                player.animeNode.Connect(
                    AnimatedSprite2D.SignalName.AnimationFinished,
                    Callable.From(() => game.Next()),
                    (uint)ConnectFlags.OneShot
                );
                game.hud.SetBlack(0.1f);
                game.Next();
            break;
            case 200:
                //等待动画
            break;
            case 300:
                player.opera -= 1;
                game.camera.targetZoom = 1;
                game.hud.BlackOpen(0.1f);
                game.Next();
            break;
            case 400:
                game.hud.ShowTips(0);
                game.Next();
            break;
            case 500:
                //游戏中
            break;
            case 600:
                //玩家死亡
            break;

            //进入下层
            case 1600:
                //玩家进入下一层的动画开始
                game.hud.BlackClose();
                player.opera += 1;
                player.MoveStop();
                game.Next();
            break;
            case 1700:
                player.MoveStop();
                if(game.hud.blackEnd)
                {
                    game.Next();
                }
            break;
            case 1800:
                //换层统一走ChangeLevel：地图重建、门绑定、玩家传送、背景切换都在里面，
                //以前这里是内联复制的一份，漏掉了背景切换，导致后面几层背景不变
                ChangeLevel();
                game.hud.BlackOpen();
                game.Next();
            break;
            case 1900:
                if(game.hud.blackEnd)
                {
                    //前往游戏等待
                    player.opera -= 1;
                    game.GoTo(500);
                }
            break;

        }
    }

    public override void OnPlayerDeath()
    {
        base.OnPlayerDeath();
        GD.Print("玩家死亡");
        //默认进入死亡界面
        game.hud.endUi.Visible = true;
        game.hud.endUi.endText.Text = "失败";
        game.Next();
    }

    public override void OnPhysicsProcess(double delta)
    {
        base.OnPhysicsProcess(delta);
        //对其摄像机到玩家位置
        CameraFollowPlayer();
        //检测玩家主动道具
        if(player.usableItem != null)
        {
            if (firstGetUseableItem)
            {
                firstGetUseableItem = false;
                game.hud.ShowTips(1);
            }
        }
    }

    //非事件房间清理完之后自动吸取房间内所有资源点
    public override void OnRoomCleared(Room room)
    {
        base.OnRoomCleared(room);
        if (!room.CheckTag(RoomTag.Event))
        {
            foreach(var point in room.GetChildren())
            {
                if(point is EnergyPoint ep)
                {
                    if(ep.energyType == EnergyType.Coin)
                    {
                        ep.target = player;
                    }
                }
            }
        }
        
    }

    public override void OnEnemySpawned(Enemy enemy)
    {
        base.OnEnemySpawned(enemy);
        float levelUp = 0.3f;
        //根据level提升敌怪属性，提升为当前等级 - 1乘以30%
        enemy.attack.Multi(1 + (level - 1) * levelUp, "LEVEL", 80);
        enemy.maxHealth.Multi(1 + (level - 1) * levelUp, "LEVEL", 80);
        enemy.maxHealth.Add((level - 1) * 30f, "LEVEL", 80);
        enemy.Heal(enemy.maxHealth);
        enemy.defense.Multi(1 + (level - 1) * levelUp, "LEVEL", 80);
        enemy.shield *= 1 + (level - 1) * levelUp;
    }

    public void NextLevel()
    {
        level+=1;
        game.GoTo(1600);
    }

    /// <summary>
    /// 按当前层数切换背景：第3层起显示bg2、第5层起显示bg3。
    /// 这里用"赋值"而不是"只置为可见"：重开一局回到第1层时才能把bg2/bg3正确隐藏
    /// </summary>
    void UpdateBackground()
    {
        if(game.bg == null)
        {
            return;
        }

        game.bg.bg2.Visible = level > 2;
        game.bg.bg3.Visible = level > 4;
    }

    public void ChangeLevel()
    {
        mapData = GetMapData(level);
        UpdateBackground();
        map.Initialize(mapData, game.randomSystem);
        if(map.bossRoom != null)
        {
            foreach(Door door in map.bossRoom.otherDoors)
            {
                door.OnEnter += (e) => NextLevel();
            }
        }
        player.TransTo(map.startRoom.GlobalPosition);
        map.startRoom.Enter(player);
    }

    public override void OnEnd(bool isVictory)
    {
        GD.Print("游戏结束");
        SceneManager.Instance.GoNextScene("GameEnd");
    }

    /// <summary>
    /// 当前层对应的关卡音乐（1~2层 / 3~4层 / 5层起）
    /// </summary>
    string GetLevelMusicName()
    {
        if(level <= 2)
        {
            return "Level12";
        }

        if(level <= 4)
        {
            return "Level34";
        }

        return "Level5";
    }

    /// <summary>
    /// 按房间切换音乐：第4层Boss房用专属Boss曲，其余按层数用关卡曲，都走渐变切换
    /// </summary>
    void UpdateMusic(Room room)
    {
        if(game == null)
        {
            return;
        }

        string musicName = GetLevelMusicName();
        if(level == 4 && room != null && room.CheckTag(RoomTag.Boss))
        {
            musicName = "Level4Boss";
        }

        AudioStream music = AssetManager.Instance?.GetMusic(musicName);
        if(music == null)
        {
            return;
        }

        game.PlayMusic(music, 1.5f);
    }

    public override void OnRoomEnter(Room room)
    {
        base.OnRoomEnter(room);
        UpdateMusic(room);

        //第3层起商店商品价格翻倍
        if(level >= 3 && room != null && room.CheckTag(RoomTag.Shop))
        {
            UpdateShopPrice(room);
        }
    }

    /// <summary>
    /// 商店价格翻倍：把每个商品的加价设成道具原价，最终价 = 原价 + 加价 = 2倍。
    /// 用节点元数据标记，重复进出同一个商店房不会叠加；
    /// 商品被刷新（换道具）时也要重新翻倍，所以接一次 OnShopItemReset
    /// </summary>
    void UpdateShopPrice(Room room)
    {
        foreach(ShopItem shopItem in Utils.Tools.GetAllChildren<ShopItem>(room))
        {
            if(shopItem == null || shopItem.HasMeta("level_price_doubled"))
            {
                continue;
            }

            shopItem.SetMeta("level_price_doubled", true);
            shopItem.OnShopItemReset += () => DoubleShopItemPrice(shopItem);
            DoubleShopItemPrice(shopItem);
        }
    }

    /// <summary>
    /// 把单个商品的价格翻倍：加价 = 道具原价（ShopItem每帧用 原价+upPrice 算价格）
    /// </summary>
    void DoubleShopItemPrice(ShopItem shopItem)
    {
        if(shopItem == null || shopItem.data == null)
        {
            return;
        }

        shopItem.upPrice = shopItem.GetPrice(shopItem.data);
    }
}
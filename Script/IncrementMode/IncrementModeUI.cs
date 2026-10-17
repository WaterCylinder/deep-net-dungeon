using Godot;
using System;

public partial class IncrementModeUI : Control
{
    [Export] public TextureButton nextTurnButton;
    [Export] public Label turnLabel;
    [Export] public Label resourceLabel;
    [Export] public Control resourceTip;
    [Export] public Label cumulativeLabel;

    [Export] public Control buildingPanelNode;
    [Export] public TextureButton buildingPanelButton;
    [Export] public Control buildingPanelContainer;
    [Export] public bool buildingPanelShowout = true;
    public float buildingPanelStartPosY = 0;

    [Export] public PackedScene buildingPanelItemPrefab;
    [Export] public TextureRect buildingItemfollow;
    [Export] public BuildingItemController buildingItemController;

    [Export] public BuildingInfo info;

    [Export] public Control shopNode;
    [Export] public Control shopContainer;
    [Export] public PackedScene shopItemPrefab;
    [Export] public Label shopRefreshCostLabel;
    [Export] public Button shopRefresh;
    [Export] public Button shopEnd;
    [Export] public Button shopStow;
    public bool isStow = false;
    

    [Export] public Control taskContainer;
    [Export] public PackedScene taskLabel;

    [Export] public Control end;
    [Export] public Label endLabel;
    [Export] public Label endScore;
    [Export] public Button returnButton;


    [Export] public Control tipsNode;
    [Export] public Control showNode;
    [Export] public Label showLabel;

    [Export] public AudioStream uiSound;
    /// <summary>
    /// 资源点获取音效
    /// </summary>
    [Export] public AudioStream scoreSound;

    Game game => GameManager.Instance.game;
    IncrementWorld world => game.incrementWorld;

    public override void _Ready()
    {
        base._Ready();
        nextTurnButton.Pressed += () =>
        {
            //购物时不相应
            if(world.shoping)return;
            world.TurnEnd();
            turnLabel.Text = $"第{world.turn}回合";

            Tips("下一回合").SetColor(Colors.Yellow);

            game.PlaySound(uiSound);
            ///重置商店位置
            if (isStow)
            {
                isStow = false;
                float x = shopNode.Position.X + shopNode.Size.X;
                shopNode.Position = new Vector2(x, shopNode.Position.Y);
                shopStow.Text = "收起";
            }

            game.PlaySound(scoreSound);
        };

        buildingPanelStartPosY = buildingPanelNode.Position.Y;
        float buildingPanelTargetPosY =  buildingPanelStartPosY + DisplayServer.WindowGetSize().Y * 0.2f;

        buildingPanelButton.Pressed += () =>
        {
            if (buildingPanelShowout)
            {
                Tween tween = CreateTween();
                tween.TweenMethod(
                    Callable.From((float v) =>
                    {
                        buildingPanelNode.Position = new Vector2(0, v);
                    }),
                    buildingPanelStartPosY, buildingPanelTargetPosY, 0.1f
                );
                tween.Finished += () =>
                {
                    buildingPanelShowout = false;
                };
            }
            else
            {
                Tween tween = CreateTween();
                tween.TweenMethod(
                    Callable.From((float v) =>
                    {
                        buildingPanelNode.Position = new Vector2(0, v);
                    }),
                    buildingPanelTargetPosY, buildingPanelStartPosY, 0.1f
                );
                tween.Finished += () =>
                {
                    buildingPanelShowout = true;
                };
            }
        };
    
        shopEnd.Pressed += () =>
        {
            ///商店关闭
            shopNode.Visible = false;
            world.EndShop();
            turnLabel.Text = $"第{world.turn}回合";
            game.PlaySound(uiSound);
            if (isStow)
            {
                isStow = false;
                float x = shopNode.Position.X + shopNode.Size.X;
                shopNode.Position = new Vector2(x, shopNode.Position.Y);
                shopStow.Text = "收起";
            }
        };

        shopRefresh.Pressed += () =>
        {
            world.TryRefreshShop();
            shopRefreshCostLabel.Text = $"消耗{world.shopRefreshCost}";
            game.PlaySound(uiSound);
        };

        shopStow.Pressed += () =>
        {
            if (isStow)
            {
                isStow = false;
                float x = shopNode.Position.X + shopNode.Size.X;
                shopNode.Position = new Vector2(x, shopNode.Position.Y);
                shopStow.Text = "收起";
            }
            else
            {
                isStow = true;
                float x = shopNode.Position.X - shopNode.Size.X;
                shopNode.Position = new Vector2(x, shopNode.Position.Y);
                shopStow.Text = "展开";
            }
        };

        returnButton.Pressed += SceneManager.Instance.GoBackToMainMenu;

    }


    public override void _Process(double delta)
    {
        base._Process(delta);
        resourceLabel.Text = world.resourcePoint.ToString();
        cumulativeLabel.Text = world.cumulativePoint.ToString();

        InputManager input = InputManager.Instance;

        if(world.selectBuildingData != null)
        {
            buildingItemfollow.Visible = true;
            buildingItemfollow.Texture = world.selectBuildingData.sprite;
            buildingItemfollow.Position = input.GetMousePosition() - buildingItemfollow.Size / 2;
        }
        else
        {
            buildingItemfollow.Visible = false;
        }
    }

    public BuildingPanelItem AddBuildingPanelItem(BuildingData data)
    {
        var item = buildingPanelItemPrefab.Instantiate<BuildingPanelItem>();
        buildingPanelContainer.AddChild(item);
        item.Init(data);
        world.Update();
        return item;
    }

    public BuildingShopItem AddShopItem(BuildingData data)
    {
        var item = shopItemPrefab.Instantiate<BuildingShopItem>();
        shopContainer.AddChild(item);
        item.Init(data);
        world.Update();
        return item;
    }

    public IncrementTaskLabel AddTaskLabel(IncrementTask task)
    {
        var item = taskLabel.Instantiate<IncrementTaskLabel>();
        taskContainer.AddChild(item);
        item.task = task;
        item.Update();
        world.Update();
        return item;
    }

    /// <summary>
    /// 显示提示
    /// </summary>
    /// <param name="text"></param>
    /// <param name="pos"></param>
    /// <returns></returns>
    public SmallText Tips(string text, Vector2 pos = default)
    {
        if(pos == default)
        {
            pos = InputManager.Instance.GetMousePosition();
        }
        return SmallText.CreateSmallText(
            SmallTextType.ValueChangeShow,
            pos,
            text,
            Colors.White,
            parent:tipsNode
        );
    }

    #region 动效

    public Tween OpenShop()
    {
        shopNode.Visible = true;
        shopNode.Modulate = Colors.Transparent;
        Tween tween = shopNode.CreateTween();
        //从透明渐变出现
        tween.TweenMethod(
            Callable.From<float>(v =>
            {
                shopNode.Modulate = new Color(1f, 1f, 1f, v);
            }),
            0f, 1f, 0.2f
        );
        return tween;
    }

    public Tween ShowText(string text)
    {
        float start = -showNode.Size.Y;
        Tween tween = showNode.CreateTween();
        showNode.Position = new Vector2(0, start);
        showLabel.Text = text;
        tween.TweenMethod(
            Callable.From<float>(v =>
            {
                showNode.Position = new Vector2(0, v);
            }),
            start, 0f, 0.2f
        );
        tween.TweenInterval(1);
        tween.TweenMethod(
            Callable.From<float>(v =>
            {
                showNode.Position = new Vector2(0, v);
            }),
            0, start, 0.2f
        );
        return tween;

    }

    #endregion

}

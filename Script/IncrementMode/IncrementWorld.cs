using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using Utils;

public partial class IncrementWorld : Node
{
    [Export] public BuildingData coreData;
    [Export] public BuildPointMap buildPointMap;

    [Export] public IncrementModeUI ui;
    [Export] public BuildingData selectBuildingData = null;
    [Export] public BuildingPanelItem selectBuildingPanelItem;

    [ExportCategory("游戏数据")]
    [Export] public long resourcePoint = 0;
    /// <summary>
    /// 累计点数
    /// </summary>
    [Export] public long cumulativePoint = 0;
    /// <summary>
    /// 回合内累计点数，在回合结束后展示
    /// </summary>
    [Export] public long turnPoint = 0;
    [Export] public int turn = 0;
    [Export] public BuildingPool shopItemPool;
    [Export] public int shopRefreshCost = 0;
    [Export] public int shopRefreshCostBase = 5;
    [Export] public int shopRefreshCostPromote = 5;
    [Export] public int shopRefreshCostPromoteBase = 5;
    [Export] public bool shoping = false;

    [Export] public Array<IncrementTask> tasks;
    [Export] public Array<BuildingPool> rarityPools;

    [Signal] public delegate void OnResourcePointChangedEventHandler(int value);
    [Signal] public delegate void OnSelectBuildingSetEventHandler(Building building);
    [Signal] public delegate void OnTurnEndEventHandler();
    [Signal] public delegate void OnShopEndEventHandler();

    public BuildPoint Map(Vector2I pos) => buildPointMap.Get(pos);
    public BuildPoint Map(int x, int y) => buildPointMap.Get(new Vector2I(x, y));

    Game game => GameManager.Instance.game;
    bool updating = false;
    public string random;

    public void Start()
    {
        random = game.randomSystem.SetRandomGenerator();
        //更新任务
        InitTask();
        //放置核心
        Building.SetBuilding(Map(2, 2), coreData);

        shopRefreshCost = shopRefreshCostBase;
        NextTurn();
    }

    public void TurnEnd()
    {
        Update();
        if(turn >= 0)
        {
            foreach(var point in buildPointMap.allPoints)
            {
                Building building = point.buildingNow;
                if (Game.IsAble(building))
                {
                    building.onTurnEnd?.Run(building, turn);
                }
            }
        }
        Update();
        //展示并清空回合内获得的资源点
        ui.Tips("+" + turnPoint.ToString(), ui.resourceTip.GlobalPosition).SetColor(Colors.Green);
        turnPoint = 0;
        EmitSignal(SignalName.OnTurnEnd);
    }

    public void NextTurn()
    {
        turn += 1;
        Update();
        ui.ShowText("回合开始");
        if(turn >= 0)
        {
            foreach(var point in buildPointMap.allPoints)
            {
                Building building = point.buildingNow;
                if (Game.IsAble(building))
                {
                    building.onTurnStart?.Run(building, turn);
                }
            }
        }
        Update();
    }

    public void ChangeResourcePoint(int value)
    {
        resourcePoint += value;
        if(value > 0)
        {
            cumulativePoint += value;
        }
        else
        {
            ui.Tips("-" + value.ToString(), ui.resourceTip.GlobalPosition).SetColor(Colors.Red);
        }
        turnPoint += value;
        EmitSignal(SignalName.OnResourcePointChanged, value);
        Update();
    }

    public int GetPanelItemCount()
    {
        int count = 0;
        foreach(var item in ui.buildingPanelContainer.GetChildren())
        {
            if (Game.IsAble(item))
            {
                count += 1;
            }
        }
        return count;
    }

    public void AddBuildingItem(BuildingData data)
    {
        if(data == null)
        {
            GD.Print("试图获取空建筑数据");
            return;
        }
        if(GetPanelItemCount() < 8)
        {
            GD.Print($"获取建筑{data}");
            ui.AddBuildingPanelItem(data);
        }
        else
        {
            ui.Tips("建筑栏已满");
            GD.Print("建筑栏已满");
        }
            
    }

    public void Buy(BuildingShopItem shopItem)
    {
        if(resourcePoint >= shopItem.price)
        {
            if(GetPanelItemCount() >= 8)
            {
                //建筑面板已满
                ui.Tips("建筑栏已满");
            }
            else
            {
                ChangeResourcePoint(-shopItem.price);
                AddBuildingItem(shopItem.data);
                shopItem.Sell();
                shopItem.data.onBuy?.Run();
                foreach(var b in GetAllBuilding())
                {
                    b.onSomeBuildingBeBuy?.Run(b, shopItem.data);
                }
            }
        }
        else
        {
            ui.Tips("资源点不足");
            //无钱购买时的行为
        }
    }

    public Array<Building> GetAllBuilding(BuildingTag tag = BuildingTag.Normal)
    {
        Array<Building> result = new();
        foreach(var point in buildPointMap.allPoints)
        {
            if(point.buildingNow != null)
            {
                if(tag != BuildingTag.Normal && point.Check(tag) == null)
                    continue;
                result.Add(point.buildingNow);
            }
        }
        return result;
    }

    /// <summary>
    /// 刷新商店内容的方法
    /// 清空当前商店中的所有物品，并从商店池中随机抽取新的物品进行补充
    /// </summary>
    public void RefreshShop()
    {
        foreach(var item in ui.shopContainer.GetChildren())
        {
            item.QueueFree();
        }
        
        var datas = shopItemPool.Pick(4);
        foreach(var data in datas)
        {
            ui.AddShopItem(data);
        }

        foreach(var b in GetAllBuilding())
        {
            b.onShopRefresh?.Run(b);
        }

        ui.shopRefreshCostLabel.Text = $"消耗{shopRefreshCost}";
        
    }
 
    public bool TryRefreshShop()
    {
        if(resourcePoint >= shopRefreshCost)
        {
            ChangeResourcePoint(-shopRefreshCost);
            //刷新时刷新费用上升百分比（有最低限制）
            shopRefreshCost += shopRefreshCostPromote;
            shopRefreshCostPromote += shopRefreshCostBase;
            RefreshShop();
            return true;
        }
        else
        {
            ui.Tips("资源不足");
            return false;
        }
    }

    public void OpenShop()
    {
        shoping = true;
        RefreshShop();
        ui.info.Close();
        ui.OpenShop();
    }

    public void EndShop()
    {
        shoping = false;
        shopRefreshCost = shopRefreshCostBase;
        shopRefreshCostPromote = shopRefreshCostPromoteBase;
        Update();
        EmitSignal(SignalName.OnShopEnd);
    }

    /// <summary>
    /// 出售建筑
    /// </summary>
    /// <param name="building"></param>
    public void Sell(Building building)
    {
        if(building.id == "core")
        {
            GD.Print("核心无法被出售");
            ui.Tips("核心无法出售");
            return;
        }
        ChangeResourcePoint(building.sellPrice * building.level);
        GD.Print($"出售 {building.name} 获得 {building.sellPrice}");
        building.OnSell();
        foreach(var b in GetAllBuilding())
        {
            b.onSomeBuildingBeSell?.Run(b, building);
        }
    }

    /// <summary>
    /// 出售建筑（未放置）
    /// </summary>
    /// <param name="data"></param>
    public void Sell(BuildingData data)
    {
        int price = data.GetDefaultSellPrice();
        ChangeResourcePoint(price);
        
        GD.Print($"出售 {data.buildingName} 获得 {price}");

        data.onSell?.Run(data);

        foreach(var b in GetAllBuilding())
        {
            b.onSomeBuildingBeSell?.Run(b, data);
        }
    }

    public void TaskUpdate()
    {
        var list = ui.taskContainer.GetChildren();
        foreach(var task in list)
        {
            if(task is IncrementTaskLabel label)
            {
                label.Update();
            }
        }
    }

    public void InitTask()
    {
        foreach(var label in ui.taskContainer.GetChildren())
        {
            label.QueueFree();
        }
        foreach(var task in tasks)
        {
            var label = ui.AddTaskLabel(task);
            label.OnFail += Fail;
            label.OnSuccess += Success;
            label.OnSuccess += label.QueueFree;
        }
    }

    //数据更新
    public void Update()
    {
        if(updating)return;
        updating = true;
        foreach(var point in buildPointMap.allPoints)
        {
            Building building = point.buildingNow;
            if (Game.IsAble(building))
            {
                building.onUpdate?.Run(building);
            }
        }
        TaskUpdate();
        ui.info.Update();
        updating = false;
    }

    /// <summary>
    /// 任务失败导致游戏失败
    /// </summary>
    public void Fail()
    {
        GD.Print("游戏失败");
        ui.end.Visible = true;
        ui.endLabel.Text = "任务失败";
        ui.endScore.Text = $"剩余资源{resourcePoint}\n累计获取资源{cumulativePoint}";
    }

    public void Success()
    {
        foreach(var label in ui.taskContainer.GetChildren())
        {
            if(Game.IsAble(label))
            {
                return;
            }
        }
        //游戏胜利
        GD.Print("游戏胜利");
        ui.end.Visible = true;
        ui.endLabel.Text = "任务完成";
        ui.endScore.Text = $"剩余资源{resourcePoint}\n累计获取资源{cumulativePoint}";
    }
}

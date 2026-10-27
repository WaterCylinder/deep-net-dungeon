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

    #region 交易阻尼参数

    /// <summary>
    /// 同一回合内每多卖一件建筑，卖出返还额外打多少折（0.1 = 每件 -10%）
    /// </summary>
    [Export(PropertyHint.Range, "0,0.5,0.01")] public float sellDecayPerItem = 0.1f;

    /// <summary>
    /// 卖出返还的最低折扣（防止折到0，也保留"清仓回血"的手感）
    /// </summary>
    [Export(PropertyHint.Range, "0.1,1,0.05")] public float sellDecayMinFactor = 0.3f;

    /// <summary>
    /// 刷新成本随"本回合买入额"上涨的系数：买入额每达到该值，刷新成本 +100%
    /// </summary>
    [Export] public int buyVolumeForDoubleRefresh = 1000;

    /// <summary>
    /// 同一建筑类型的"购买/出售联动"每回合最多触发几次
    /// </summary>
    [Export] public int oncePerTurnLinkageLimit = 1;

    /// <summary>
    /// 每次商店显示的商品数量
    /// </summary>
    [Export] public int shopItemCount = 5;

    #endregion

    #region 交易状态

    /// <summary>
    /// 本回合（本次商店）已卖出的建筑数量，用于卖出返还递减
    /// </summary>
    private int _soldThisTurn = 0;

    /// <summary>
    /// 本回合（本次商店）买入花费总额，用于刷新成本上涨
    /// </summary>
    private long _buyVolumeThisTurn = 0;

    /// <summary>
    /// 本次商店已刷新次数
    /// </summary>
    private int _refreshCountThisShop = 0;

    /// <summary>
    /// "每回合最多触发N次"的联动效果使用记录（key = 建筑id + 钩子）
    /// </summary>
    private readonly System.Collections.Generic.Dictionary<string, int> _oncePerTurnUse = new();

    /// <summary>
    /// 是否正在处理买卖产生的效果。
    /// 期间产生的资源变动一律算作交易收入（不计入任务累计），
    /// 这样"出售时给资源"的效果（机械钟、化工储液罐等）不会被当成生产性收入
    /// </summary>
    private bool _inTradeContext = false;

    /// <summary>
    /// 资源点变动来源
    /// </summary>
    public enum IncomeSource
    {
        /// <summary>
        /// 生产性收入（建筑产出、核心产出），计入任务累计
        /// </summary>
        Production,
        /// <summary>
        /// 交易（买卖、刷新），不计入任务累计
        /// </summary>
        Trade,
    }

    #endregion

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

        shopRefreshCost = GetRefreshCost();
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
        //新回合开始：清空上一个回合的买卖统计（卖出折扣、买入额、联动次数）
        ResetTurnTradeState();
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

    /// <summary>
    /// 修改资源点。
    /// 只有生产性收入会计入累计点数（任务进度）和本回合产出；
    /// 买卖/刷新等交易产生的资源不计入任务，避免玩家靠反复买卖刷任务进度
    /// </summary>
    /// <param name="value">变动值</param>
    /// <param name="source">来源（在买卖效果里会自动按交易处理）</param>
    public void ChangeResourcePoint(int value, IncomeSource source = IncomeSource.Production)
    {
        resourcePoint += value;

        //交易上下文里产生的一切资源变动都算交易收入
        if (_inTradeContext)
        {
            source = IncomeSource.Trade;
        }

        if (source == IncomeSource.Production)
        {
            if (value > 0)
            {
                cumulativePoint += value;
                turnPoint += value;
            }
        }
        else if (value < 0)
        {
            ui.Tips("-" + value.ToString(), ui.resourceTip.GlobalPosition).SetColor(Colors.Red);
        }
        else
        {
            //交易产生正收益时也提示一下，让玩家看得见自己在赚钱
            ui.Tips("+" + value.ToString(), ui.resourceTip.GlobalPosition).SetColor(Colors.Green);
        }

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
                ChangeResourcePoint(-shopItem.price, IncomeSource.Trade);
                _buyVolumeThisTurn += shopItem.price;
                AddBuildingItem(shopItem.data);
                shopItem.Sell();

                //购买类联动：同类建筑每回合最多触发一次，避免反复买卖无限叠
                if (TryUseOncePerTurn($"buy:{shopItem.data.id}"))
                {
                    shopItem.data.onBuy?.Run();
                }

                foreach(var b in GetAllBuilding())
                {
                    if (!TryUseOncePerTurn($"beBuy:{b.id}"))
                    {
                        continue;
                    }
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
        
        var datas = shopItemPool.Pick(Mathf.Max(1, shopItemCount));
        foreach(var data in datas)
        {
            ui.AddShopItem(data);
        }

        //商店刷新联动（例如"每次刷新给资源"）每回合最多触发一次，避免无限刷
        foreach(var b in GetAllBuilding())
        {
            if (!TryUseOncePerTurn($"shopRefresh:{b.id}"))
            {
                continue;
            }
            b.onShopRefresh?.Run(b);
        }

        shopRefreshCost = GetRefreshCost();
        ui.shopRefreshCostLabel.Text = $"消耗{shopRefreshCost}";
        
    }
 
    public bool TryRefreshShop()
    {
        int cost = GetRefreshCost();
        if(resourcePoint >= cost)
        {
            ChangeResourcePoint(-cost, IncomeSource.Trade);
            _refreshCountThisShop += 1;
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
        //每次开店重置本次商店的刷新次数与交易统计
        _refreshCountThisShop = 0;
        _soldThisTurn = 0;
        _buyVolumeThisTurn = 0;
        _oncePerTurnUse.Clear();
        RefreshShop();
        ui.info.Close();
        ui.OpenShop();
    }

    public void EndShop()
    {
        shoping = false;
        shopRefreshCost = GetRefreshCost();
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

        int refund = GetSellRefund(building.sellPrice);
        ChangeResourcePoint(refund, IncomeSource.Trade);
        _soldThisTurn += 1;
        GD.Print($"出售 {building.name} 获得 {refund}");

        //出售期间的所有效果都按"交易上下文"处理，产生的资源不计入任务
        _inTradeContext = true;
        try
        {
            //建筑自身的出售效果：同类建筑每回合最多触发一次
            building.OnSell(TryUseOncePerTurn($"sell:{building.id}"));

            foreach(var b in GetAllBuilding())
            {
                if (!TryUseOncePerTurn($"beSell:{b.id}"))
                {
                    continue;
                }
                b.onSomeBuildingBeSell?.Run(b, building);
            }
        }
        finally
        {
            _inTradeContext = false;
        }
    }

    /// <summary>
    /// 出售建筑（未放置）
    /// </summary>
    /// <param name="data"></param>
    public void Sell(BuildingData data)
    {
        //未放置的建筑按"实际买入价的一半"返还（含阶段通胀，与放置后出售一致）
        int basePrice = data.sellPrice >= 0
            ? data.sellPrice
            : Mathf.FloorToInt(GetPrice(data) * 0.5f);
        int price = GetSellRefund(basePrice);
        ChangeResourcePoint(price, IncomeSource.Trade);
        _soldThisTurn += 1;

        GD.Print($"出售 {data.buildingName} 获得 {price}");

        _inTradeContext = true;
        try
        {
            if (TryUseOncePerTurn($"sellData:{data.id}"))
            {
                data.onSell?.Run(data);
            }

            foreach(var b in GetAllBuilding())
            {
                if (!TryUseOncePerTurn($"beSell:{b.id}"))
                {
                    continue;
                }
                b.onSomeBuildingBeSell?.Run(b, data);
            }
        }
        finally
        {
            _inTradeContext = false;
        }
    }

    /// <summary>
    /// 卖出返还：本回合卖得越多折扣越大（最低 sellDecayMinFactor），
    /// 让"反复买卖赚差价"从无摩擦变成有成本，但保留清仓回血的手感
    /// </summary>
    public int GetSellRefund(int sellPrice)
    {
        float factor = Mathf.Max(sellDecayMinFactor, 1f - sellDecayPerItem * _soldThisTurn);
        return Mathf.FloorToInt(sellPrice * factor);
    }

    /// <summary>
    /// 本次商店的刷新成本：
    /// 基础价 ×（1 + 本回合买入额 / buyVolumeForDoubleRefresh）×（1 + 本次商店已刷新次数）。
    /// 买入额越大刷新越贵，靠"买一堆再刷"来滚经济的路子会被自动限速
    /// </summary>
    public int GetRefreshCost()
    {
        float volumeFactor = 1f + (float)_buyVolumeThisTurn / Mathf.Max(1, buyVolumeForDoubleRefresh);
        float countFactor = 1f + _refreshCountThisShop;
        return Mathf.Max(1, Mathf.RoundToInt(shopRefreshCostBase * volumeFactor * countFactor));
    }

    /// <summary>
    /// r1 建筑的后期价格系数：随阶段上涨。
    /// 后期收益膨胀后，便宜建筑相对收入会变成"白菜价"，铺满刷交易也不再花钱，
    /// 这里让 r1 的价格跟着阶段涨（r2/r3 保持原价，它们的回本周期本来就长）
    /// </summary>
    public float GetInflationMultiplier(BuildingData data)
    {
        if (data == null || data.rarity > 1)
        {
            return 1f;
        }
        if (turn < 9)
        {
            return 1f;
        }
        if (turn < 32)
        {
            return 2f;
        }
        return 4f;
    }

    /// <summary>
    /// 商品的实际购买价（含阶段通胀）
    /// </summary>
    public int GetPrice(BuildingData data)
    {
        if (data == null)
        {
            return 0;
        }
        return Mathf.Max(1, Mathf.RoundToInt(data.buyPrice * GetInflationMultiplier(data)));
    }

    /// <summary>
    /// 每回合（每次商店）最多触发 limit 次的联动效果
    /// </summary>
    /// <param name="key">效果标识（通常用"建筑id:钩子"）</param>
    /// <param name="limit">上限，默认用 oncePerTurnLinkageLimit</param>
    /// <returns>还能触发返回true，并记一次使用</returns>
    private bool TryUseOncePerTurn(string key, int limit = -1)
    {
        int max = limit >= 0 ? limit : Mathf.Max(1, oncePerTurnLinkageLimit);
        _oncePerTurnUse.TryGetValue(key, out int used);
        if (used >= max)
        {
            return false;
        }
        _oncePerTurnUse[key] = used + 1;
        return true;
    }

    /// <summary>
    /// 重置回合内的交易状态（每回合开始调用）
    /// </summary>
    private void ResetTurnTradeState()
    {
        _soldThisTurn = 0;
        _buyVolumeThisTurn = 0;
        _refreshCountThisShop = 0;
        _oncePerTurnUse.Clear();
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

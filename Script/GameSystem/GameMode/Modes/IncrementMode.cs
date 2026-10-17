using Godot;
using System;

/// <summary>
/// 增量模式-增量塔防
/// </summary>
public partial class IncrementMode : GameModeBase
{
    public override string ModeName => "Increment";

    public int a = 0;

    /// <summary>
    /// 每格滚轮改变的相机缩放值
    /// </summary>
    public float zoomStep = 0.1f;

    /// <summary>
    /// 触发拖动所需的鼠标位移阈值（屏幕像素），累积位移未超过阈值时不移动相机
    /// </summary>
    public float dragThreshold = 4f;

    /// <summary>
    /// 本次按住左键期间累积的鼠标位移，用于判断是否进入拖动
    /// </summary>
    private Vector2 _dragAccumulate = Vector2.Zero;

    /// <summary>
    /// 是否已进入拖动状态，进入后本次按住期间一直保持
    /// </summary>
    private bool _dragging = false;

    /// <summary>
    /// 上一帧已用于移动相机的鼠标位置，避免像素级的微小抖动累积成相机漂移
    /// </summary>
    private Vector2 _dragLastPos = Vector2.Zero;

    /// <summary>
    /// 是否已记录拖动起始位置
    /// </summary>
    private bool _dragPosValid = false;

    public override void OnStart()
    {
        camera.targetZoom = 0.5f;
        //回合结束时逻辑，每隔1个回合开启一次商店。
        IncrementWorld world = game.incrementWorld;
        world.OnTurnEnd += () =>
        {
            int turn = world.turn;
            if(turn >= 1 && turn % 1 == 0)
            {
                world.shoping = true;
                world.ui.ShowText("回合结束").Finished += world.OpenShop;
            }
            else
            {
                world.NextTurn();
            }
        };
        //商店关闭时进入下一回合
        world.OnShopEnd += world.NextTurn;
    }

    public override void OnProcess(double delta)
    {
        base.OnProcess(delta);
        IncrementWorld world = game.incrementWorld;
        switch (game.A)
        {
            case 0:
            world.Start();
            //添加1级商店
            world.shopItemPool.Extend(world.rarityPools[0]);
            world.OnTurnEnd += () =>
            {
                if(world.turn == 10) game.A = 200;
                if(world.turn == 50) game.A = 1000;
            };
            BuildingData testData = AssetManager.Instance.GetData<BuildingData>(
                "Resource/IncrementMode/Building/Machine/standard_machinery", null
            );
            world.AddBuildingItem(testData);
            world.AddBuildingItem(testData);
            game.Next();
            break;

            case 200:
            //二阶段
            world.shopItemPool.Extend(world.rarityPools[1]);
            world.shopRefreshCostBase = 10;
            world.shopRefreshCostPromoteBase = 10;
            game.Next();
            break;

            case 1000:
            world.shopItemPool.Extend(world.rarityPools[2]);
            world.shopRefreshCostBase = 50;
            world.shopRefreshCostPromoteBase = 50;
            game.Next();
            break;
        }

        HandleCameraDrag();
        HandleCameraZoom();
    }

    public override void OnPhysicsProcess(double delta)
    {
        base.OnPhysicsProcess(delta);
    }

    /// <summary>
    /// 按住鼠标左键拖动相机。
    /// 需累积位移超过dragThreshold才进入拖动，单纯点击不会移动相机。
    /// 鼠标位移是屏幕坐标，除以相机缩放后才是世界坐标位移。
    /// </summary>
    void HandleCameraDrag()
    {
        InputManager input = InputManager.Instance;
        if (input == null || camera == null)
        {
            return;
        }

        //未按住左键，复位拖动状态
        if (!input.IsActionPressed(InputManager.Actions.ActiveSkill))
        {
            _dragging = false;
            _dragAccumulate = Vector2.Zero;
            _dragPosValid = false;
            return;
        }

        Vector2 mousePos = input.GetMousePosition();
        if (!_dragPosValid)
        {
            _dragLastPos = mousePos;
            _dragPosValid = true;
            return;
        }

        Vector2 frameDelta = mousePos - _dragLastPos;

        //尚未进入拖动时累积位移，超过阈值才开始拖动
        if (!_dragging)
        {
            _dragAccumulate += frameDelta;
            if (_dragAccumulate.Length() < dragThreshold)
            {
                return;
            }
            //从当前位置开始拖动，累积位移不再用于移动相机
            _dragging = true;
            _dragLastPos = mousePos;
            return;
        }

        //已进入拖动：用相邻两帧的真实位移移动相机，避免累加后一次性跳变
        _dragLastPos = mousePos;
        if (frameDelta == Vector2.Zero)
        {
            return;
        }

        //屏幕位移取反即为相机位移方向，再按缩放换算到世界坐标
        float zoom = Mathf.Max(camera.Zoom.X, 0.001f);
        camera.SetTargetPosition(camera.targetPos - frameDelta / zoom);
    }

    /// <summary>
    /// 滚轮缩放相机，滚轮上为放大，滚轮下为缩小
    /// </summary>
    void HandleCameraZoom()
    {
        InputManager input = InputManager.Instance;
        if (input == null || camera == null || Mathf.IsZeroApprox(zoomStep))
        {
            return;
        }

        int direction = input.GetZoomDirection();
        if (direction == 0)
        {
            return;
        }

        //相机Zoom越小视野越大：滚轮向上时direction为1，应减小Zoom以放大
        camera.SetTargetZoom(camera.targetZoom + direction * zoomStep);
    }
}

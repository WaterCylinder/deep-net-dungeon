using Godot;
using System;

[GlobalClass]
public partial class SceneBehavior : Behavior
{
    Game game => GameManager.Instance.game;

    public void Test()
    {
        game.room.Log("玩家进行介绍对话结束事件测试");
    }

    public void GenerateStartItem(TriggerDialogue trigger)
    {
        if(trigger == null)return;
        //只触发一次
        if(trigger.AlreadyTrigger)return;
        if(game.room == null)return;
        
        game.room.Log("玩家完成第一段对话，生成初始道具并且打开所有门");
        Vector2 pos = trigger.GlobalPosition;
        pos.Y += 96;
        ItemData data = AssetManager.Instance.GetData<ItemData>("Resource.Demo.Item.item_auto_attack", null);
        Item item = Item.CreateItem(data, pos);
        //捡起初始道具时打开门
        item.OnPickSignal += picker =>
        {
            game.room.OpenAllDoor();
        };
    }
}

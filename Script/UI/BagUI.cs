using Godot;
using System;

public partial class BagUI : Control
{
    Game game => GameManager.Instance.game;
    Player player => game.player;

    [Export] public Texture2D buttonOpen;
    [Export] public Texture2D buttonClose;

    [Export] public TextureButton button;
    [Export] public Control bagUI;
    [Export] public Control itemsNode;
    [Export] public PackedScene layoutItemPrefab;
    [Export] public Control introNode;
    [Export] public TextureRect introImage;
    [Export] public RichTextLabel introLabel;
    [Export] public Label introLevel;
    [Export] public Label introName;
    [Export] public Label introTag;

    public override void _Ready()
    {
        button.Pressed += SwitchBagUI;
        ChangeIntroUI(null);
    }

    public void SwitchBagUI()
    {
        bagUI.Visible = !bagUI.Visible;
        if (bagUI.Visible)
        {
            button.TextureNormal = buttonOpen;
            UpdateItems();
        }
        else
        {
            button.TextureNormal = buttonClose;
        }
    }

    public void UpdateItems()
    {
        if(! Game.IsAble(player))
            return;
        Bag bag = player.GetComponent<Bag>();
        if(bag == null)
            return;

        //清除所有的背包Ui里显示的物体
        foreach(var child in itemsNode.GetChildren())
        {
            child.QueueFree();
        }
        ChangeIntroUI(null);

        // 先不做区分把所有背包物品都显示在菜单里
        foreach (var item in bag.itemDict)
        {
            ItemData data = item.Key;
            Control layoutNode = layoutItemPrefab.Instantiate() as Control;
            itemsNode.AddChild(layoutNode);
            TextureButton button = layoutNode.GetNode<TextureButton>("TextureButton");
            button.TextureNormal = data.sprite;
            string num = "";
            if (item.Value > 1)
            {
                num = item.Value.ToString();
            }
            layoutNode.GetNode<Label>("Label").Text = num;
            button.Pressed += () => ChangeIntroUI(data);
        }
    }

    public void ChangeIntroUI(ItemData data)
    {
        if(data == null)
        {
            introNode.Visible = false;
            return;
        }
        else
        {
            introNode.Visible = true;
        }
        GD.Print($"BAG：显示道具{data.ItemName}");
        introImage.Texture = data.sprite;
        introName.Text = data.showName;
        introLabel.Text = data.intro;
        introLevel.Text = $"Lv.{data.itemLevel}";
        introTag.Text = "None";
    }
}

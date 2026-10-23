using System.Formats.Asn1;
using Godot;
using Godot.Collections;

public partial class BagUI : Control
{
    Game game => GameManager.Instance.game;
    Player player => game.player;
    Bag bag = null;

    [Export] public Texture2D buttonOpen;
    [Export] public Texture2D buttonClose;
    [Export] public TextureButton button;

    [Export] public Control bagUI;
    
    [Export] public BagUIItem itemBullet;
    [Export] public BagUIItem itemUsed;
    [Export] public BagUIItem itemSpecial;

    [Export] public BagUIItemIntro itemIntro;

    [Export] public Control itemNode;
    [Export] public Array<Array<BagUIItem>> items = new();

    [Export] public Button characterButton;
    [Export] public BagUIPlayerInfo playerInfo;
    
    public bool isInit = false;

    public override void _Ready()
    {
        button.Pressed += SwitchBagUI;
        //获取所有普通道具栏
        foreach(var itemLine in itemNode.GetChildren())
        {
            Array<BagUIItem> line = new();
            foreach(var item in itemLine.GetChildren())
            {
                if(item is BagUIItem bagItem)
                {
                    line.Add(bagItem);
                    BagUIItem _item = bagItem;
                    bagItem.button.Pressed += () =>
                    {
                        GD.Print("点击背包物品");
                        itemIntro.Visible = true;
                        playerInfo.Visible = false;
                        itemIntro.Init(_item.data, _item.count);  
                    };
                }
            }
            items.Add(line);
        }
        itemBullet.button.Pressed += () =>
        {
            itemIntro.Visible = true;
            playerInfo.Visible = false;
            itemIntro.Init(itemBullet.data);
        };
        itemUsed.button.Pressed += () =>
        {
            itemIntro.Visible = true;
            playerInfo.Visible = false;
            itemIntro.Init(itemUsed.data);
        };
        itemSpecial.button.Pressed += () =>
        {
            itemIntro.Visible = true;
            playerInfo.Visible = false;
            itemIntro.Init(itemSpecial.data);
        };

        itemIntro.Init(null);

        characterButton.Pressed += () =>
        {
            itemIntro.Visible = false;
            playerInfo.Visible = true;
        };
    }

    public void SwitchBagUI()
    {
        bagUI.Visible = !bagUI.Visible;
        if (bagUI.Visible)
        {
            button.TextureNormal = buttonOpen;
            if (!isInit)
            {
                isInit = true;
                bag = player.GetComponent<Bag>();
                bag.OnUpdate += Update;
                Update();
            }
            Tween tween = bagUI.CreateTween();
            tween.TweenMethod(Callable.From<float>(v =>
                {
                    Vector2 scale = bagUI.Scale;
                    scale.X = v;
                    bagUI.Scale = scale;
                }),
                0f, 1f, 0.1
            );
        }
        else
        {
            button.TextureNormal = buttonClose;
        }
    }

    public void Update()
    {
        if(bag == null)return;
        var dict = bag.specialItemDict;
        if(dict[ItemTag.BulletItem] != null)
            itemBullet.Init(dict[ItemTag.BulletItem]);
        else
            itemBullet.Clear();
        if(dict[ItemTag.UsableItem] != null)
            itemUsed.Init(dict[ItemTag.UsableItem]);
        else
            itemUsed.Clear();
        if(dict[ItemTag.SpecialItem] != null)
            itemSpecial.Init(dict[ItemTag.SpecialItem]);
        else
            itemSpecial.Clear();
        
        Array<ItemData> itemDatas = new();
        Array<int> itemCount = new();

        int index = 0;

        foreach(var item in bag.itemDict)
        {
            itemDatas.Add(item.Key);
            itemCount.Add(item.Value);
        }

        foreach(var line in items)
        {
            foreach(var item in line)
            {
                if(item is BagUIItem bugItem)
                {
                    if(index >= itemDatas.Count)
                    {
                        bugItem.Clear();
                    }
                    else
                    {
                        bugItem.Init(itemDatas[index]);
                        bugItem.count = itemCount[index];
                        index += 1;
                    }
                }
            }
        }
    }



}

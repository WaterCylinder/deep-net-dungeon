using Godot;
using System;

public partial class BagUIItemIntro : Control
{
    [Export] public ItemData data;
    [Export] public TextureRect textureRect;
    [Export] public Label nameLabel;
    [Export] public Label levelLabel;
    [Export] public Label tagLabel;
    [Export] public Label countLabel;
    [Export] public Label introLabel;

    public void Init(ItemData data, int count = 1)
    {


        this.data = data;
        textureRect.Texture = data?.sprite;
        nameLabel.Text = data?.showName;
        introLabel.Text = data?.intro;

        if(data == null)
        {
            tagLabel.Text = "";
            levelLabel.Text = "";
            countLabel.Text = "";
            return;
        }

        foreach(var tag in data.tags)
        {
            if (Item.ItemTagMapping.ContainsKey(tag))
            {
                tagLabel.Text = Item.ItemTagMapping[tag];
            }
        }

        levelLabel.Text = $"level.{data.itemLevel}";
        countLabel.Text = "数量" + count.ToString();
    }
}

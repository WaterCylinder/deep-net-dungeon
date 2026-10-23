using Godot;
using System;

public partial class BagUIItem : Control
{
    [Export] public ItemData data;
    [Export] public int count;
    [Export] public TextureButton button;

    public void Init(ItemData data)
    {
        this.data = data;
        button.TextureNormal = data.sprite;
    }

    public void Clear()
    {
        data = null;
        button.TextureNormal = null;
    }
}

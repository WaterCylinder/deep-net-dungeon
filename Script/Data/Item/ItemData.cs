using Godot;
using Godot.Collections;

[GlobalClass]
public partial class ItemData : Resource
{
    [Export] public string ItemName;
    [Export] public string showName;
    [Export] public string intro;
    [Export] public Array<ItemTag> tags = new();
    [Export] public int itemLevel;

    [Export] public Texture2D sprite;
    [Export] public SpriteFrames spriteFrames;

    /// <summary>
    /// 最大堆叠数量
    /// </summary>
    [Export] public int maxCount = -1;

    [Export] public Behavior onInit;
    [Export] public Behavior onPick;
    [Export] public Behavior onUse;
    [Export] public Behavior onDrop;
    [Export] public Behavior onRemove;
    [Export] public Behavior onUpdate;

}

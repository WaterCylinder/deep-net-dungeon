using Godot;
using Godot.Collections;

namespace DeepNetDungeon.Data
{
    /// <summary>
    /// 道具数据基类（Resource）
    /// </summary>
    [GlobalClass]
    public partial class ItemDataBase : Resource
    {
        [Export] public string itemId = "";
        [Export] public string itemName = "";
        [Export] public string description = "";
        [Export] public Texture2D icon;
        [Export] public Array<ItemTag> tags = new();

        /// <summary>
        /// 应用道具效果
        /// </summary>
        public virtual void ApplyEffect(Node player)
        {
            // 留空：应用效果逻辑
        }
    }
}

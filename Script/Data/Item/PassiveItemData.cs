using Godot;
using Godot.Collections;

namespace DeepNetDungeon.Data
{
    /// <summary>
    /// 被动道具数据（Resource）
    /// </summary>
    [GlobalClass]
    public partial class PassiveItemData : ItemDataBase
    {
        [Export] public Dictionary<string, float> statBoosts = new();
        [Export] public Array<Resource> effects = new();

        public override void ApplyEffect(Node player)
        {
            // 留空：应用被动效果逻辑
        }
    }
}

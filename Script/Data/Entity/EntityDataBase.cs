using Godot;

namespace DeepNetDungeon.Data
{
    /// <summary>
    /// 实体数据基类
    /// </summary>
    [GlobalClass]
    public partial class EntityDataBase : Resource
    {
        [Export] public string entityId = "";
        [Export] public string entityName = "";
        [Export] public string description = "";
        [Export] public float maxHealth = 100f;
        [Export] public float speed = 200f;
        [Export] public Texture2D icon;
    }
}

using Godot;

namespace DeepNetDungeon.GameSystem
{
    /// <summary>
    /// 随机系统 - 处理所有随机数生成
    /// </summary>
    public partial class RandomSystem : Node
    {
        private static RandomSystem _instance;
        public static RandomSystem Instance => _instance;

        private RandomNumberGenerator _rng = new();

        public override void _Ready()
        {
            _instance = this;
            _rng.Randomize();
        }

        /// <summary>
        /// 获取范围内随机整数
        /// </summary>
        public int Range(int min, int max)
        {
            return _rng.Randi() % (max - min + 1) + min;
        }

        /// <summary>
        /// 获取范围内随机浮点数
        /// </summary>
        public float RangeF(float min, float max)
        {
            return _rng.Randf() * (max - min) + min;
        }

        /// <summary>
        /// 加权随机选择
        /// </summary>
        public T WeightedChoice<T>(T[] items, float[] weights)
        {
            // 留空：加权随机选择逻辑
            return items.Length > 0 ? items[0] : default;
        }

        /// <summary>
        /// 概率判定
        /// </summary>
        public bool Chance(float probability)
        {
            return _rng.Randf() < probability;
        }

        /// <summary>
        /// 从列表中随机选择
        /// </summary>
        public T Choice<T>(T[] items)
        {
            if (items.Length == 0) return default;
            int index = (int)(_rng.Randf() * items.Length);
            return items[Mathf.Min(index, items.Length - 1)];
        }

        /// <summary>
        /// 设置随机种子
        /// </summary>
        public void SetSeed(uint seed)
        {
            _rng.Seed = seed;
        }
    }
}

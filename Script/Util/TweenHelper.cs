using Godot;
using System;

namespace DeepNetDungeon.Util
{
    /// <summary>
    /// 补间动画辅助类
    /// </summary>
    public static class TweenHelper
    {
        /// <summary>
        /// 缓动函数类型
        /// </summary>
        public delegate float EaseFunc(float t);

        /// <summary>
        /// 线性缓动
        /// </summary>
        public static float EaseLinear(float t) => t;

        /// <summary>
        /// 二次缓进
        /// </summary>
        public static float EaseInQuad(float t) => t * t;

        /// <summary>
        /// 二次缓出
        /// </summary>
        public static float EaseOutQuad(float t) => t * (2f - t);

        /// <summary>
        /// 二次缓入缓出
        /// </summary>
        public static float EaseInOutQuad(float t) => t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;

        /// <summary>
        /// 三次缓进
        /// </summary>
        public static float EaseInCubic(float t) => t * t * t;

        /// <summary>
        /// 三次缓出
        /// </summary>
        public static float EaseOutCubic(float t)
        {
            float t1 = t - 1f;
            return t1 * t1 * t1 + 1f;
        }

        /// <summary>
        /// 补间变换
        /// </summary>
        public static float Lerp(float start, float end, float t, EaseFunc easeFunc = null)
        {
            if (easeFunc != null)
            {
                t = easeFunc(t);
            }
            return Mathf.Lerp(start, end, t);
        }

        /// <summary>
        /// 向量补间
        /// </summary>
        public static Vector2 LerpVector2(Vector2 start, Vector2 end, float t, EaseFunc easeFunc = null)
        {
            if (easeFunc != null)
            {
                t = easeFunc(t);
            }
            return start.Lerp(end, t);
        }

        /// <summary>
        /// 颜色补间
        /// </summary>
        public static Color LerpColor(Color start, Color end, float t, EaseFunc easeFunc = null)
        {
            if (easeFunc != null)
            {
                t = easeFunc(t);
            }
            return start.Lerp(end, t);
        }
    }
}

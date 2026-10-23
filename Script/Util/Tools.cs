using Godot;
using System;
using System.Linq;

namespace Utils
{
	public class Tools
    {
        /// <summary>
        /// 将文件路径合并，匹配多种分隔符，只支持文件不支持文件夹，最后一个匹配的后缀名
        /// </summary>
        /// <param name="path"></param>
        /// <param name="basePath"></param>
        /// <returns></returns>
        public static string Path(string path, string basePath = "")
        {
            string[] pathList = path.Split(['/','\\','.','-']);
			string result = basePath;
			for(int i = 0; i<pathList.Length; i++)
            {
                result = result.PathJoin(pathList[i]);
            }
			return result;
        }

        /// <summary>
        /// 合并参数集
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public static object[] MergeParams(object[] first, object[] second)
        {
            if (first == null || first.Length == 0)
                return second ?? Array.Empty<object>();
            
            if (second == null || second.Length == 0)
                return first ?? Array.Empty<object>();
            
            var result = new object[first.Length + second.Length];
            Array.Copy(first, 0, result, 0, first.Length);
            Array.Copy(second, 0, result, first.Length, second.Length);
            
            return result;
        }

        /// <summary>
        /// 方向列表按照目标方向的角度距离进行排序
        /// </summary>
        /// <param name="towardList">方向列表</param>
        /// <param name="target">目标方向</param>
        /// <param name="offsetRange">最大差异，角度值，默认180</param>
        /// <returns></returns>
        public static Godot.Collections.Array<Vector2> TowardFit(Godot.Collections.Array<Vector2> towardList, Vector2 target, float offsetRange = 180f)
        {
            if(towardList == null || towardList.Count == 0)
            {
                return new();
            }
            System.Collections.Generic.List<(Vector2, float)> list = new();
            float offsetRed = Mathf.DegToRad(offsetRange);
            float targetAngle = target.Angle();
            foreach(Vector2 item in towardList)
            {
                float diffAngle = Mathf.Abs((targetAngle - item.Angle()) % (Mathf.Pi * 2));
                if(diffAngle < offsetRed)
                    list.Add((item, diffAngle));
            }
            if(list.Count == 0)
            {
                return new();
            }
            list.Sort((a, b) => a.Item2.CompareTo(b.Item2));
            Godot.Collections.Array<Vector2> result = new();
            foreach(var item in list)
                result.Add(item.Item1);
            return result;
        }

        /// <summary>
        /// 获取指定节点下的所有指定类型的子节点（包括嵌套子节点）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static Godot.Collections.Array<T> GetAllChildren<[MustBeVariant] T>(Node parent) where T : Node
        {
            Godot.Collections.Array<T> result = new();
            
            if (parent == null)
                return result;

            // 使用栈进行深度优先遍历
            System.Collections.Generic.Stack<Node> stack = new();
            
            // 先将所有直接子节点入栈
            foreach (Node child in parent.GetChildren())
            {
                stack.Push(child);
            }

            // 深度优先遍历
            while (stack.Count > 0)
            {
                Node current = stack.Pop();
                
                // 检查当前节点是否匹配类型
                if (current is T typedNode)
                {
                    result.Add(typedNode);
                }
                
                // 将当前节点的所有子节点入栈（继续向下搜索）
                foreach (Node child in current.GetChildren())
                {
                    stack.Push(child);
                }
            }

            return result;
        }

        /// <summary>
        /// 洗牌算法随机打乱数组顺序
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <param name="random"></param>
        public static void Shuffle<[MustBeVariant] T>(Godot.Collections.Array<T> array, Random random)
        {
            int n = array.Count;
            while (n > 1)
            {
                n--;
                int k = random.Next(n + 1);
                (array[k], array[n]) = (array[n], array[k]);
            }
        }

        /// <summary>
        /// 输出二维数组
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static string Check2DArray<T>(T[,] array)
        {
            string result = "";
            for(int i = 0; i < array.GetLength(0); i++)
            {
                for(int j=0; j < array.GetLength(1); j++)
                {
                    result += $"{array[i,j]}, ";
                }
                result += "\n";
            }
            return result;
        }

        /// <summary>
        /// 获取控件位置在相机中的世界坐标
        /// </summary>
        /// <param name="control"></param>
        /// <param name="camera"></param>
        /// <returns></returns>
        public static Vector2 ScreenToWorld(Control control, Camera2D camera)
        {
            // 1. Control 本地坐标 -> 屏幕坐标
            //    用控件中心点作为参考，也可以改成其他本地坐标
            Vector2 uiLocalPos = control.Size / 2f;
            Vector2 screenPos = control.GetGlobalTransformWithCanvas() * uiLocalPos;

            // 2. 屏幕坐标 -> 世界坐标
            //    用相机的 canvas transform 反推
            Vector2 worldPos = camera.GetCanvasTransform().AffineInverse() * screenPos;

            return worldPos;
        }

        /// <summary>
        /// 在 area 区域内随机找 num 个点，避开 avoidPositions 附近的区域
        /// 只处理 RectangleShape2D 和 CircleShape2D
        /// </summary>
        public static System.Collections.Generic.List<Vector2> GetSpawnPoints(
            Area2D area,
            System.Collections.Generic.List<Vector2> avoidPositions,
            int num,
            System.Random rng,
            float safeRadius = 320f)
        {
            var result = new System.Collections.Generic.List<Vector2>();
            if (num <= 0) return result;

            // 1. 合并所有碰撞形状的包围盒，同时缓存形状信息
            Rect2 bounds = new Rect2();
            bool first = true;
            var shapes = new System.Collections.Generic.List<(CollisionShape2D Node, Shape2D Shape)>();

            foreach (var child in area.GetChildren())
            {
                if (child is CollisionShape2D cs && cs.Shape != null)
                {
                    shapes.Add((cs, cs.Shape));
                    Vector2 center = cs.GlobalPosition;
                    Rect2 r;

                    if (cs.Shape is RectangleShape2D rect)
                        r = new Rect2(center - rect.Size / 2f, rect.Size);
                    else if (cs.Shape is CircleShape2D circle)
                        r = new Rect2(center - new Vector2(circle.Radius, circle.Radius),
                                    new Vector2(circle.Radius * 2f, circle.Radius * 2f));
                    else
                        continue; // 其他形状跳过

                    if (first) { bounds = r; first = false; }
                    else bounds = bounds.Merge(r);
                }
            }

            if (first) // 没有任何有效形状
            {
                GD.PushWarning("Area2D 没有 RectangleShape2D 或 CircleShape2D");
                return result;
            }

            // 2. 采样候选点，候选越多结果越好但越慢
            int sampleCount = Mathf.Max(num * 20, 80);
            var candidates = new System.Collections.Generic.List<Vector2>();

            int maxTries = sampleCount * 3;
            int tries = 0;
            while (candidates.Count < sampleCount && tries < maxTries)
            {
                tries++;
                var p = new Vector2(
                    (float)(bounds.Position.X + rng.NextDouble() * bounds.Size.X),
                    (float)(bounds.Position.Y + rng.NextDouble() * bounds.Size.Y)
                );

                // 判断点是否落在任意形状内
                foreach (var (node, shape) in shapes)
                {
                    Vector2 local = node.ToLocal(p);
                    bool inside = false;

                    if (shape is RectangleShape2D rect)
                        inside = new Rect2(-rect.Size / 2f, rect.Size).HasPoint(local);
                    else if (shape is CircleShape2D circle)
                        inside = local.Length() <= circle.Radius;

                    if (inside) { candidates.Add(p); break; }
                }
            }

            if (candidates.Count == 0) return result;

            // 3. 拒绝采样：过滤掉离避让点太近的候选
            if (avoidPositions != null && avoidPositions.Count > 0)
            {
                float safeRadiusSq = safeRadius * safeRadius;
                var valid = new System.Collections.Generic.List<Vector2>();

                foreach (var p in candidates)
                {
                    bool tooClose = false;
                    foreach (var avoid in avoidPositions)
                    {
                        if (p.DistanceSquaredTo(avoid) < safeRadiusSq)
                        {
                            tooClose = true;
                            break;
                        }
                    }
                    if (!tooClose) valid.Add(p);
                }

                // 兜底：如果过滤后不够，缩小安全半径再补
                if (valid.Count < num)
                {
                    float relaxedSq = safeRadiusSq * 0.25f;
                    foreach (var p in candidates)
                    {
                        if (valid.Count >= num) break;
                        if (valid.Contains(p)) continue;

                        bool tooClose = false;
                        foreach (var avoid in avoidPositions)
                        {
                            if (p.DistanceSquaredTo(avoid) < relaxedSq) { tooClose = true; break; }
                        }
                        if (!tooClose) valid.Add(p);
                    }
                }

                candidates = valid;
            }

            if (candidates.Count == 0) return result;

            // 4. 随机洗牌后取前 num 个（Fisher-Yates）
            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
            }
            for (int i = 0; i < Mathf.Min(num, candidates.Count); i++)
                result.Add(candidates[i]);

            return result;
        }

        /// <summary>
        /// 按权重抽取物件
        /// </summary>
        /// <param name="weights">权重列表</param>
        /// <param name="n">抽取数量</param>
        /// <param name="rng"></param>
        /// <returns></returns>
        public static System.Collections.Generic.List<T> PickWeighted<T>(
            System.Collections.Generic.Dictionary<T, int> weights,
            int n,
            Random rng)
        {
            var result = new System.Collections.Generic.List<T>();
            if (weights == null || weights.Count == 0 || n <= 0)
                return result;

            // 过滤掉权重 <= 0 的项，并计算总权重
            var items = new System.Collections.Generic.List<T>();
            var cumulative = new System.Collections.Generic.List<int>(); // 累积权重
            int total = 0;

            foreach (var kv in weights)
            {
                if (kv.Value <= 0f) continue;
                total += kv.Value;
                items.Add(kv.Key);
                cumulative.Add(total);
            }

            if (total <= 0f || items.Count == 0)
                return result;

            // 抽取 n 次，每次在 [0, total) 里取随机数，用二分查找定位
            for (int i = 0; i < n; i++)
            {
                float roll = (float)(rng.NextDouble() * total);

                // 二分查找第一个累积权重大于 roll 的项
                int lo = 0, hi = cumulative.Count - 1;
                while (lo < hi)
                {
                    int mid = (lo + hi) / 2;
                    if (cumulative[mid] <= roll) lo = mid + 1;
                    else hi = mid;
                }

                result.Add(items[lo]);
            }

            return result;
        }

        /// <summary>
        /// 计算子弹提前量发射的朝向，以击中以 targetVel 匀速直线运动的目标。
        /// 返回 false 表示无解（打不中）。
        /// </summary>
        /// <param name="pos">子弹发射位置</param>
        /// <param name="bulletSpeed">子弹速率（>0）</param>
        /// <param name="targetPos">目标当前位置</param>
        /// <param name="targetVel">目标速度矢量（方向 * 速率）</param>
        /// <param name="shootDir">输出：子弹发射单位朝向</param>
        /// <param name="timeToHit">输出：命中所需时间</param>
        /// <returns></returns>
        public static bool TryGetInterceptDirection(
            Vector2 pos,            // 子弹发射位置
            float bulletSpeed,      // 子弹速率（>0）
            Vector2 targetPos,      // 目标当前位置
            Vector2 targetVel,      // 目标速度矢量（方向 * 速率）
            out Vector2 shootDir,   // 输出：子弹发射单位朝向
            out float timeToHit)    // 输出：命中所需时间
        {
            shootDir = Vector2.Zero;
            timeToHit = 0f;

            Vector2 D = targetPos - pos;
            float targetSpeedSq = targetVel.LengthSquared();
            float bulletSpeedSq = bulletSpeed * bulletSpeed;

            // 系数
            float a = bulletSpeedSq - targetSpeedSq;
            float b = -2f * D.Dot(targetVel);
            float c = -D.LengthSquared();

            float t;

            if (Mathf.Abs(a) < 1e-6f)
            {
                // 退化为一次方程 b*t + c = 0
                if (Mathf.Abs(b) < 1e-6f)
                    return false;               // 无解（或目标与子弹同速且共线）
                t = -c / b;
                if (t <= 0f) return false;
            }
            else
            {
                float disc = b * b - 4f * a * c;
                if (disc < 0f) return false;    // 无实根

                float sqrtDisc = Mathf.Sqrt(disc);
                float t1 = (-b - sqrtDisc) / (2f * a);
                float t2 = (-b + sqrtDisc) / (2f * a);

                // 取最小正根
                if (t1 > 0f && t2 > 0f) t = Mathf.Min(t1, t2);
                else if (t1 > 0f)       t = t1;
                else if (t2 > 0f)       t = t2;
                else return false;      // 两根都非正
            }

            Vector2 hitPoint = targetPos + targetVel * t;
            Vector2 dir = hitPoint - pos;
            if (dir.LengthSquared() < 1e-12f) return false; // 重合，方向无意义

            shootDir = dir.Normalized();
            timeToHit = t;
            return true;
        }
    }
}

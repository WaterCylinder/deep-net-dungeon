using Godot;
using System;

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
    }
}

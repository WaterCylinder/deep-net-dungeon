/*
* 文件名：Coroutine.cs
* 命名空间：Utils.Coroutine
* 作者：WaterCylinder
*/
using Godot;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 协程
/// </summary>
namespace Utils.Coroutine
{
	public enum CoroutineState
    {
		/// <summary>
        /// 协程初始化
        /// </summary>
        Init,
		/// <summary>
        /// 协程运行中
        /// </summary>
		Processing,
		/// <summary>
        /// 协程等待中
        /// </summary>
		Waiting,
		/// <summary>
        /// 协程结束
        /// </summary>
		End
    }
	/// <summary>
    /// 协程类
    /// </summary>
    public class Coroutine
	{
		public string name;
		public CoroutineState state;
		public double timer;
		public double waitTime;
		private double waitTimeCache;

		public Callable process;

		public Coroutine(Callable process, double time)
		{
			this.process = process;
			this.waitTime = time;
			this.waitTimeCache = time;
			this.timer = 0;
			this.state = CoroutineState.Init;
		}

		public void Start()
        {
			timer = 0;
            waitTime = waitTimeCache;
        }

		public void Stop()
        {
            waitTime = 99999999.0;
			timer = waitTime;
        }

		public void Cancle()
        {
            try
            {
				Stop();
                CoroutineManager.Instance.coroutinePool.Remove(this);
            }
            catch
            {
                //
            }
        }

	}

	public partial class CoroutineManager : Node
    {
        private static CoroutineManager instance;
		public static CoroutineManager Instance
        {
            get
            {
                if(instance == null)
                {
                    try
                    {
						GD.Print("正在初始化协程管理器");
						SceneTree sceneTree = Engine.GetMainLoop() as SceneTree;
                        instance = new CoroutineManager();
						instance.Name = "CoroutineManager";
						instance.ProcessMode = ProcessModeEnum.Always;
						sceneTree.Root.AddChild(instance);
                    }
					catch(Exception e)
                    {
                        GD.PrintErr($"初始化协程管理器失败！{e}");
                    }
                }
				return instance;
            }
        }
		public List<Coroutine> coroutinePool = new();
        /// <summary>
        /// 创建协程
        /// </summary>
        /// <param name="process">协程方法</param>
        /// <param name="coroutineName">协程名称</param>
        /// <param name="time">协程执行间隔</param>
        /// <returns></returns>
		public Coroutine StartCoroutine(Callable process, string coroutineName, double time = -1)
        {
            Coroutine coroutine = new(process, time);
			coroutinePool.Add(coroutine);
			return coroutine;
        }
        /// <summary>
        /// 创建协程
        /// </summary>
        /// <param name="process">协程方法</param>
        /// <param name="time">协程执行间隔</param>
        /// <returns></returns>
		public Coroutine StartCoroutine(Callable process, double time = -1)
        {	
			string name = process.GetHashCode() + Guid.NewGuid().ToString();
            return StartCoroutine(process, name, time);
        }

        public override void _Process(double delta)
        {
            foreach(Coroutine co in coroutinePool)
            {
                if(co.waitTime < 0)
                {
                    co.process.Call();
					co.Cancle();
					continue;
                }
				if(co.timer > 0)
                {
					co.timer -= delta;
                }
                else
                {
                    co.process.Call();
					co.timer = co.waitTime - delta;
                }
            }
        }


    }

}

using System;
using System.Linq;
using Godot;
using Godot.Collections;

/*
参数数据
*/

public partial class Args : Resource
{
    [Export] Dictionary<string, Variant> args = new Dictionary<string, Variant>();

    /// <summary>
    /// 添加参数，不会重复添加
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="_default"></param>
    public void Add<[MustBeVariant] T>(string key, T _default = default)
    {
        if (!args.ContainsKey(key))
        {
            //GD.Print($"DEBUG: 添加参数{key}");
            args.Add(key, Variant.From(_default));
        }
    }

    /// <summary>
    /// 获取参数，不存在的参数会获取到默认值
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <returns></returns>
    public T Get<[MustBeVariant] T>(string key, T _default = default)
    {
        if(args.ContainsKey(key))
        {
            return args[key].As<T>();
        }
        else
        {
            //注意：这里必须存 _default，不能存 default。
            //以前存的是 default（数值类型就是0），导致"第一次返回默认值、之后每次都返回0"——
            //像弹幕发射间隔这种 Get("interval", 1f) 的参数，第二帧就变成0，于是每帧都发射
            if (_default is null)
            {
                args.Add(key, default);
            }
            else
            {
                args.Add(key, Variant.From(_default));
            }
            return _default;
        }
    }

    /// <summary>
    /// 设置参数，不存在则添加
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void Set(string key, Variant value)
    {
        if(args.ContainsKey(key))
        {
            args[key] = value;
        }
        else
        {
            args.Add(key, value);
        }
    }

    public void Init(Dictionary<string, Variant> initialArgs)
    {
        if(initialArgs == null)
        {
            return;
        }
        args = initialArgs.Duplicate(true);
    }

    /// <summary>
    /// 复制出一份独立的参数对象（内部字典深拷贝，字典里的对象引用保持原样）
    ///
    /// 为什么需要：以[Export]方式挂到节点上的Args，在PackedScene里会被存成SubResource，
    /// 而同一个预制体的所有实例默认共享这一个Resource对象。
    /// 如果不复制，同类实体的运行期参数（行为状态机、事件标记等）会互相覆盖。
    /// </summary>
    public Args Copy()
    {
        Args copy = new Args();
        copy.Init(args);
        return copy;
    }

    public bool Check(string key, Type type = null)
    {
        if(type != null)
        {
            if (args.ContainsKey(key))
            {
                return args[key].GetType() == type;
            }else return false;
        }
        return args.ContainsKey(key);
    }

    public void ForEach(Action<string, Variant> action)
    {
        foreach(var arg in args)
        {
            action?.Invoke(arg.Key, arg.Value);
        }
    }
}

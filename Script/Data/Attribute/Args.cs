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
            GD.Print($"DEBUG: 添加参数{key}");
            args.Add(key, Variant.From(_default));
        }
    }

    /// <summary>
    /// 获取参数，不存在的参数会获取到默认值
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <returns></returns>
    public T Get<[MustBeVariant] T>(string key)
    {
        if(args.ContainsKey(key))
        {
            return args[key].As<T>();
        }
        else
        {
            args.Add(key, default);
            return default;
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

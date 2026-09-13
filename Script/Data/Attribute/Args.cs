using System.Linq;
using Godot;
using Godot.Collections;

/*
参数数据
*/

public partial class Args : Resource
{
    [Export] Dictionary<string, Variant> args = new Dictionary<string, Variant>();

    public T Get<[MustBeVariant] T>(string key)
    {
        GD.Print($"Args:获取参数{key}");
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
        args = initialArgs.Duplicate(true);
    }

    public bool Check(string key)
    {
        return args.ContainsKey(key);
    }
}

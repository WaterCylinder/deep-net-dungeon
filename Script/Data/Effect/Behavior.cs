using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;

[GlobalClass] // 标记为全局类，可在整个项目中访问
public partial class Behavior : Resource // Behavior类继承自Resource，是资源类的一部分
{
    public static bool logEnabled = false; // 是否启用日志记录
    // 使用静态字典存储方法信息，键为方法名，值为方法信息
    public static System.Collections.Generic.Dictionary<string, MethodInfo> MethodList = new();
    // 行为名称
    [Export]public string behaviorName = "";
    // 参数
    [Export]public Godot.Collections.Dictionary<string, Variant> args = new();

    void Log(string message)
    {
        if(logEnabled)
            GD.Print($"[{GetType().Name}.{behaviorName}] {message}");
    }
    
    /// <summary>
    /// 执行指定名称的行为方法，并返回结果
    /// </summary>
    /// <typeparam name="T">返回值的类型</typeparam>
    /// <returns>方法执行后的结果，如果执行失败则返回默认值</returns>
    public virtual T Run<T>(string methodName, params object[] args)
    {   
        // 用于存储要执行的方法信息
        MethodInfo method;
        // 首先检查方法是否已缓存
        if (MethodList.ContainsKey(methodName))
        {
            // 从缓存中获取方法
            method = MethodList[methodName];
        }
        else
        {
            // 如果未缓存，则从当前类型中获取方法并添加到缓存
            method = GetType().GetMethod(methodName);
            MethodList.Add(methodName, method);
        }
        // 如果找到方法
        if (method != null)
        {
            try
            {
                // 执行方法并返回结果
                var parameters = method.GetParameters(); //函数签名参数
                if(parameters.Length == 0)
                {
                    Log("空参数执行");
                    return (T)method.Invoke(this, null);
                }
                //参数匹配性检查
                bool isFormat = true;
                if(parameters.Length != args.Length)
                    isFormat = false;
                else
                {
                    for(int i = 0; i<parameters.Length; i++)
                    {
                        ParameterInfo para = parameters[i];
                        object arg = args[i];
                        if(arg == null)
                        {
                            //传入NULL则默认匹配
                            continue;
                        }
                        if(!para.ParameterType.IsAssignableFrom(arg.GetType()))
                        {
                            isFormat = false;
                            break;
                        }
                    }
                }
                if (isFormat)
                {
                    string parameterNames ="";
                    foreach(ParameterInfo para in parameters)
                        parameterNames += para.Name + ", ";
                    Log($"参数 {parameterNames} 传入 {string.Join(",",args)}");
                }
                else
                {
                    Log($"参数数量不匹配，正在截取可用参数");
                    List<object> temp = [];
                    List<object> argsList = [.. args];
                    string parameterNames ="";
                    foreach(ParameterInfo para in parameters)
                    {
                        parameterNames += para.Name + ", ";
                        bool isSet = false;
                        for(int i = 0; i<argsList.Count; i++)
                        {
                            object arg = argsList[i];
                            if (para.ParameterType.IsAssignableFrom(arg.GetType()))
                            {
                                temp.Add(arg);
                                isSet = true;
                                argsList.RemoveAt(i);
                                break;
                            }
                        }
                        if (!isSet)
                        {
                            temp.Add(null);
                        }
                    }
                    args = [.. temp];
                    Log($"参数 {parameterNames} 匹配后传入参数 {string.Join(",",args)}");
                }
                
                return (T)method.Invoke(this, args);
            }
            catch (System.Exception e)
            {
                // 捕获并打印执行异常信息
                GD.PushError($"{GetType().Name}@{method.Name}执行异常{e.InnerException}");
                GD.PushError(e);
                return default;
            }
            
        }
        // 如果未找到方法，打印错误信息并返回默认值
        GD.PushError($"{GetType().Name} 未找到行为方法：" + behaviorName);
        return default;
    }

    public virtual T Run<T>(params object[] args)
    {
        return Run<T>(behaviorName, args);
    }

    /// <summary>
    /// 执行行为逻辑
    /// </summary>
    public void Run(params object[] args)
    {
        Run<object>(args);
    }

    public void Run(string methodName, params object[] args)
    {
        Run<object>(methodName);
    }

    /// <summary>
    /// 获取行为参数
    /// </summary>
    /// <param name="key"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    public Variant Arg(string key, Variant defaultValue = default)
    {
        if (args.ContainsKey(key))
        {
            return args[key];
        }
        else
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// 获取行为参数
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    public T Arg<[MustBeVariant] T>(string key, T defaultValue = default)
    {
        if (args.ContainsKey(key))
        {
            if(args[key] is T value)
            {
                return value;
            }
            return defaultValue;
        }
        else
        {
            return defaultValue;
        }
    }

    #region 全局行为方法
    public string DebugLog()
    {   
        if (!args.ContainsKey("info"))
        {
            GD.PrintErr("DebugLog行为缺少参数info");
            return "";
        }
        string info = args["info"].AsString();
        GD.Print(info);
        return info;
    }
    #endregion
}
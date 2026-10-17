using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;

[GlobalClass] // 标记为全局类，可在整个项目中访问
public partial class Behavior : Resource // Behavior类继承自Resource，是资源类的一部分
{
    public static bool logEnabled = false; // 是否启用日志记录
    // 方法缓存按Behavior子类分区，避免不同子类的同名方法互相串味
    // 结构：Behavior类型 -> （方法名 -> 方法信息）
    public static Dictionary<Type, Dictionary<string, MethodInfo>> MethodList = new();
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
    /// 获取当前类型的方法信息，首次访问时缓存
    /// </summary>
    /// <param name="methodName">方法名</param>
    /// <returns>方法信息，未找到返回null</returns>
    private MethodInfo GetCachedMethod(string methodName)
    {
        Type type = GetType();
        if(!MethodList.TryGetValue(type, out var methods))
        {
            methods = new Dictionary<string, MethodInfo>();
            MethodList[type] = methods;
        }
        if(!methods.TryGetValue(methodName, out MethodInfo method))
        {
            method = type.GetMethod(methodName);
            methods[methodName] = method;
        }
        return method;
    }
    
    /// <summary>
    /// 执行指定名称的行为方法，并返回结果
    /// </summary>
    /// <typeparam name="T">返回值的类型</typeparam>
    /// <returns>方法执行后的结果，如果执行失败则返回默认值</returns>
    public virtual T Run<T>(string methodName, params object[] args)
    {   
        //获取方法信息（按当前对象类型分区缓存）
        MethodInfo method = GetCachedMethod(methodName);
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
                if(parameters == null || args == null || parameters.Length != args.Length)
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
                    List<object> argsList = args == null ? [] : [.. args];
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
    /// 获取行为参数。
    /// 支持值类型（float/int/string/bool等）与资源类型，统一走 Variant 的值转换。
    /// 会让 float/int/string 等值类型参数静默回退到默认值。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    public T Arg<[MustBeVariant] T>(string key, T defaultValue = default)
    {
        if (args == null || !args.TryGetValue(key, out Variant value) || value.VariantType == Variant.Type.Nil)
        {
            return defaultValue;
        }

        try
        {
            return value.As<T>();
        }
        catch (System.Exception e)
        {
            //数据里填错类型时告警并使用默认值，避免一条错误数据打断数据驱动流程
            GD.PushWarning($"[Behavior:{behaviorName}] 参数'{key}'无法转换为{typeof(T).Name}（实际为{value.VariantType}），已使用默认值。{e.Message}");
            return defaultValue;
        }
    }

    /// <summary>
    /// 设置行为参数
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void SetArg<[MustBeVariant] T>(string key, T value)
    {
        if (args == null)
        {
            GD.PushWarning($"[Behavior:{behaviorName}] args 未初始化，SetArg('{key}') 被忽略。");
            return;
        }
        if (string.IsNullOrEmpty(key))
        {
            GD.PushWarning($"[Behavior:{behaviorName}] SetArg 收到空 key，已忽略。");
            return;
        }

        args[key] = Variant.From(value);
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
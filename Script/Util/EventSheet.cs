
using System;
using System.Collections.Generic;
using System.Xml.XPath;


public class EventSheet<R, T> where T : MulticastDelegate 
{
    protected Dictionary<string, T> _events;

    public EventSheet(){
        _events = new();
    }

    /// <summary>
    /// 事件集合添加事件，会覆盖掉重名事件
    /// </summary>
    /// <param name="func"></param>
    /// <param name="name"></param>
    public void Add(T func, string name = null)
    {
        if(name == null)
        {
            name = Guid.NewGuid().ToString();
        }
        if (_events.ContainsKey(name))
        {
            _events[name] = func;
        }
        else
        {
            _events.Add(name, func);
        }
    }

    public List<string> GetEventIDs()
    {
        return [.. _events.Keys];
    }

    public T GetEvent(string id)
    {
        if (!_events.ContainsKey(id))
        {
            return null;
        }
        return _events[id];
    }

    public virtual R Invoke(params object[] args)
    {
        foreach(T func in _events.Values)
        {
            func.DynamicInvoke(args);
        }
        return default;
    }

    //运算符重载
    public static dynamic operator + (EventSheet<R, T> sheet, T func)
    {
        sheet.Add(func);
        return sheet;
    }
}

public class EventSheet : EventSheet<object, Action>
{
    
}

public class BoolEventSheet : EventSheet<bool, Func<bool, bool>>
{
    public bool Invoke(bool value)
    {
        bool result = value;
        foreach (Func<bool, bool> func in _events.Values)
        {
            result = func(result);
        }
        return result;
    }
}
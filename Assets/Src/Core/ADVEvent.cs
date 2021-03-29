using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

public class ADVEvent
{
    public struct EventInfo
    {
        public EventInfo(string eventname, Action<object[]> callback, int priority = 1)
        {
            this.eventname = eventname;
            this.callback = callback;
            this.priority = priority;
        }
        public string eventname;
        public Action<object[]> callback;
        public int priority;
    }


    public static bool debugEvent = false;

    private static Dictionary<string, List<EventInfo>> eventpool = new Dictionary<string, List<EventInfo>>();


    /// <summary>注册事件</summary>
    /// <param name="eventname">事件名称</param>
    /// <param name="callback">事件回调</param>
    /// <param name="priority">事件优先级 (>= 1)</param>
    public static void Register(string eventname, Action<object[]> callback, int priority = 1)
    {
        if (eventpool.ContainsKey(eventname))
        {
            // 检测是否有重复的回调  有的话不处理 提示一下  没有的话继续注册
            if (CheckRepeatCallBack(eventname, callback))
            {
                Dbg.WARNING_MSG(string.Format("[Event Repeat] > Event Has Repeat CallBack! EventName : {0}     RegisterTarger : {1}    FireMethod : {2}", eventname, callback.Target, callback.Method));
                return;
            }
        }
        else
        {
            // 创建新list  注册
            eventpool.Add(eventname, new List<EventInfo>());
            //eventpool[eventname] = new List<EventInfo>();
        }
        if (priority < 1)
        {
            Dbg.WARNING_MSG(string.Format("[Event Priority] > Event Priority Must >= 1   EventName : {0}     RegisterTarger : {1}    FireMethod : {2}    Priority : {3}", eventname, callback.Target, callback.Method, priority));
            priority = 1;
        }
        eventpool[eventname].Add(new EventInfo(eventname, callback, priority));
        if (priority > 1) // 优先级大于1 才进行优先级排序， 否则直接插入队列尾部
        {
            OrderPriority(eventname);
        }

        if (debugEvent)
        {
            Dbg.INFO_MSG(string.Format("[Event Register] > EventName : {0}     RegisterTarger : {1}    FireMethod : {2}", eventname, callback.Target, callback.Method));
        }
    }

    /// <summary>注销所有事件</summary>
    public static void DeregisterAllEvent()
    {
        foreach (var item in eventpool.Values)
        {
            item.Clear();
        }
        if (debugEvent)
        {
            Dbg.INFO_MSG("[Event Deregister] > Deregister All Event!!!");
        }
    }


    /// <summary>注销事件</summary>
    /// <param name="eventname">事件名称</param>
    public static void Deregister(string eventname)
    {
        if (eventpool.ContainsKey(eventname))
        {
            eventpool[eventname].Clear();
            if (debugEvent)
            {
                Dbg.INFO_MSG(string.Format("[Event Deregister] > EventName : {0}", eventname));
            }
        }
    }


    /// <summary>注销事件</summary>
    /// <param name="eventname">事件名称</param>
    /// <param name="callback">事件回调</param>
    public static void Deregister(string eventname, Action<object[]> callback)
    {
        if (eventpool.ContainsKey(eventname))
        {
            int eventCount = eventpool[eventname].Count;
            for (int i = eventCount - 1; i >= 0; i--)
            {
                if (eventpool[eventname][i].callback == callback)
                {
                    if (debugEvent)
                    {
                        Dbg.INFO_MSG(string.Format("[Event Deregister] > EventName : {0}    FireTarger : {1}   FireMethod : {2}", eventname, eventpool[eventname][i].callback.Target, eventpool[eventname][i].callback.Method));
                    }
                    eventpool[eventname].Remove(eventpool[eventname][i]);
                }
            }
        }
    }


    /// <summary>触发</summary>
    /// <param name="eventname">事件名称</param>
    /// <param name="param">事件传递参数</param>
    public static void Fire(string eventname, params object[] param)
    {
        if (debugEvent)
        {
            Dbg.INFO_MSG("Event Fire: " + eventname);
        }
        if (eventpool.ContainsKey(eventname))
        {
            int eventCount = eventpool[eventname].Count;
            for (int i = eventCount - 1; i >= 0; i--)
            {
                // try
                // {
                    eventpool[eventname][i].callback(param);
                // }
                // catch (System.Exception e)
                // {
                //     Dbg.ERROR_MSG(e);
                // }
                // if (debugEvent)
                // {
                //     Dbg.INFO_MSG(string.Format("[Event Fire] > EventName : {0}     FireTarger : {1}    FireMethod : {2}", eventname, eventpool[eventname][i].callback.Target, eventpool[eventname][i].callback.Method));
                // }
            }
        }
    }


    /// <summary>触发</summary>
    /// <param name="eventname">事件名称</param>
    /// <param name="param">事件传递参数</param>
    public static void Fire(string eventname, object param)
    {
        if (debugEvent)
        {
            Dbg.INFO_MSG("Event Fire: " + eventname);
        }
        if (eventpool.ContainsKey(eventname))
        {
            int eventCount = eventpool[eventname].Count;
            for (int i = eventCount - 1; i >= 0; i--)
            {
                try
                {
                    eventpool[eventname][i].callback(new object[] { param });// TODO 内存优化
                }
                catch (System.Exception e)
                {
                    Dbg.ERROR_MSG(e);
                }
                if (debugEvent)
                {
                    Dbg.INFO_MSG(string.Format("[Event Fire] > EventName : {0}     FireTarger : {1}    FireMethod : {2}", eventname, eventpool[eventname][i].callback.Target, eventpool[eventname][i].callback.Method));
                }
            }
        }
    }


    public static void ChangePriority()
    {

    }


    /// <summary>
    /// 获取引用类型的内存地址方法
    /// </summary>
    /// <param name="o"></param>
    /// <returns></returns>
    public static string GetMemory(object o)
    {
        GCHandle h = GCHandle.Alloc(o, GCHandleType.WeakTrackResurrection);

        IntPtr addr = GCHandle.ToIntPtr(h);

        return "0x" + addr.ToString("X");
    }

    // 检测该回调是否重复
    private static bool CheckRepeatCallBack(string eventname, Action<object[]> callback)
    {
        for (int i = 0; i < eventpool[eventname].Count; i++)
        {
            if (eventpool[eventname][i].callback == callback)
            {
                return true;
            }
        }
        return false;
    }

    // 优先级排序
    private static void OrderPriority(string eventname)
    {
        if (eventpool.ContainsKey(eventname)) //TODO 所有的 ContainsKey 替换成 TryGetValue
            eventpool[eventname].OrderBy(p => p.priority);
    }

}

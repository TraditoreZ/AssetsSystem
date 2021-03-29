using ECSTimer;
using System.Collections.Generic;

public class TimerMgr : BehaviourSingleton<TimerMgr>
{
    private List<Timer> timeTimers = new List<Timer>();

    private Stack<Timer> waitDelTimers = new Stack<Timer>();

    private Stack<Timer> delTimers = new Stack<Timer>();

    private uint indexID = 0;
    // Use this for initialization
    void Start()
    {
        InvokeRepeating("Timer", 0, 0.1f);
    }

    // Update is called once per frame
    void Update()
    {
        TickTimer(true);
    }

    private void Timer()
    {
        TickTimer(false);
    }

    private void TickTimer(bool isFrame)
    {
        for (int i = timeTimers.Count - 1; i >= 0; i--)
        {
            if (timeTimers[i].Enable() && timeTimers[i].IsFrame == isFrame)
            {
                timeTimers[i].SetTick();
                if (timeTimers[i].TimeOver())
                {
                    timeTimers[i].CallBack();
                    if (timeTimers[i].IsLoop)
                    {
                        timeTimers[i].Reset();
                    }
                    else
                    {
                        timeTimers[i].Stop();
                    }
                }
            }
        }
        while (waitDelTimers.Count > 0)
        {
            Timer timer = waitDelTimers.Pop();
            delTimers.Push(timer);
            timeTimers.Remove(timer);
        }
    }

    private Timer GetTimerByID(uint timerID)
    {
        for (int i = 0; i < timeTimers.Count; i++)
        {
            if (timeTimers[i].id == timerID)
            {
                return timeTimers[i];
            }
        }
        return null;
    }

    private uint CreateID()
    {
        if (indexID > uint.MaxValue - 10)
        {
            Dbg.WARNING_MSG("TimerMgr IndexID IS Full..");
            indexID = 0;
        }
        return indexID++;
    }

    // TODO 负责管理和分配所有Timer  到点叫醒通知回调

    /// <summary>
    /// 创建一个定时器
    /// </summary>
    /// <param name="time">定时 （帧定时为整数， 时间定时为精度0.1的小数）</param>
    /// <param name="callBack">回调函数</param>
    /// <param name="loop">是否循环执行</param>
    /// <param name="isFrame">是否启用帧定时</param>
    /// <returns></returns>
    public uint CreateTimer(float time, System.Action<uint, object[]> callBack, bool loop, bool isFrame, params object[] objs)
    {
        Timer tempTimer = null;
        if (delTimers.Count > 0)
        {
            tempTimer = delTimers.Pop();
        }
        else
        {
            tempTimer = new Timer();
        }
        tempTimer.SetTimer(time, callBack, loop, isFrame, objs);
        tempTimer.id = CreateID();
        timeTimers.Add(tempTimer);
        return tempTimer.id;
    }

    public void StartTimer(uint timerID)
    {
        var timer = GetTimerByID(timerID);
        if (timer != null)
        {
            timer.Start();
        }
        else
        {
            Dbg.ERROR_MSG("Not Find Timer By ID:" + timerID);
        }
    }

    public void StopTimer(uint timerID)
    {
        var timer = GetTimerByID(timerID);
        if (timer != null)
        {
            timer.Stop();
        }
        else
        {
            Dbg.ERROR_MSG("Not Find Timer By ID:" + timerID);
        }
    }

    public void ResetTimer(uint timerID)
    {
        var timer = GetTimerByID(timerID);
        if (timer != null)
        {
            timer.Reset();
        }
        else
        {
            Dbg.ERROR_MSG("Not Find Timer By ID:" + timerID);
        }
    }

    public bool DestroyTimer(uint timerID)
    {
        Timer timer = GetTimerByID(timerID);
        if (timer != null)
        {
            timer.Destroy();
            waitDelTimers.Push(timer);
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool TimerEnable(uint timerID)
    {
        for (int i = 0; i < timeTimers.Count; i++)
        {
            if (timeTimers[i].id == timerID)
            {
                return timeTimers[i].Enable();
            }
        }
        return false;
    }
}

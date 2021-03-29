using System;

namespace ECSTimer
{

    public class Timer
    {
        private Action<uint, object[]> _callBack;
        private float _time;
        private bool _loop;
        private bool enable;
        private float tickTime;
        private bool _isFrame;
        private object[] _paramsObj;

        public uint id;

        /// <summary>
        /// 设置时间定时器
        /// </summary>
        /// <param name="time">时间 （仅支持小数点后1位）</param>
        /// <param name="callBack">回调</param>
        /// <param name="loop">是否循环</param>
        public void SetTimer(float time, Action<uint, object[]> callBack, bool loop, bool isFrame, object[] obj)
        {
            _time = time;
            _callBack = callBack;
            _loop = loop;
            _isFrame = isFrame;
            _paramsObj = obj;
        }


        public void Start()
        {
            enable = true;
        }

        public void Stop()
        {
            enable = false;
        }

        public void Reset()
        {
            tickTime = 0;
        }

        public void Destroy()
        {
            enable = false;
            _callBack = null;
            tickTime = 0;
        }

        public void SetTick()
        {
            tickTime += 0.1f;
        }

        public bool TimeOver()
        {
            return tickTime >= _time;
        }

        public bool Enable()
        {
            return enable;
        }

        public void CallBack()
        {
            try
            {
                _callBack(id, _paramsObj);
            }
            catch (System.Exception e)
            {
                Dbg.ERROR_MSG("TimerMgr Call Error:" + e);
            }
        }

        public bool IsLoop
        {
            get { return _loop; }
        }

        public bool IsFrame
        {
            get { return _isFrame; }
        }

    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
namespace ECSDebug
{
    public class Profiler
    {

        static Dictionary<string, Stopwatch> _profiles = new Dictionary<string, Stopwatch>();

        public static void profileStart(string name)
        {
            Stopwatch watch = null;
            if (!_profiles.TryGetValue(name, out watch))
            {
                watch = new Stopwatch();
                _profiles.Add(name, watch);
            }
            else
                watch.Reset();
            watch.Start();
        }

        public static double profileEnd(string name)
        {
            if (_profiles.ContainsKey(name))
            {
                _profiles[name].Stop();
                return _profiles[name].Elapsed.TotalMilliseconds;
            }
            return 0;
        }

    }
}
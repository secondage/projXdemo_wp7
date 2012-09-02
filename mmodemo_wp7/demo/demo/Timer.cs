using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Threading;
using System.Diagnostics;
namespace demo
{
    public class HiPerfTimer
    {
        private double startTime, stopTime;

        private Stopwatch stopwatch;

        // 构造函数
        public HiPerfTimer()
        {
            startTime = 0;
            stopTime = 0;
            stopwatch = new Stopwatch();
        }

        // 开始计时器
        public void Start()
        {
            // 来让等待线程工作
            //Thread.Sleep(0);

            stopwatch.Start();
        }

        // 停止计时器
        public void Stop()
        {
            stopwatch.Stop();
        }

        // 返回计时器经过时间(单位：秒)
        public double Duration
        {
            get
            {
                return stopwatch.Elapsed.TotalSeconds;
            }
        }

        public double GetDuration()
        {
            double t;
            if (stopTime == 0)
            {
                startTime = stopwatch.Elapsed.TotalSeconds;
                t = startTime;
            }
            else
            {
                stopTime = stopwatch.Elapsed.TotalSeconds;
                t = stopTime - startTime;
                startTime = stopTime;
            }
            return t;
        }

        public double GetTotalDuration()
        {
            return stopwatch.Elapsed.TotalSeconds;
        }
    }
}

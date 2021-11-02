using ClockifyHelper.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace ClockifyHelper.Services
{
    public class IdleTimeService : IDisposable
    {
        private Win32LastInputInfo lastInputBuffer = new Win32LastInputInfo();

        private const double timerInterval = 10 * 1000;
        private uint? baseLine;
        private uint timeSinceBaseLine;

        private TimeSpan idleTimeThreshold = TimeSpan.FromMinutes(1);

        private bool isIdle;

        private Timer timer;

        public event EventHandler UserIdled;
        public event EventHandler UserReactivated;

        public IdleTimeService(TimeSpan idleTimeThreshold)
        {
            if (idleTimeThreshold == TimeSpan.Zero)
            {
                throw new ArgumentException($"{nameof(idleTimeThreshold)} cannot be zero");
            }

            lastInputBuffer.cbSize = (uint)Win32LastInputInfo.SizeOf;
            this.idleTimeThreshold = idleTimeThreshold;

            timer = new Timer(timerInterval);
            timer.Elapsed += Timer_Elapsed;
            timer.AutoReset = true;
        }

        public void ChangeIdleTimeThreshold(TimeSpan idleTimeThreshold)
        {
            this.idleTimeThreshold = idleTimeThreshold;
        }

        public void Start()
        {
            timer.Start();
        }

        public void Stop()
        {
            timer.Stop();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Win32.GetLastInputInfo(out lastInputBuffer);

            if (lastInputBuffer.dwTime != baseLine)
            {
                baseLine = lastInputBuffer.dwTime;
                timeSinceBaseLine = baseLine.Value;
            }
            else
            {
                timeSinceBaseLine += (uint)timerInterval;
            }

            var idleTime = timeSinceBaseLine - baseLine.Value;
            var idleTimeSpan = TimeSpan.FromMilliseconds(idleTime);

            if (idleTimeSpan >= idleTimeThreshold)
            {
                if (!isIdle)
                {
                    isIdle = true;
                    UserIdled?.Invoke(this, EventArgs.Empty);
                }
            }
            else
            {
                if (isIdle)
                {
                    isIdle = false;
                    UserReactivated?.Invoke(this, EventArgs.Empty);
                }
            }

            Debug.WriteLine($"User been idle for {Math.Round(idleTimeSpan.TotalSeconds, 2)} seconds");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    try
                    {
                        timer.Elapsed -= Timer_Elapsed;
                    }
                    catch { }

                    timer?.Dispose();
                }
                catch { }
            }
        }
    }
}

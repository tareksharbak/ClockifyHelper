using ClockifyHelper.Helpers;
using Microsoft.Win32;
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

        private DateTime idleSince = DateTime.UtcNow;

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

            SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
        }

        private void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.SessionLogon || e.Reason == SessionSwitchReason.SessionUnlock)
            {
                UserReactivated?.Invoke(this, EventArgs.Empty);
            }

            if (e.Reason == SessionSwitchReason.SessionLogoff || e.Reason == SessionSwitchReason.SessionLock)
            {
                UserIdled?.Invoke(this, EventArgs.Empty);
            }
        }

        public void ChangeIdleTimeThreshold(TimeSpan idleTimeThreshold)
        {
            this.idleTimeThreshold = idleTimeThreshold;
        }

        public void Start()
        {
            Win32.GetLastInputInfo(out lastInputBuffer);
            baseLine = lastInputBuffer.dwTime;
            idleSince = DateTime.UtcNow;
            timer.Start();
        }

        public void Stop()
        {
            timer.Stop();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Win32.GetLastInputInfo(out lastInputBuffer);

            var idleTime = DateTime.UtcNow - idleSince;

            if (idleTime >= idleTimeThreshold)
            {
                ViewModel.Instance.Log($"Idle threshold exceeded");

                if (!isIdle)
                {
                    if (lastInputBuffer.dwTime != baseLine)
                    {
                        //user was idle (but wasn't detected) and now reactivated
                        ViewModel.Instance.Log($"User was idle (but not detected) and now reactivated");
                        UserReactivated?.Invoke(this, EventArgs.Empty);
                    }
                    else
                    {
                        ViewModel.Instance.Log($"User has gone idle");
                        isIdle = true;
                        UserIdled?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
            else
            {
                if (isIdle)
                {
                    ViewModel.Instance.Log($"User was idle and now reactivated");

                    isIdle = false;
                    UserReactivated?.Invoke(this, EventArgs.Empty);
                }
            }

            if (lastInputBuffer.dwTime != baseLine)
            {
                baseLine = lastInputBuffer.dwTime;
                idleSince = DateTime.UtcNow;
            }

            idleTime = DateTime.UtcNow - idleSince;
            Debug.WriteLine($"User been idle for {Math.Round(idleTime.TotalSeconds, 2)} seconds");
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

                    try
                    {
                        SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
                    }
                    catch { }

                    timer?.Dispose();
                }
                catch { }
            }
        }
    }
}

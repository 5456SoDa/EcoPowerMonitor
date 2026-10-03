using System;
using System.Collections.Generic;
using EcoPowerMonitor.Models;

namespace EcoPowerMonitor.Services
{
    public interface IHardwareMonitorService : IDisposable
    {
        event Action<PowerReading, List<SensorItem>>? ReadingUpdated;
        bool IsRunning { get; }
        void Start(int intervalMs = 1000);
        void Stop();
        void UpdatePollingRate(int intervalMs);
    }
}

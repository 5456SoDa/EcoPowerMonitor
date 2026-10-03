using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoPowerMonitor.Models;

namespace EcoPowerMonitor.Services
{
    public interface IProcessMonitorService
    {
        event Action<List<ProcessPowerItem>>? TopProcessesUpdated;
        void Start();
        void Stop();
        void UpdateSystemPower(double totalWatts, double cpuWatts, double costPerHour, string currencySymbol);
    }

    public class ProcessMonitorService : IProcessMonitorService, IDisposable
    {
        private readonly Dictionary<int, (TimeSpan CpuTime, DateTime SampleTime)> _previousSamples = new();
        private CancellationTokenSource? _cts;
        private double _currentTotalWatts = 30.0;
        private double _currentCpuWatts = 15.0;
        private double _currentCostPerHour = 0.10;
        private string _currencySymbol = "฿";
        private readonly int _processorCount = Environment.ProcessorCount;

        public event Action<List<ProcessPowerItem>>? TopProcessesUpdated;

        public void UpdateSystemPower(double totalWatts, double cpuWatts, double costPerHour, string currencySymbol)
        {
            _currentTotalWatts = Math.Max(1.0, totalWatts);
            _currentCpuWatts = Math.Max(1.0, cpuWatts);
            _currentCostPerHour = costPerHour;
            _currencySymbol = currencySymbol;
        }

        public void Start()
        {
            if (_cts != null) return;
            _cts = new CancellationTokenSource();
            Task.Run(() => MonitorLoopAsync(_cts.Token));
        }

        public void Stop()
        {
            _cts?.Cancel();
            _cts = null;
        }

        private async Task MonitorLoopAsync(CancellationToken token)
        {
            // Initial warm-up delay
            await Task.Delay(1500, token);

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var items = CollectTopProcesses();
                    if (items.Count > 0)
                    {
                        TopProcessesUpdated?.Invoke(items);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Process monitor sample notice: {ex.Message}");
                }

                try
                {
                    await Task.Delay(2000, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private List<ProcessPowerItem> CollectTopProcesses()
        {
            var results = new List<(int Pid, string Name, double CpuPercent, double MemMb)>();
            var now = DateTime.UtcNow;
            var currentPids = new HashSet<int>();

            Process[] processes = Process.GetProcesses();
            foreach (var p in processes)
            {
                try
                {
                    int pid = p.Id;
                    currentPids.Add(pid);

                    // Skip Idle and System system-level meta-pids
                    if (pid <= 4) continue;

                    TimeSpan cpuTime = p.TotalProcessorTime;
                    long memBytes = p.WorkingSet64;
                    double memMb = memBytes / (1024.0 * 1024.0);

                    if (_previousSamples.TryGetValue(pid, out var prev))
                    {
                        double elapsedSeconds = (now - prev.SampleTime).TotalSeconds;
                        if (elapsedSeconds > 0.5)
                        {
                            double cpuDeltaSec = (cpuTime - prev.CpuTime).TotalSeconds;
                            double cpuPercent = Math.Max(0.0, Math.Min(100.0, (cpuDeltaSec / (elapsedSeconds * _processorCount)) * 100.0));
                            if (cpuPercent > 0.1 || memMb > 50.0)
                            {
                                results.Add((pid, p.ProcessName, cpuPercent, memMb));
                            }
                        }
                    }

                    _previousSamples[pid] = (cpuTime, now);
                }
                catch
                {
                    // Ignore processes with restricted security descriptors (e.g. system services)
                }
                finally
                {
                    p.Dispose();
                }
            }

            // Prune dead processes from cache to prevent memory leaks
            var deadPids = _previousSamples.Keys.Where(k => !currentPids.Contains(k)).ToList();
            foreach (var dead in deadPids)
            {
                _previousSamples.Remove(dead);
            }

            // Sort by CPU consumption descending
            var top = results
                .OrderByDescending(r => r.CpuPercent)
                .ThenByDescending(r => r.MemMb)
                .Take(5)
                .ToList();

            var list = new List<ProcessPowerItem>();
            int rank = 1;
            double totalCpuInTop = Math.Max(1.0, top.Sum(t => t.CpuPercent));

            foreach (var item in top)
            {
                // Dynamic power share based on CPU workload
                double processRatio = item.CpuPercent / 100.0;
                double estimatedWatts = Math.Round(Math.Max(0.5, processRatio * _currentCpuWatts + (item.MemMb / 1024.0) * 0.4), 1);
                double costShare = (_currentCostPerHour * (estimatedWatts / _currentTotalWatts));

                list.Add(new ProcessPowerItem
                {
                    Rank = rank++,
                    ProcessId = item.Pid,
                    ProcessName = item.Name,
                    DisplayName = FormatDisplayName(item.Name),
                    CpuPercentage = item.CpuPercent,
                    MemoryMB = item.MemMb,
                    EstimatedWatts = estimatedWatts,
                    FormattedCostPerHour = $"{costShare:F2} {_currencySymbol}/hr"
                });
            }

            return list;
        }

        private static string FormatDisplayName(string rawName)
        {
            return rawName.ToLowerInvariant() switch
            {
                "chrome" => "Google Chrome",
                "msedge" => "Microsoft Edge",
                "firefox" => "Mozilla Firefox",
                "code" => "Visual Studio Code",
                "devenv" => "Visual Studio",
                "spotify" => "Spotify",
                "discord" => "Discord",
                "steam" => "Steam Client",
                "explorer" => "Windows Explorer",
                "ecopower" => "EcoPower Monitor",
                "ecopower_v4" => "EcoPower Monitor",
                "taskmgr" => "Task Manager",
                _ => char.ToUpper(rawName[0]) + rawName.Substring(1)
            };
        }

        public void Dispose()
        {
            Stop();
        }
    }
}

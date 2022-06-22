using System;
using System.Collections.Generic;
using System.Threading;
using DeveloperRenewal.Extensions;
using Longbow.Tasks;

namespace DeveloperRenewal.Trigger
{
    public class RandomTrigger : ITrigger
    {
        public RandomTrigger(){}

        public RandomTrigger(TimeSpan maxInterval, TimeSpan minInterval)
        {
            MaxInterval = maxInterval;
            MinInterval = minInterval;
            StartTime = DateTimeOffset.Now;
            Timeout = TimeSpan.MaxValue;
        }
        
        private bool _enabled = true;
        /// <summary>
        /// 获得/设置 触发器是否启用
        /// </summary>
        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled != value)
                {
                    _enabled = value;
                    if (!value)
                    {
                        NextRuntime = null;
                        LastResult = TriggerResult.Cancelled;
                    }
                    EnabeldChanged?.Invoke(value);
                }
            }
        }
        
        public TimeSpan MinInterval { get; set; }

        public TimeSpan MaxInterval { get; set; }
        
        public bool Pulse(CancellationToken cancellationToken = default)
        {
            var ret = false;
            var random = new Random();
            var interval = TimeSpan.FromSeconds(random.NextDouble(MinInterval.TotalSeconds, MaxInterval.TotalSeconds));
            if (!cancellationToken.WaitHandle.WaitOne(interval))
            {
                LastRuntime = DateTimeOffset.Now;
                interval = TimeSpan.FromSeconds(random.NextDouble(MinInterval.TotalSeconds, MaxInterval.TotalSeconds));
                NextRuntime = DateTimeOffset.Now.Add(interval);
                ret = true;
            }
            return ret;
        }

        public Dictionary<string, object> SetData() => new Dictionary<string, object>()
        {
            ["Name"] = Name,
            ["LastResult"] = LastResult,
            ["NextRuntime"] = NextRuntime?.ToString() ?? "",
            ["LastRunElapsedTime"] = LastRunElapsedTime.ToString(),
            ["StartTime"] = StartTime?.ToString() ?? "",
            ["LastRuntime"] = LastRuntime?.ToString() ?? "",
            [nameof(MinInterval)] = MinInterval.TotalSeconds.ToString() ?? "",
            [nameof(MaxInterval)] = MaxInterval.TotalSeconds.ToString() ?? ""
        };

        public void LoadData(Dictionary<string, object> datas)
        {
            if (datas["Name"] != null) Name = datas["Name"].ToString() ?? "";
            if (Enum.TryParse<TriggerResult>(datas["LastResult"].ToString(), out var result)) LastResult = result;
            if (TimeSpan.TryParse(datas["LastRunElapsedTime"].ToString(), out var elapsedTime)) LastRunElapsedTime = elapsedTime;
            if (DateTimeOffset.TryParse(datas["NextRuntime"]?.ToString(), out var nextRuntime)) NextRuntime = nextRuntime;
            if (NextRuntime != null && NextRuntime < DateTimeOffset.Now) NextRuntime = null;
            if (DateTimeOffset.TryParse(datas["StartTime"]?.ToString(), out var startTime)) StartTime = startTime;
            if (DateTimeOffset.TryParse(datas["LastRuntime"]?.ToString(), out var lastRuntime)) LastRuntime = lastRuntime;
            if (double.TryParse(datas[nameof(MinInterval)]?.ToString(), out var minInterval)) MinInterval = TimeSpan.FromSeconds(minInterval);
            if (double.TryParse(datas[nameof(MaxInterval)]?.ToString(), out var maxInterval)) MinInterval = TimeSpan.FromSeconds(maxInterval);
        }

        public string Name { get; set; }
        public Action<bool> EnabeldChanged { get; set; }
        public DateTimeOffset? StartTime { get; set; }
        public DateTimeOffset? LastRuntime { get; set; }
        public DateTimeOffset? NextRuntime { get; set; }
        public TimeSpan LastRunElapsedTime { get; set; }
        public TriggerResult LastResult { get; set; }
        public TimeSpan Timeout { get; set; }
        public Action<ITrigger> PulseCallback { get; set; }
    }
}
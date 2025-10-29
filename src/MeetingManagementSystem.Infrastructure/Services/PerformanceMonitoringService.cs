using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace MeetingManagementSystem.Infrastructure.Services
{
    public interface IPerformanceMonitoringService
    {
        IDisposable MeasureOperation(string operationName);
        void RecordMetric(string metricName, double value);
        Dictionary<string, PerformanceMetrics> GetMetrics();
    }

    public class PerformanceMetrics
    {
        public long TotalCalls { get; set; }
        public double AverageDuration { get; set; }
        public double MinDuration { get; set; }
        public double MaxDuration { get; set; }
        public double TotalDuration { get; set; }
    }

    public class PerformanceMonitoringService : IPerformanceMonitoringService
    {
        private readonly ILogger<PerformanceMonitoringService> _logger;
        private readonly Dictionary<string, List<double>> _metrics = new();
        private readonly object _lock = new();

        public PerformanceMonitoringService(ILogger<PerformanceMonitoringService> logger)
        {
            _logger = logger;
        }

        public IDisposable MeasureOperation(string operationName)
        {
            return new OperationTimer(operationName, this, _logger);
        }

        public void RecordMetric(string metricName, double value)
        {
            lock (_lock)
            {
                if (!_metrics.ContainsKey(metricName))
                {
                    _metrics[metricName] = new List<double>();
                }
                _metrics[metricName].Add(value);

                // Log slow operations (> 1 second)
                if (value > 1000)
                {
                    _logger.LogWarning("Slow operation detected: {Operation} took {Duration}ms", metricName, value);
                }
            }
        }

        public Dictionary<string, PerformanceMetrics> GetMetrics()
        {
            lock (_lock)
            {
                var result = new Dictionary<string, PerformanceMetrics>();

                foreach (var kvp in _metrics)
                {
                    var values = kvp.Value;
                    if (values.Count > 0)
                    {
                        result[kvp.Key] = new PerformanceMetrics
                        {
                            TotalCalls = values.Count,
                            AverageDuration = values.Average(),
                            MinDuration = values.Min(),
                            MaxDuration = values.Max(),
                            TotalDuration = values.Sum()
                        };
                    }
                }

                return result;
            }
        }

        private class OperationTimer : IDisposable
        {
            private readonly string _operationName;
            private readonly PerformanceMonitoringService _service;
            private readonly ILogger _logger;
            private readonly Stopwatch _stopwatch;

            public OperationTimer(string operationName, PerformanceMonitoringService service, ILogger logger)
            {
                _operationName = operationName;
                _service = service;
                _logger = logger;
                _stopwatch = Stopwatch.StartNew();
            }

            public void Dispose()
            {
                _stopwatch.Stop();
                var duration = _stopwatch.Elapsed.TotalMilliseconds;
                _service.RecordMetric(_operationName, duration);
                
                _logger.LogDebug("Operation {Operation} completed in {Duration}ms", _operationName, duration);
            }
        }
    }
}

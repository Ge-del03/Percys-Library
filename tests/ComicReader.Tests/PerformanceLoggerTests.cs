using System;
using ComicReader.ContinuousReader;
using Xunit;

namespace ComicReader.Tests
{
    public class PerformanceLoggerTests
    {
        [Fact]
        public void MeasureLogs()
        {
            string? last = null;
            PerformanceLogger.LogAction = s => last = s;
            using (PerformanceLogger.Measure("test-measure")) { }
            Assert.False(string.IsNullOrEmpty(last));
            Assert.Contains("test-measure", last);
        }
    }
}

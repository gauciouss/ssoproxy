using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace gd.Util
{

    /// <summary>
    /// 說明: Profiler 類別用於測量程式碼區塊的執行時間，提供開始與結束時間的紀錄，以及計算執行時間的功能。
    /// </summary> <summary>
    /// 
    /// </summary>
    public class Profiler
    {
        public long StartTime { get; private set; }

        public long EndTime { get; private set; }

        public long ExecutionTime => EndTime - StartTime;

        public Profiler()
        {
            StartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public void Stop()
        {
            EndTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();            
        }

        public string StartLog(string msg)
        {
            return $"[Profiler] {msg} 開始時間: {StartTime} ms";
        }

        public string StopLog(string msg)
        {
            return $"[Profiler] {msg} 結束時間: {EndTime} ms, 執行時間: {ExecutionTime} ms";
        }
    }
}
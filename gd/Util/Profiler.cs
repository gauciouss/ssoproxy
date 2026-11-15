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

        public string TargetClzName { get; set; } = string.Empty;

        public Profiler()
        {
            StartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public Profiler(string targetClzName) : this()
        {
            TargetClzName = targetClzName;            
            StartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }


        public void Stop()
        {
            EndTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();            
        }

        public string StartLog(string msg)
        {
            if(string.IsNullOrEmpty(TargetClzName))
            {
                return $"[Profiler][START] {msg} START_TIME: {StartTime} ms";
            }
            else
            {
                return $"[{TargetClzName}][START]{msg} START_TIME: {StartTime} ms";
            }            
        }

        public string StopLog(string msg)
        {
            Stop();            
            if(string.IsNullOrEmpty(TargetClzName))
            {
                return $"[Profiler][END] {msg} END_TIME: {EndTime} ms, EXEC_TIME: {ExecutionTime} ms";
            }
            else
            {
                return $"[{TargetClzName}][END] {msg} END_TIME: {EndTime} ms, EXEC_TIME: {ExecutionTime} ms";
            }       
        }
    }
}
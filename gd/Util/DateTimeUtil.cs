using System;

namespace gd.Util;

public class DateTimeUtil
{

    /// <summary>
    /// 將日期職轉換為中華民國年格式
    /// </summary>
    /// <param name="dt"></param>
    /// <returns></returns>
    public static string Format2ROCDate(DateTime? dt)
    {        
        if(dt == null)
        {
            dt = new DateTime();
        }
        int year = dt.Value.Year - 1911;
        return $"{year}年/{dt.Value.Month}月/{dt.Value.Day}日";        
    }

    /// <summary>
    /// 依據 format 將日期轉換為指定格式
    /// </summary>
    /// <param name="dt"></param>
    /// <param name="format"></param>
    /// <returns></returns> <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static string FormatDateTime(DateTime? dt, string format = "yyyy-MM-dd HH:mm:ss" )
    {
        if (dt == null)
        {
            dt = new DateTime();
        }
        return $"{dt.Value.ToString(format)}";
    }

}
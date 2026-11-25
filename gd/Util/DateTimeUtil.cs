using System;

namespace gd.Util;

public class DateTimeUtil
{

    /// <summary>
    /// 將日期職轉換為中華民國年格式
    /// </summary>
    /// <param name="dt">指定時間，若為null時則取DateTime.Now</param>
    /// <returns></returns>
    public static string Format2ROCDate(DateTime? dt)
    {        
        if(dt == null)
        {
            dt = DateTime.Now;
        }
        int year = dt.Value.Year - 1911;
        return $"{year}年/{dt.Value.Month}月/{dt.Value.Day}日";        
    }

    /// <summary>
    /// 依據 format 將日期轉換為指定格式
    /// </summary>
    /// <param name="dt">指定時間，若為null時則取DateTime.Now</param>
    /// <param name="format">日期格式，預設為：yyyy-MM-dd HH:mm:ss</param>
    /// <returns></returns> <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static string FormatDateTime(DateTime? dt, string format = "yyyy-MM-dd HH:mm:ss" ) 
    {
        if (dt == null)
        {
            dt = DateTime.Now;
        }
        return $"{dt.Value.ToString(format)}";
    }

}
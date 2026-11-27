using System;
using System.Collections;

namespace gd.Util;


/// <summary>
/// 通用工具類別
/// </summary>
public class Util
{

    /// <summary>
    /// 檢查目標是否為空
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <returns></returns>
    public static bool IsEmpty<T>(List<T> list)
    {
        return list == null || list.Count == 0;
    }

    /// <summary>
    /// 檢查目標是否為空
    /// </summary>
    /// <param name="dict"></param>
    /// <returns></returns> <summary>
    /// 
    /// </summary>
    /// <param name="dict"></param>
    /// <returns></returns>
    public static bool IsEmpty(DictionaryBase dict)
    {
        return dict == null || dict.Count == 0;
    }

    /// <summary>
    /// 檢查目標是否為空
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static bool IsEmpty(string? str)
    {
        return StringUtil.IsNullOrWhiteSpace(str);
    }
}

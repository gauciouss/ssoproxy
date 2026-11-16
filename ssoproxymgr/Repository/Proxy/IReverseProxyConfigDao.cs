namespace ssoproxymgr.Repository.Proxy;

public interface IReverseProxyConfigDao
{
    Task<List<ReverseProxyClusterRecord>> GetClustersAsync(bool includeDisabled = false);
    Task<int> SaveClusterAsync(ReverseProxyClusterRecord cluster);
    Task<List<ReverseProxyRouteRecord>> GetRoutesAsync(bool includeDisabled = false);
    Task<int> SaveRouteAsync(ReverseProxyRouteRecord route);
    Task<List<ReverseProxyDestinationRecord>> GetDestinationsAsync(bool includeDisabled = false);
    Task<int> SaveDestinationAsync(ReverseProxyDestinationRecord destination);
}

/// <summary>
/// 功能說明：反向代理設定資料表的資料模型，對應到資料庫中的表格結構。
/// </summary>
/// <param name="ClusterId"></param>
/// <param name="ClusterName"></param>
/// <param name="IsEnabled"></param>
/// <returns></returns>
public sealed record ReverseProxyClusterRecord(string ClusterId, string? ClusterName, bool IsEnabled);

/// <summary>
/// 功能說明：反向代理路由設定資料表的資料模型，對應到資料庫中的表格結構。
/// </summary>
/// <param name="RouteName"></param>
/// <param name="ClusterId"></param>
/// <param name="Order"></param>
/// <param name="MatchPath"></param>
/// <param name="MatchHosts"></param>
/// <param name="IsEnabled"></param>
/// <returns></returns>
public sealed record ReverseProxyRouteRecord(
    string RouteName,
    string ClusterId,
    int Order,
    string? MatchPath,
    string? MatchHosts,
    bool IsEnabled);

/// <summary>
/// 功能說明：反向代理目標設定資料表的資料模型，對應到資料庫中的表格結構。
/// </summary>
/// <param name="ClusterId"></param>
/// <param name="DestinationName"></param>
/// <param name="Address"></param>
/// <param name="IsEnabled"></param>
/// <returns></returns>
public sealed record ReverseProxyDestinationRecord(
    string ClusterId,
    string DestinationName,
    string Address,
    bool IsEnabled);

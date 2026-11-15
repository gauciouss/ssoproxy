namespace ssoproxy.Repository.Proxy;

public interface IReverseProxyConfigDao
{
    Task<List<ReverseProxyClusterRecord>> GetClustersAsync(bool includeDisabled = false);
    Task<ReverseProxyClusterRecord?> GetClusterAsync(string clusterId, bool includeDisabled = false);
    Task<int> SaveClusterAsync(ReverseProxyClusterRecord cluster);
    Task<int> DeleteClusterAsync(string clusterId);

    Task<List<ReverseProxyRouteRecord>> GetRoutesAsync(bool includeDisabled = false);
    Task<ReverseProxyRouteRecord?> GetRouteAsync(string routeName, bool includeDisabled = false);
    Task<int> SaveRouteAsync(ReverseProxyRouteRecord route);
    Task<int> DeleteRouteAsync(string routeName);

    Task<List<ReverseProxyDestinationRecord>> GetDestinationsAsync(string? clusterId = null, bool includeDisabled = false);
    Task<ReverseProxyDestinationRecord?> GetDestinationAsync(string clusterId, string destinationName, bool includeDisabled = false);
    Task<int> SaveDestinationAsync(ReverseProxyDestinationRecord destination);
    Task<int> DeleteDestinationAsync(string clusterId, string destinationName);
}

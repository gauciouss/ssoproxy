namespace ssoproxy.Models;

public sealed class ReverseProxyConfigPayload
{
    public List<ReverseProxyClusterModel> Clusters { get; set; } = new();
    public List<ReverseProxyRouteModel> Routes { get; set; } = new();
    public List<ReverseProxyDestinationModel> Destinations { get; set; } = new();
}

public sealed class ReverseProxyClusterModel
{
    public string ClusterId { get; set; } = string.Empty;
    public string? ClusterName { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public sealed class ReverseProxyRouteModel
{
    public string RouteName { get; set; } = string.Empty;
    public string ClusterId { get; set; } = string.Empty;
    public int Order { get; set; } = 100;
    public string? MatchPath { get; set; }
    public string? MatchHosts { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public sealed class ReverseProxyDestinationModel
{
    public string ClusterId { get; set; } = string.Empty;
    public string DestinationName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
}

public sealed class ReverseProxyPublishResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string PublishedAt { get; set; } = string.Empty;
}

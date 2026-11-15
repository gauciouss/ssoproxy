using gd.Util;
using Microsoft.AspNetCore.Mvc;
using NLog;
using ssoproxy.Models;
using ssoproxy.Proxy;
using ssoproxy.Repository.Proxy;

namespace ssoproxy.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MgrController : ControllerBase
{
    private readonly IReverseProxyConfigDao _configDao;
    private readonly DynamicProxyConfigProvider _proxyConfigProvider;
    private readonly IConfiguration _configuration;

    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public MgrController(
        IReverseProxyConfigDao configDao,
        DynamicProxyConfigProvider proxyConfigProvider,
        IConfiguration configuration)
    {
        _configDao = configDao;
        _proxyConfigProvider = proxyConfigProvider;
        _configuration = configuration;
    }

    /// <summary>    
    /// 取得目前的反向代理配置，從資料庫中讀取並返回為 payload。
    /// </summary>
    /// <returns></returns>
    [HttpGet("config")]
    public async Task<ActionResult<ReverseProxyConfigPayload>> GetConfig()
    {
        Profiler p = new Profiler("ssoproxy.Controllers.MgrController.GetConfig");
        _logger.Trace(p.StartLog("Loading current reverse proxy settings..."));

        var config = new ReverseProxyConfigPayload();

        config.Clusters = (await _configDao.GetClustersAsync(true))
            .Select(x => new ReverseProxyClusterModel
            {
                ClusterId = x.ClusterId,
                ClusterName = x.ClusterName,
                IsEnabled = x.IsEnabled
            })
            .ToList();

        config.Routes = (await _configDao.GetRoutesAsync(true))
            .Select(x => new ReverseProxyRouteModel
            {
                RouteName = x.RouteName,
                ClusterId = x.ClusterId,
                Order = x.Order,
                MatchPath = x.MatchPath,
                MatchHosts = x.MatchHosts,
                IsEnabled = x.IsEnabled
            })
            .ToList();

        config.Destinations = (await _configDao.GetDestinationsAsync(includeDisabled: true))
            .Select(x => new ReverseProxyDestinationModel
            {
                ClusterId = x.ClusterId,
                DestinationName = x.DestinationName,
                Address = x.Address,
                IsEnabled = x.IsEnabled
            })
            .ToList();
            
        _logger.Info(p.StopLog("Loaded current reverse proxy settings. result: {}"), config);

        return Ok(config);
    }


    /// <summary>    
    /// 儲存提供的反向代理配置到資料庫，並更新記憶體中的配置。
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("publish")]
    public async Task<ActionResult<ReverseProxyPublishResponse>> Publish([FromBody] ReverseProxyConfigPayload request)
    {
        Profiler p =new Profiler("ssoproxy.Controllers.MgrController.Publish");
        _logger.Trace(p.StartLog("Publishing reverse proxy settings..."));
        if (request is null)
        {
            return BadRequest(new ReverseProxyPublishResponse
            {
                Success = false,
                Message = "Request payload is required."
            });
        }

        try
        {
            foreach (var cluster in request.Clusters)
            {
                await _configDao.SaveClusterAsync(new ReverseProxyClusterRecord(
                    cluster.ClusterId,
                    cluster.ClusterName,
                    cluster.IsEnabled));
            }

            foreach (var destination in request.Destinations)
            {
                await _configDao.SaveDestinationAsync(new ReverseProxyDestinationRecord(
                    destination.ClusterId,
                    destination.DestinationName,
                    destination.Address,
                    destination.IsEnabled));
            }

            foreach (var route in request.Routes)
            {
                await _configDao.SaveRouteAsync(new ReverseProxyRouteRecord(
                    route.RouteName,
                    route.ClusterId,
                    route.Order,
                    route.MatchPath,
                    route.MatchHosts,
                    route.IsEnabled));
            }

            var runtimeConfig = await ReverseProxySqlConfigLoader.LoadFromDatabaseAsync(_configuration);
            _proxyConfigProvider.Update(runtimeConfig.Routes, runtimeConfig.Clusters.Values.ToList());

            var _ok = Ok(new ReverseProxyPublishResponse
            {
                Success = true,
                Message = "Reverse proxy settings published successfully.",
                PublishedAt = DateTimeOffset.UtcNow.ToString("O")
            });

            _logger.Info(p.StopLog("Reverse proxy settings published successfully. result: {}"), _ok.Value);

            return _ok;
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ReverseProxyPublishResponse
            {
                Success = false,
                Message = ex.Message
            });
        }
    }
}

using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using NLog;

namespace gd.Extensions;

/// <summary>
/// 提供擴充方法以便於在 .NET Core DI 容器中自動掃描並註冊指定命名空間下的服務類別。
/// </summary>
public static class ServiceCollectionExtensions
{


    private static readonly Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    /// <summary>
    /// 自動掃描並註冊多個指定命名空間下的所有服務
    /// </summary>
    /// <param name="services">DI 容器</param>
    /// <param name="targetNamespaces">要掃描的目標命名空間陣列</param>
    /// <param name="lifetime">DI 生命週期，預設為 Scoped</param>
    public static IServiceCollection AddNamespaceComponents(
        this IServiceCollection services, 
        string[] targetNamespaces, 
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        Logger.Info($"[DI] 開始掃描並註冊命名空間: {string.Join(", ", targetNamespaces)}，生命週期: {lifetime}");
        // 1. 取得入口執行 Assembly，這樣才能掃描主專案的類別
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

        // 2. 篩選出屬於指定命名空間清單中、是 Class、不是抽象類別的型別
        var componentTypes = assembly.GetTypes()
            .Where(t => t.Namespace != null 
                        && targetNamespaces.Contains(t.Namespace) 
                        && t.IsClass 
                        && !t.IsAbstract);

        Logger.Info($"[DI] 找到 {componentTypes.Count()} 個符合條件的服務類別。");
        // 3. 遍歷符合條件的 Class 進行註冊
        foreach (var implementationType in componentTypes)
        {
            // 尋找這個 Class 實作的對應介面 (例如：EmailService 對應 IEmailService)
            var interfaceType = implementationType.GetInterfaces()
                .FirstOrDefault(i => i.Name == $"I{implementationType.Name}");

            if (interfaceType != null)
                {
                // 進行動態註冊
                var descriptor = new ServiceDescriptor(interfaceType, implementationType, lifetime);
                Logger.Info($"[DI] 註冊服務: {interfaceType.FullName} -> {implementationType.FullName}，生命週期: {lifetime}");
                services.Add(descriptor);
            }
        }

        return services;
    }
}

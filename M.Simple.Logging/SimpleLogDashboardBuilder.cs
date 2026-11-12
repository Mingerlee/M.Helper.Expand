#if NET6_0
using Microsoft.AspNetCore.Builder;

namespace M.Simple.Logging;

/// <summary>.NET 6 Dashboard 的统一端点约定，授权策略作用于页面及全部 API。</summary>
public sealed class SimpleLogDashboardBuilder : IEndpointConventionBuilder
{
    private readonly List<IEndpointConventionBuilder> _endpoints = new();

    internal void AddEndpoint(IEndpointConventionBuilder endpoint) => _endpoints.Add(endpoint);

    public void Add(Action<EndpointBuilder> convention)
    {
        foreach (var endpoint in _endpoints) endpoint.Add(convention);
    }
}
#endif

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace M.Simple.Caching.Dashboard;

internal sealed class DashboardState(ILogger<DashboardState> logger, TimeProvider clock)
{
    private readonly object _gate = new();
    private readonly Dictionary<string, Plan> _plans = new();
    private readonly Queue<CacheOperation> _operations = new();

    internal static string Actor(ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.Identity?.Name ?? "(authenticated)";
    internal static string Owner(ClaimsPrincipal user) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        string.Join("\n", user.Claims.Select(c => c.Type + "=" + c.Value).Order(StringComparer.Ordinal)))));

    internal string CreatePlan(ClaimsPrincipal user, string prefix, string[] keys)
    {
        lock (_gate)
        {
            foreach (var id in _plans.Where(p => p.Value.ExpiresAt <= clock.GetUtcNow()).Select(p => p.Key).ToArray()) _plans.Remove(id);
            if (_plans.Count >= 100) throw new InvalidOperationException("Too many pending deletion previews. Retry after two minutes.");
            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            _plans.Add(token, new(Owner(user), prefix, keys, clock.GetUtcNow().AddMinutes(2)));
            return token;
        }
    }

    internal Plan? TakePlan(string token, ClaimsPrincipal user)
    {
        lock (_gate)
        {
            if (!_plans.TryGetValue(token, out var plan) || plan.Owner != Owner(user)) return null;
            _plans.Remove(token);
            return plan.ExpiresAt > clock.GetUtcNow() ? plan : null;
        }
    }

    internal void Audit(ClaimsPrincipal user, string action, string key, string outcome)
    {
        var operation = new CacheOperation(clock.GetUtcNow(), Actor(user), action, key, outcome);
        lock (_gate)
        {
            _operations.Enqueue(operation);
            while (_operations.Count > 100) _operations.Dequeue();
        }
        logger.LogInformation("Cache administration: actor={CacheActor} action={CacheAction} key={CacheKey} outcome={CacheOutcome}",
            operation.Actor, operation.Action, operation.Key, operation.Outcome);
    }

    internal CacheOperation[] Operations() { lock (_gate) return _operations.Reverse().ToArray(); }
    internal sealed record Plan(string Owner, string Prefix, string[] Keys, DateTimeOffset ExpiresAt);
    internal sealed record CacheOperation(DateTimeOffset At, string Actor, string Action, string Key, string Outcome);
}

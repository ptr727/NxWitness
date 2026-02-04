using System.Net.Http.Headers;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace CreateMatrix;

internal static class HttpClientFactory
{
    private static readonly Lazy<HttpClient> s_httpClient = new(CreateHttpClient);
    private static readonly Lazy<ResilienceHandler> s_resilienceHandler = new(
        CreateResilienceHandler
    );

    internal static HttpClient GetHttpClient() => s_httpClient.Value;

    private static ResilienceHandler GetResilienceHandler() => s_resilienceHandler.Value;

    private static ResilienceHandler CreateResilienceHandler() =>
        new(
            new ResiliencePipelineBuilder<HttpResponseMessage>()
                .AddRetry(
                    new Polly.Retry.RetryStrategyOptions<HttpResponseMessage>
                    {
                        MaxRetryAttempts = 3,
                        BackoffType = DelayBackoffType.Exponential,
                        UseJitter = true,
                        Delay = TimeSpan.FromSeconds(1),
                        MaxDelay = TimeSpan.FromSeconds(30),
                        ShouldHandle = args =>
                            ValueTask.FromResult(
                                args.Outcome.Exception != null
                                    || args.Outcome.Result is { IsSuccessStatusCode: false }
                            ),
                    }
                )
                .AddCircuitBreaker(
                    new Polly.CircuitBreaker.CircuitBreakerStrategyOptions<HttpResponseMessage>
                    {
                        FailureRatio = 0.2,
                        MinimumThroughput = 10,
                        SamplingDuration = TimeSpan.FromSeconds(60),
                        BreakDuration = TimeSpan.FromSeconds(30),
                        ShouldHandle = args =>
                            ValueTask.FromResult(
                                args.Outcome.Exception != null
                                    || args.Outcome.Result is { IsSuccessStatusCode: false }
                            ),
                    }
                )
                .AddTimeout(TimeSpan.FromSeconds(30))
                .Build()
        )
        {
            InnerHandler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(15),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            },
        };

    private static HttpClient CreateHttpClient()
    {
        HttpClient httpClient = new(GetResilienceHandler()) { Timeout = TimeSpan.FromSeconds(120) };
        httpClient.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue(AssemblyInfo.AppName, AssemblyInfo.InformationalVersion)
        );
        return httpClient;
    }
}

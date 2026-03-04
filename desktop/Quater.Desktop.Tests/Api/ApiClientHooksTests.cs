using System;
using System.Threading;
using System.Threading.Tasks;
using RestSharp;
using Quater.Desktop.Api.Client;

namespace Quater.Desktop.Tests.Api;

/// <summary>
/// Tests for ApiClientHooks deadlock prevention and behaviour.
/// </summary>
public sealed class ApiClientHooksTests : IDisposable
{
    private readonly Func<CancellationToken, Task<string?>>? _originalAccessTokenProvider;
    private readonly Func<Guid?>? _originalLabIdProvider;

    public ApiClientHooksTests()
    {
        _originalAccessTokenProvider = ApiClient.AccessTokenProvider;
        _originalLabIdProvider = ApiClient.LabIdProvider;
    }

    /// <summary>
    /// A <see cref="SynchronizationContext"/> that queues continuations but never drains them
    /// while the calling thread is blocked. This simulates the Avalonia/WPF UI thread context
    /// where a direct .GetAwaiter().GetResult() on an awaitable that needs to resume on the
    /// UI thread would deadlock.
    /// </summary>
    private sealed class DeadlockingSynchronizationContext : SynchronizationContext
    {
        private readonly List<(SendOrPostCallback callback, object? state)> _queue = [];

        public override void Post(SendOrPostCallback d, object? state)
        {
            // Queue rather than execute — simulates a dispatcher that cannot run
            // while the thread is blocked in .GetResult().
            lock (_queue)
            {
                _queue.Add((d, state));
            }
        }

        public override void Send(SendOrPostCallback d, object? state)
        {
            // Synchronous send on a blocked UI thread — deadlock.
            throw new InvalidOperationException("Send on a blocked UI SynchronizationContext — deadlock!");
        }

        /// <summary>Drain all queued continuations.</summary>
        public void Drain()
        {
            List<(SendOrPostCallback callback, object? state)> items;
            lock (_queue)
            {
                items = [.. _queue];
                _queue.Clear();
            }

            foreach (var (cb, s) in items)
                cb(s);
        }
    }

    /// <summary>
    /// Verifies that when AccessTokenProvider is an async method that yields (simulating a
    /// cache miss that triggers async refresh), wrapping it in Task.Run completes successfully
    /// even when called from a thread that has a SynchronizationContext which cannot run
    /// continuations while the thread is blocked.
    ///
    /// A direct tokenProvider(ct).GetAwaiter().GetResult() on such a thread would deadlock
    /// because the async state machine captures the SynchronizationContext and tries to post
    /// its continuation back to it — but the context's Post queue is never drained while
    /// GetResult() is blocking.
    ///
    /// Task.Run moves the async work to the thread pool where there is NO
    /// SynchronizationContext, so continuations run freely, and GetResult() on the outer
    /// Task completes without needing the UI thread.
    /// </summary>
    [Fact]
    public async Task TaskRun_WhenProviderYieldsAndContextIsBlocking_CompletesWithoutDeadlock()
    {
        // Arrange
        const string expectedToken = "test-token-abc123";

        async Task<string?> AsyncTokenProvider(CancellationToken ct)
        {
            // Yield to simulate async work (e.g., refreshing the token cache).
            // Without Task.Run, this continuation would be posted back to the
            // SynchronizationContext set below — causing a deadlock.
            await Task.Yield();
            return expectedToken;
        }

        ApiClient.AccessTokenProvider = AsyncTokenProvider;

        // Act — run on a dedicated thread with a blocking SynchronizationContext to
        // simulate the Avalonia UI thread (5 second timeout: if Task.Run wasn't used
        // this would deadlock and the test would hang until the timeout fires).
        string? token = null;
        var uiContext = new DeadlockingSynchronizationContext();
        var exception = (Exception?)null;

        await Task.Run(() =>
        {
            var thread = new Thread(() =>
            {
                SynchronizationContext.SetSynchronizationContext(uiContext);
                try
                {
                    var tokenProvider = ApiClient.AccessTokenProvider;
                    Assert.NotNull(tokenProvider);

                    // This is the exact pattern used in InterceptRequest after the fix.
                    // Without Task.Run, await Task.Yield() would post its continuation
                    // back to uiContext, which never drains, causing a deadlock.
#pragma warning disable xUnit1031 // deliberate: testing synchronous blocking pattern
                    token = Task.Run(() => tokenProvider(CancellationToken.None)).GetAwaiter().GetResult();
#pragma warning restore xUnit1031
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            });
            thread.IsBackground = true;
            thread.Start();
            thread.Join(millisecondsTimeout: 5000);
            Assert.True(thread.ThreadState == ThreadState.Stopped, "Thread did not complete within 5 seconds — possible deadlock");
        });

        // Assert
        Assert.Null(exception);
        Assert.Equal(expectedToken, token);
    }

    /// <summary>
    /// Verifies that when AccessTokenProvider returns a non-empty token, calling the provider
    /// via Task.Run returns the expected token value.
    /// </summary>
    [Fact]
    public void TaskRun_WhenProviderReturnsToken_ReturnsCorrectToken()
    {
        // Arrange
        const string expectedToken = "bearer-token-xyz";

        async Task<string?> Provider(CancellationToken ct)
        {
            await Task.Delay(1, ct); // minimal async work
            return expectedToken;
        }

        ApiClient.AccessTokenProvider = Provider;

        // Act — the Task.Run pattern used in InterceptRequest
        var tokenProvider = ApiClient.AccessTokenProvider;
        Assert.NotNull(tokenProvider);
#pragma warning disable xUnit1031 // deliberate: testing synchronous blocking pattern
        var token = Task.Run(() => tokenProvider(CancellationToken.None)).GetAwaiter().GetResult();
#pragma warning restore xUnit1031

        // Assert
        Assert.Equal(expectedToken, token);
    }

    /// <summary>
    /// Verifies that when AccessTokenProvider returns null, the Task.Run pattern still
    /// completes without throwing and returns null.
    /// </summary>
    [Fact]
    public void TaskRun_WhenProviderReturnsNull_ReturnsNull()
    {
        // Arrange
        ApiClient.AccessTokenProvider = _ => Task.FromResult<string?>(null);

        // Act
        var tokenProvider = ApiClient.AccessTokenProvider;
        Assert.NotNull(tokenProvider);
#pragma warning disable xUnit1031 // deliberate: testing synchronous blocking pattern
        var token = Task.Run(() => tokenProvider(CancellationToken.None)).GetAwaiter().GetResult();
#pragma warning restore xUnit1031

        // Assert
        Assert.Null(token);
    }
    [Fact]
    public void ApplyRequestHeaders_WhenTokenProviderCancels_DoesNotThrow()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        ApiClient.AccessTokenProvider = _ => Task.FromCanceled<string?>(cts.Token);
        var request = new RestRequest();

        var exception = Record.Exception(() => ApiClient.ApplyRequestHeaders(request));

        Assert.Null(exception);
    }

    public void Dispose()
    {
        ApiClient.AccessTokenProvider = _originalAccessTokenProvider;
        ApiClient.LabIdProvider = _originalLabIdProvider;
    }
}

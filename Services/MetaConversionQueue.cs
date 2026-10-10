using System.Threading.Channels;

namespace EdgeTech.API.Services;

public interface IMetaConversionQueue
{
    void Enqueue(Func<CancellationToken, Task> workItem);
    Task CompleteAsync();
}

public class MetaConversionQueue : IMetaConversionQueue, IAsyncDisposable
{
    private readonly Channel<Func<CancellationToken, Task>> _queue;
    private readonly Task _worker;
    private readonly CancellationTokenSource _cts = new();

    public MetaConversionQueue()
    {
        _queue = Channel.CreateUnbounded<Func<CancellationToken, Task>>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });

        _worker = Task.Run(ProcessQueueAsync);
    }

    public void Enqueue(Func<CancellationToken, Task> workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);
        _queue.Writer.TryWrite(workItem);
    }

    public async Task CompleteAsync()
    {
        _queue.Writer.TryComplete();
        await _worker;
    }

    private async Task ProcessQueueAsync()
    {
        await foreach (var workItem in _queue.Reader.ReadAllAsync(_cts.Token))
        {
            try
            {
                await workItem(_cts.Token);
            }
            catch
            {
                // Queue is intentionally fire-and-forget; failures are logged by the worker itself.
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        await CompleteAsync();
        _cts.Dispose();
    }
}

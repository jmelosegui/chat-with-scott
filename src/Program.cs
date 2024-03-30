using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;

AsyncLocal<int> myValue = new();
List<MyTask> tasks = new();
for (int i = 0; i < 100; i++)
{
    myValue.Value = i;
    tasks.Add(MyTask.Run(delegate
    {
        Console.WriteLine(myValue.Value);
        Thread.Sleep(1000);
    }));
}
foreach (var t in tasks) t.Wait();

class MyTask
{
    private bool _completed;
    private Exception? _exception;
    private Action? _continuation;
    private ExecutionContext? _context;

    public bool IsCompleted
    {
        get
        {
            // No recommended because of:
            // https://youtu.be/R-z2Hv-7nxk?t=2075
            lock (this)
            {
                return _completed;
            }
        }
    }

    public void SetResult() => Complete(null);

    public void SetException(Exception exception) => Complete(exception);

    private void Complete(Exception? exception)
    {
        lock (this)
        {
            if (_completed) throw new InvalidOperationException("Stop messing up my code");

            _completed = true;
            _exception = exception;

            if (_continuation is not null)
            {
                MyThreadPool.QueueUserWorkItem(delegate
                {
                    if (_context is null)
                    {
                        _continuation();
                    }
                    else
                    {
                        ExecutionContext.Run(_context, (object? state) => ((Action)state!).Invoke(), _continuation);
                    }
                });
            }
        }
    }

    public void Wait()
    {
        ManualResetEventSlim? mres = null;

        // No recommended because of:
        // https://youtu.be/R-z2Hv-7nxk?t=2075
        lock (this)
        {
            if (!_completed)
            {
                mres = new ManualResetEventSlim();
                ContinueWith(mres.Set);
            }
        }

        mres?.Wait();

        if (_exception is not null)
        {
            ExceptionDispatchInfo.Throw(_exception);
        }
    }

    public void ContinueWith(Action action)
    {
        // No recommended because of:
        // https://youtu.be/R-z2Hv-7nxk?t=2075
        lock (this)
        {
            if (_completed)
            {
                MyThreadPool.QueueUserWorkItem(action);
            }
            else
            {
                _continuation = action;
                _context = ExecutionContext.Capture();
            }
        }
    }

    public static MyTask Run(Action action)
    {
        MyTask task = new();

        MyThreadPool.QueueUserWorkItem(() =>
        {
            try
            {
                action();
                task.SetResult();
            }
            catch (Exception ex)
            {
                task.SetException(ex);
            }
        });

        return task;
    }
}

static class MyThreadPool
{
    private static readonly BlockingCollection<(Action, ExecutionContext?)> s_workItems = new();

    public static void QueueUserWorkItem(Action action) => s_workItems.Add((action, ExecutionContext.Capture()));

    static MyThreadPool()
    {
        for (int i = 0; i < Environment.ProcessorCount; i++)
        {
            new Thread(() =>
            {
                while (true)
                {
                    (Action workItem, ExecutionContext? context) = s_workItems.Take();
                    if (context is null)
                    {
                        workItem();
                    }
                    else
                    {
                        ExecutionContext.Run(context, (object? state) => ((Action)state!).Invoke(), workItem);
                    }
                }
            })
            { IsBackground = true }.Start();
        }
    }
}

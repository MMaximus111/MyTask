using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace Training;

[AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
public class MyTask
{
    private bool _completed;
    private Exception? _exception;
    private Action? _continuation;
    private ExecutionContext? _context;
    
    public Awaiter GetAwaiter() => new(this);
    
    public bool IsCompleted
    {
        get
        {
            lock (this)
            {
                return _completed;
            }
        }
    }
    
    public static MyTask Run(Action action)
    {
        MyTask t = new();

        MyThreadPool.QueueUserWorkItem(() =>
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                t.SetException(e);
                return;
            }
            t.SetResult();
        });

        return t;
    }
    
    public void Wait()
    {
        ManualResetEventSlim? manualResetEventSlim = null;

        lock (this)
        {
            if (!_completed)
            {
                manualResetEventSlim = new ManualResetEventSlim();
                ContinueWith(manualResetEventSlim.Set);
            }
        }

        manualResetEventSlim?.Wait();

        if (_exception is not null)
        {
            ExceptionDispatchInfo.Throw(_exception);
        }
    }
    
    public void SetException(Exception exception)
    {
        Complete(exception);
    }
    
    public void SetResult() => Complete(null);
    
    public void Complete(Exception? exception)
    {
        lock (this)
        {
            if (_completed)
            {
                throw new Exception("Task already completed");
            }

            _completed = true;
            _exception = exception;

            if (_continuation is not null)
            {
                MyThreadPool.QueueUserWorkItem(() =>
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

    public MyTask ContinueWith(Action action)
    {
        MyTask continuationTask = new();

        Action callback = () =>
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                continuationTask.SetException(e);
                return;
            }

            continuationTask.SetResult();
        };

        lock (this)
        {
            if (_completed)
            {
                MyThreadPool.QueueUserWorkItem(callback);
            }
            else
            {
                _continuation = callback;
                _context = ExecutionContext.Capture();
            }
        }

        return continuationTask;
    }
}
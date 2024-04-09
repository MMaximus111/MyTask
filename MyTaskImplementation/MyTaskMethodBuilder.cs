using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Training;

public class MyTaskMethodBuilder
{
    internal readonly struct VoidTaskResult
    {
    }

    Exception? _exception;
    bool _hasResult;
    SpinLock _lock;
    MyTaskCompletionSource<VoidTaskResult>? _source;

    public MyTask Task
    {
        get
        {
            bool lockTaken = false;

            try
            {
                _lock.Enter(ref lockTaken);

                if (_exception is not null)
                {
                    _source!.Task.SetException(_exception);

                    return  _source!.Task;
                }

                if (_hasResult)
                {
                    _source!.Task.Complete(null);

                    return _source.Task;
                }

                _source ??= new MyTaskCompletionSource<VoidTaskResult>(TaskCreationOptions.RunContinuationsAsynchronously);

                return _source.Task;
            }
            finally
            {
                if (lockTaken)
                    _lock.Exit();
            }
        }
    }

    public void AwaitOnCompleted<TAwaiter, TStateMachine>(
        ref TAwaiter awaiter,
        ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine =>
        awaiter.OnCompleted(stateMachine.MoveNext);

    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
        ref TAwaiter awaiter,
        ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine =>
        awaiter.UnsafeOnCompleted(stateMachine.MoveNext);


    public static MyTaskMethodBuilder Create() => new()
    {
        _lock = new SpinLock(Debugger.IsAttached)
    };

    public void SetException(Exception exception)
    {
        var lockTaken = false;
        try
        {
            _lock.Enter(ref lockTaken);
            if (Volatile.Read(ref _source) is { } source)
            {
                source.TrySetException(exception);
            }
            else
            {
                _exception = exception;
            }
        }
        finally
        {
            if (lockTaken)
                _lock.Exit();
        }
    }

    public void SetResult()
    {
        bool lockTaken = false;

        try
        {
            _lock.Enter(ref lockTaken);

            _source!.TrySetResult(new VoidTaskResult());

            _hasResult = true;
        }
        finally
        {
            if (lockTaken)
            {
                _lock.Exit();
            }
        }
    }

    public void SetStateMachine(IAsyncStateMachine stateMachine)
    {
    }

    public void Start<TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : IAsyncStateMachine => stateMachine.MoveNext();
}
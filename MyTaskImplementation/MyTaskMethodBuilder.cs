using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Training;

public class MyTaskMethodBuilder
{
    private Exception? _exception;
    private bool _hasResult;
    private SpinLock _lock;
    private MyTask? _myTask;

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
                    _myTask!.SetException(_exception);

                    return  _myTask;
                }

                if (_hasResult)
                {
                    _myTask!.Complete(null);

                    return _myTask;
                }

                _myTask ??= new MyTask();

                return _myTask;
            }
            finally
            {
                if (lockTaken)
                {
                    _lock.Exit();
                }
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
        bool lockTaken = false;

        try
        {
            _lock.Enter(ref lockTaken);

            if (Volatile.Read(ref _myTask) is { } myTask)
            {
                myTask.SetException(exception);
            }
            else
            {
                _exception = exception;
            }
        }
        finally
        {
            if (lockTaken)
            {
                _lock.Exit();
            }
        }
    }

    public void SetResult()
    {
        bool lockTaken = false;

        try
        {
            _lock.Enter(ref lockTaken);

            _myTask!.SetResult();

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
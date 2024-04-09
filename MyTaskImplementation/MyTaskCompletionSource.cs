namespace Training;

public class MyTaskCompletionSource<TResult>
{
    private readonly MyTask _task;

    public MyTaskCompletionSource(TaskCreationOptions creationOptions)
    {
        _task = new MyTask();
    }

    public MyTask Task => _task;

    public bool TrySetException(Exception exception)
    {
        if (exception is null)
        {
            throw new Exception();
        }

        _task.SetException(exception);

        return true;
    }

    public bool TrySetResult(TResult result)
    {
        _task.SetResult();

        return true;
    }
}
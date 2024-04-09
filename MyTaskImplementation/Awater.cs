using System.Runtime.CompilerServices;

namespace Training;

public readonly struct Awaiter(MyTask t) : INotifyCompletion
{
    public bool IsCompleted => t.IsCompleted;

    public void OnCompleted(Action continuation) => t.ContinueWith(continuation);

    public void GetResult() => t.Wait();
}
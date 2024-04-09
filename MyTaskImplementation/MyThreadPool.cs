using System.Collections.Concurrent;

namespace Training;

public static class MyThreadPool
{
    private static readonly BlockingCollection<(Action action, ExecutionContext? executionContext)> _workItems = new();

    static MyThreadPool()
    {
        for (int i = 0; i < Environment.ProcessorCount; i++)
        {
            Thread thread = new Thread(() =>
            {
                while (true)
                {
                    (Action action, ExecutionContext? executionContext) workItem = _workItems.Take();

                    if (workItem.executionContext is null)
                    {
                        workItem.action();
                    }
                    else
                    {
                        ExecutionContext.Run(workItem.executionContext,
                            state => ((((Action action, ExecutionContext? executionContext))state!).action).Invoke(),
                            workItem);
                    }
                }
            })
            {
                IsBackground = true
            };

            thread.Start();
        }
    }

    public static void QueueUserWorkItem(Action action) => _workItems.Add((action, ExecutionContext.Capture()));
}
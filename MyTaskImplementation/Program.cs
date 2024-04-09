using Training;

for (int i = 0; i < 100; i++)
{
    int iterator = i;

    await PrintAsync(iterator);
}

Console.ReadKey();
return;

async MyTask PrintAsync(int iterator)
{
    await MyTask.Run(() =>
    {
        Thread.Sleep(1000);

        Console.WriteLine($"Task {iterator} started in thread {Thread.CurrentThread.ManagedThreadId}");
    }).ContinueWith(() =>
    {
        Console.WriteLine($"Task {iterator} finished in thread {Thread.CurrentThread.ManagedThreadId}");
    });
}
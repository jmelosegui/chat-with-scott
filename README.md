
This repository contains a duplicate of the code demonstrated by Scott Hanselman and Stephen Toub in their YouTube video:
[Writing async/await from scratch in C# with Stephen Toub](https://www.youtube.com/watch?v=R-z2Hv-7nxk)

The objective is to enhance the exercise by incorporating asynchronous support into the MyTask class.

```csharp
static async MyTask PrintAsync()
{
    for (int i = 0; ; i++)
    {
        await MyTask.Delay(1000);
        Console.WriteLine(i);
    }
}
```

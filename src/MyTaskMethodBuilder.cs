using System.Diagnostics;
using System.Runtime.CompilerServices;    

internal struct MyTaskMethodBuilder
{ 
    MyTask _task;

    public MyTaskMethodBuilder()
    {
        _task = new MyTask();
    }

    public MyTask Task => _task;

    public static MyTaskMethodBuilder Create() => new MyTaskMethodBuilder();

    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
    {

        // Store current ExecutionContext and SynchronizationContext as "previousXxx".
        // This allows to restore them and undo any Context changes made in stateMachine.MoveNext
        // so that they won't "leak" out of the first await.
        ExecutionContext? previousExecutionCtx = ExecutionContext.Capture();
        SynchronizationContext? previousSyncCtx = SynchronizationContext.Current;

        try
        {
            stateMachine.MoveNext();
        }
        finally
        {
            // Restore the previous ExecutionContext and SynchronizationContext.
            if (previousSyncCtx != SynchronizationContext.Current)
            {
                SynchronizationContext.SetSynchronizationContext(previousSyncCtx);
            }
            
            ExecutionContext? currentExecutionCtx = ExecutionContext.Capture();
            if (previousExecutionCtx is not null && previousExecutionCtx != currentExecutionCtx)
            {
                ExecutionContext.Restore(previousExecutionCtx);
            }
        }
    }

    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
    where TAwaiter : INotifyCompletion
    where TStateMachine : IAsyncStateMachine
    {
        // This is a very basic implementation. You'll need to replace this with code that correctly
        // hooks up the continuation.
        awaiter.OnCompleted(stateMachine.MoveNext);
    }

    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
    where TAwaiter : ICriticalNotifyCompletion
    where TStateMachine : IAsyncStateMachine
    {
        // This is a very basic implementation. You'll need to replace this with code that correctly
        // hooks up the continuation.
        awaiter.OnCompleted(stateMachine.MoveNext);
    }

    public void SetStateMachine(IAsyncStateMachine stateMachine)
    {
        // Reference: https://source.dot.net/#System.Private.CoreLib/src/libraries/System.Private.CoreLib/src/System/Runtime/CompilerServices/AsyncMethodBuilderCore.cs,81d9cb05b4578797

        // SetStateMachine was originally needed in order to store the boxed state machine reference into
        // the boxed copy.  Now that a normal box is no longer used, SetStateMachine is also legacy.  We need not
        // do anything here, and thus assert to ensure we're not calling this from our own implementations.
        Debug.Fail("SetStateMachine should not be used.");
    }

    public void SetException(Exception exception) => _task.SetException(exception);

    public void SetResult() => _task.SetResult();
}

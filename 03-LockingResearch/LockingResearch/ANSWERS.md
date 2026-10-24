# Assignment 03 — Answers

Student name:  Ahmed Mohamed Mokhtar
Group / ID: .Net Group 2

1. System.Threading.Monitor

- it is a class that helps us to avoid the race condition problem by protecting the critical section
- The Monitor class provides threads to enter one by one into the code block,
  it accepts the thread into the critical section and executes the block with a single thread,
  then releases the thread, This means that all the other threads must wait  the locked section is released
  And this cycle will continue in the same way
- Monitor class is like lock but provides more control by providing built-in methods

2. The C# lock statement compiles down to a try-finally block that uses Monitor.Enter and Monitor.Exit under the hood : 

bool lockTaken = false;
try
{
    Monitor.Enter(_lock, ref lockTaken);
}
finally
{
    if (lockTaken)
    {
        Monitor.Exit(_lock);
    }
}

3. Lock
- Automatically handles lock acquisition and release
- strictly limited to basics (acquiring and releasing an exclusive lock)

Monitor

- Require you to write the method calls manually (such as Monitor.Enter and Monitor.Exit)
- Offers more control over lock using built in methods (Monitor.TryEnter, Monitor.Wait, Monitor.Pulse)

4. Developer might use them when he needs more control over threads

Monitor.TryEnter(): Acquire a lock with a timeout - use it when a thread shouldn't wait for a long time to acquire a lock
Monitor.Wait(): Release the lock and wait for a signal - Use it when a thread needs to pause until another thread signals that some condition may have changed
Monitor.Pulse(): Notify a waiting thread - Use it to wake one thread waiting on the same monitor

5. Lock: 

private readonly object _sync = new object();
private int _counter = 0;

public void Increment()
{
  
    lock (_sync)
    {
        if (_counter < 0) return;
        _counter++;
    }
}

Monitor: 

private readonly object _sync = new object();
private int _counter = 0;

public void Increment()
{
    Monitor.Enter(_sync);
    try
    {
        if (_counter < 0) return;
        _counter++;
    }
    finally
    {
        Monitor.Exit(_sync);
    }
}

Links:


- https://dogukanuhn.medium.com/better-locking-with-system-threading-lock-421a378ed3fe
- https://www.c-sharpcorner.com/article/monitor-and-lock-in-C-Sharp
- https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/lock

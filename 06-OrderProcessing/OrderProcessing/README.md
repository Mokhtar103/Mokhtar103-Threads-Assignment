1. Choosing API in JsonFileStore

- I Used a generic, type safe APIs such as ReadAllAsync<T>(), WriteAllAsync<T>()

Why?

* Reusability: the same implementation works for all entities (customers, products,etc..)
* Type safety: callers specify the type they want to work with
* Separation of concerns: JsonFileStore handles only file operations and serialization
  while repositories decide what data an operation needs
* Consistency: every collection uses the same serialization settings and file handling rules

Phase 1 — Questions & Answers

1. Why are the sync and async times almost the same when orders are awaited one after another?

- In sync version, we use Thread.Sleep() which blocks the current thread,
  in the async version, we use await Task.Delay() which allows the thread to do other work while waiting

- But since we await each order before starting the next one, we don't overlap the delays across different orders
  Therefore, the sync and async times can be similar

2. Is the thread ID before an await always the same as after it? Why? Use your own log lines as evidence

- No, the thread ID is not guaranteed to remain the same after an await

- Because when the compiler reachs await in the code, the thread executing the method is freed
  back to the thread pool making it available to do another work

- When the awaited task is completed, the remaining code after the await is expected to run by an available thread in thread pool,
  Which thread runs the remaining code depends on the runtime enviroment,
  maybe the same thread and maybe not

- Example from my output : 

[16:44:01.038] [Thread 12] ProductRepository -> Opening connection... (200 ms)
[16:44:01.242] [Thread 12] ProductRepository -> Executing query... (327 ms)
[16:44:01.582] [Thread 5] ProductRepository -> Product 1 found

here thread changed from 12 to 5 after async operation

3. What does the thread do while Task.Delay() is waiting? And while Thread.Sleep() is waiting

Task.Delay() : 

- The method suspends the thread at await
- The thread is released back to thread pool making it available to do another work
- When the delay completes, the method continues, maybe on the same or different thread (depends on which one is available)

Thread.Sleep() : 

- The thread is blocked for a specific duration
- it can't be released or do other work while waiting for the duration
- After the duration, the thread can continue execution

4. Why should a repository method return Task<T> instead of T?

- It prevents thread blocking and starvation: returning Task<T> allows us to use async and await, making the method asynchronous,
  The executing thread is released during I/O operations to handle other tasks,
  When the I/O operation finishes and returns data, a thread from the thread pool picks up and continues the job

5. File.ReadAllText vs File.ReadAllTextAsync

File.ReadAllText : 

- Return Type: string
- Waiting Behavior: block the calling thread until I/O operations is finished
- synchronous workflows

File.ReadAllTextAsync : 

- Return Type: Task<string>
- Waiting behavior: releasing the thread without blocking it
- Usually used with await
- asynchronous workflows

** Does the async version occupy a thread while the disk is reading?

- No, when the working thread reachs await, it is freed and sent back to thread pool,
  it doesn't wait for the disk reading to be done


## Phase 2

8.5 - why Task.Run() fits here but not for the database calls?

- Because Task.Run() is suitable for CPU-bound work like invoice generation,
  the thread pool manages the heavy CPU-bound work by assigning the work to an available thread,
  making the main thread free to do other work and keeping the applicaton responsive

- While database calls are I/O bound which require the application to wait for a response,
  the problem with Task.Run() here that the thread of thread pool will be blocked waiting for database,
  making it a bad option

Phase 2 questions

1. What is the difference between starting a task and awaiting it?

- Starting a task means starting an async operation,
  code does not stop, it immediately moves on to the next line of code on the current thread,
  it also returns task object

- When we use await with task, that means pausing the current method until that task completes,
  await extracts the result from the type meaning that it turns Task<int> into an int

2. Does every Task create a new thread? Does Task.Delay() use a thread while waiting?

- No, A Task represents an operation that may complete in the future, it is not bounded to a thread

- CPU-bound tasks are assigned to the thread pool

- I/O bound tasks dont even use a thread while waiting

- Task.Delay() doesn't use a thread while waiting,
  it is just a timer used to simulate async operations
  one the timer is done, a thread from thread pool resume the code

3. .Result or .Wait() on an unfinished task instead of await

- Both block the current thread until the task completes

- .Result blocks and then returns the task's result.
- .Wait() blocks until the task finishes but doesn't return its result

- This is dangerous because of thread blocking, exception wrapping issue

- Exception Wrapping Issue: .Result and .Wait() wrap task exceptions in AggregateException,
  while await propagates the exception directly

- Demo Example : 

private static void BlockingTaskDemo()
{
    Task<int> task = CalculateAsync();

    Console.WriteLine("Waiting synchronously...");

    // Blocks the current thread until the task completes
    int result = task.Result;

    Console.WriteLine($"Result: {result}");

    Task secondTask = Task.Delay(1000);

    // Blocks the current thread for the remaining delay
    secondTask.Wait();

    Console.WriteLine("Second task completed.");
}

private static async Task<int> CalculateAsync()
{
    await Task.Delay(1000);
    return 42;
}

4. What happens to an exception thrown inside a task that is never awaited?

- the exception captured by the task and stored as its failure,
  but the failure is ignored which may cause an error that you might never know about it

5. When should you use Task.Run(), and when should you avoid it?

- Use it for CPU-bound work, the thread pool manages the heavy CPU-bound work by assigning the work to an available thread,
  making the main thread free to do other work and keeping the applicaton responsive

- Avoid it with I/O bound work, the thread of thread pool will be blocked waiting for response,
  making it a bad option

6. What is the difference between Task and Task<T>?

- Task: Represent an operation that will happen in the future that doesn't return a result

- Task<T>: an operation that will happen in the future that produces a result of type T

7. Why can a lock block not contain an await?

- Because lock is strictly tied to a specific thread, but await can change your thread

- await release the thread back to the thread pool until the operation completes,
  when the operation completes a thread from thread pool is assigned to resume the code
  which break the lock rule

8. Why did the unprotected stock reservation oversell, and how did you prevent it?

- The race condition happened because two concurrent orders tried to reserve the last Mechanical Keyboard when its stock quantity was 1

- We moved the stock check and decrement inside UpdateAsync(),
  which acquires SemaphoreSlim making check, read, write an atomic operation
  

## Phase 3 

** 9.6 Questions

1. Does async make one order's workflow faster?

- Not necessarilyو it doesn't reduce database query execution time but
  it doesn't block the thread during I/O operation

2. Why does async scale better for many orders

- Because it releases the thread to work on other things while waiting for I/O response,
  while sync version blocks the thread until the response is delivered

3. What happens to the sync version with 1,000 orders

- it consumes alot of memory and os resources, causing overhead and performance issues

4. Why is async essential for apps that call databases, files and APIs

- Because it has good thread management,
  It allows a tiny number of threads to handle tens of thousands of concurrent requests,
  which make the application more faster and responsive

** 9.7 Questions

1. The problem in the code

- not awaiting email confirmation to finish, which results in order conflict

- the message "Order processing finished" can appears before email confirmation is finished

2. What happens if the email is still being sent?

- if the console application terminates after the Main method returns,
  and there is no other work keeping the process alive, the program might close before the email operation finishes,
  A running Task does not guarantee that the application will remain open until that task completes

3. What happens if the email operation throws an exception

- The exception will not be thrown automatically because there is not await

- the caller will not receive the exception by awaiting ProcessOrderAsync itself

- The exception remains attached to the email task and may shows later as an UnobservedTaskException,
  if it goes unnoticed or unhandled making it so hard to be traced


4. Fix the implementation

Console Output Before Fix : 

OrderRepository -> Saving order #1...
EmailService -> Sending confirmation email...
Order processing finished
EmailService -> Email sent successfully

in case of exceptions nothing appears

Console Output After Fix : 

OrderRepository -> Saving order #1...
EmailService -> Sending confirmation email...
EmailService -> Email sent successfully
Order processing finished

in case of exception : 

OrderRepository -> Saving order #1...
EmailService -> Sending confirmation email...
Error: SMTP server unavailable

5. When await is necessary?

- When you need to be sure that an operation completes before proceeding

- When you need to know whether it succeeded or failed

- Ex: Processing a payment or a refund and handling the outcome

6. When fire-and-forget is acceptable?

- Wthen The operation is not critical, and its failure should never impact the user

- Ex: Sending a message to a user for account registeration

7. Why async void is dangerous?

- Because of several reasons: 
you can't await it
Exceptions cannot be caught in the same way as they are with a Task
You can wait for the operation to complete and coordinate it with other operations

8. Why it is acceptable for event handlers

- Because some event handlers in C# are required to have a signature that returns void

- Ex: UI Events


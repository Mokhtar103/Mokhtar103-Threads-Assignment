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


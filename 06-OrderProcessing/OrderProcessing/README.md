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

## Phase 4 

1. 10.4 Total time for email resend

=== Email Re-send Results ===
Expected emails: 30
Saved test emails: 30
Highest concurrency: 4
Concurrency limit: 4
Total time: 8976 ms
Each email saved exactly once: True

- Each email takes approximately 1 second to send,
 Since we can send at most 4 emails concurrently, the 30 emails are processed in batches

- There are 7 full batches of 4 emails and a final batch of 2 emails, The first 7 batches take approximately 7 seconds, and the last batch takes approximately 1 second

** Phase 4 questions

1. which tool did you use and why can't "start, then await one by one"

- 10.1 => Task.WhenEach => processing each order result as soon as the task completes, 
  without waiting for orders to finish

- 10.2 => Task.Whenall => Starts independent data concurrently and wait for them all to finish

- 10.3 => Task.WhenAny => React to first success carrier and cancel the remaining carriers

- 10.4 => A. Parallel.Foreach => distrubute cpu-bound calculations across cpu cores,
  with configurations (maximum degree of parallelism)

- 10.4 => B. Parallel.ForEachAsync => Run async email operations concurrently

** Why not start a task and await it immediately inside the same loop?

- Because it waits for each order to finish before starting the next one,
  so independent operations cannot make progress concurrently

- Instead, we start the independent operations first and then use the appropriate tool
 (Task.WhenEach, Task.WhenAll, Task.WhenAny)

2. when several loads fail, how do you get every exception?

- By Using Task.WhenAll that completes after all provided tasks have completed,
  If multiple tasks fail, its returned task with failure state

- To inspect every exception, we use foreach after whenAll finishes : 

Task allTasks = Task.WhenAll(
    profileTask,
    ordersTask,
    paymentsTask,
    emailsTask);

try
{
    await allTasks;
}
catch
{
    if (allTasks.Exception is { } aggregate)
    {
        foreach (Exception ex in aggregate.Flatten().InnerExceptions)
        {
            Console.WriteLine(ex.Message);
        }
    }
}

** What does a plain await give you?

- When awaited task fails, it throws only one of the exceptions,
rather than presenting all failures

3. what happens to the losing carriers if you never cancel them?

- The losing carrier requests continue running even after the fastest successful quote has been selected,
  which results in several problems: 

* They consume resources unnecessarily
* Their logs may appear after the winning quote has already been returned
* They may perform unnecessary work and delay application shutdown

4. Why does 10.4 A get faster with more CPU cores, while 10.4 B does not depend on the number of cores?

- Because 10.4 A is CPU-bound. Fraud scoring performs intensive calculations,
  More CPU cores allow more calculations to run in parallel, reducing the total execution time

- While 10.4 B is I/O-bound, Sending emails spends much of its time waiting for SMTP operations and file I/O,
  while one operation waits, another can make progress

5. What is the difference between parallel work (10.4 A) and concurrent asynchronous work (10.1)?

- Parallel work means multiple computations execute at the same time on different CPU cores :

* Ex: Parallel.ForEach in fraud scoring
* Suitable CPU bound work
* Performance can improve as more CPU cores become available

Concurrent async work means multiple operations can be in progress during same period :

* Ex: processing multiple orders using asynchronous methods in live checkout
* Suitable for operations involving databases, files, I/O operations

6. Output after  a cancelled run (--cancel-after)

===== FINAL STATUS COUNTS =====
Pending        : 20
Validated      : 0
Rejected       : 0
PaymentFailed  : 0
Completed      : 0
Failed         : 0
Batch finished in 2.5 s

- Orders: All 20 orders remained in Pending status, The cancellation happened before the orders progress to update their statuses

- Products: The file still contains the 8 seeded products, since all orders remained Pending,
 the expected outcome is that no stock was permanently deducted by a completed order

- Payments: The payments.json file was empty, meaning thatno payment transactions were saved during this run, and no recorded payments to refund


1. 

- How to Run Each Phase

The main menu will appear, select the phase you want to execute

	Phase	                                          What it runs
1	Phase 1 — Order Validation	             Compares synchronous and asynchronous order validation.
2	Phase 2 — Pricing & Concurrency	         Opens a submenu for sequential vs. concurrent pricing, race-condition demonstrations, and full order processing with report g
3	Phase 3 — Order Processing	             Opens a submenu for processing orders and comparing synchronous vs. asynchronous execution
4	Phase 4 — Advanced Async & Parallelism	 Opens a submenu for live checkout, customer dashboard, fastest shipping quotes, fraud scoring, and email re-send
5	Cancellation                             Test	Prompts for a cancellation delay in milliseconds, then runs live checkout with cancellation enabled
0	Exit	                                   Closes the application

- Project structure: 

OrderProcessing/
├── Program.cs
├── OrderProcessing.csproj
├── Shared/
│   ├── Models/
│   ├── Storage/
│   ├── Logging/
│   ├── Exceptions/
│   ├── Helpers/
│   ├── Repositories/
│   └── Interfaces/
├── Phase1/
│   ├── Services/
│   └── Runners/
├── Phase2/
│   ├── Services/
│   └── Runners/
├── Phase3/
│   ├── Services/
│   └── Runners/
└── Phase4/
    ├── Services/
    └── Runners/

- Where are the JSON files created?
bin/Debug/net10.0/Data

- Roles

Program.cs : Application entry point; selects and launches the required phase
Models : Represent customers, products, coupons, orders, order items, payments, emails, statuses and dashboard data
JsonFileStore : Centralizes JSON serialization, file reads and writes, and safe updates
DataSeeder : Creates the sample data and resets the appropriate phase's test files
Repositories : Retrieve and update customers, products, coupons and orders through the storage layer
Services : Implement business logic such as validation, inventory reservation, pricing, payment, email, dashboard loading and shipping quotes
Interfaces : Define contracts such as IEmailService and IShippingCarrier, allowing implementations to be replaced or tested independently
Exceptions : Represent expected failures such as missing customers, invalid coupons, payment-related failures, unavailable shipping providers and email failures
Log : Prints timestamped messages, outcomes and diagnostic information
SimulatedLatency : Adds controlled delays to simulate database queries and external services
Runners : Execute demonstrations and benchmarks, collect measurements and print results


2. Storage design

** JSON file list

The application uses JSON files, Each phase writes its results in data directory :

customers.json:	Customer IDs, names, emails, active status and loyalty tiers
products.json:	Product details, prices, available stock and weight
coupons.json:	Coupon codes, discount percentages and expiry dates
incoming_orders.json:	Original sample orders used as processing input
orders.json:	Processed orders, totals, statuses and failure reasons
payments.json:	Captured, declined or refunded payment transactions
emails.json:	Confirmation emails successfully sent
email_retry.json:	Email messages that failed after the retry attempts
invoices/ :	Invoice files, where implemented
reports/ :	Generated reports, where implemented

** JSON format

- The application uses System.Text.Json to serialize and deserialize C# objects
It includes the following settings : 
1. Camel-case property names, for example, customerId and totalAmount
2. Indented JSON, making files readable
3. String enum values, so statuses are readable instead of stored as numeric values
4. A consistent mapping between JSON records and the corresponding C# models

** How concurrent file access is handled

- The storage design solve this problem in several ways: 
1. Centralized storage: JsonFileStore handles JSON file operations instead of letting every service implement its own file access logic
2. File synchronization: Using semaphore with file paths preventing overlapping updates
3. Atomic replacement: Updates are written to a temporary file and then moved into place, reducing the risk of readers seeing a partially written JSON document
4. Controlled Operations: An update reads the latest data, applies the change and writes the result while holding the appropriate lock


3. Preformance Table


## Performance Results

The following table summarizes the execution times and performance metrics observed across the four phases of the Order Processing System

### Phase 1 — Order Validation

| Operation | Execution Time |
|---|---:|
| Synchronous validation | 32.9 s |
| Asynchronous validation | 34.2 s |
| **Time difference** | **1.3 s (3.95%)** |

Both approaches produced the expected validation results: 15 validated orders and 5 rejected orders

### Phase 2 — Pricing & Order Processing

| Operation | Execution Time |
|---|---:|
| Sequential pricing | 5.48 s |
| Concurrent pricing | 4.77 s |
| Concurrent order processing | 17.36 s |
| **Pricing speed-up** | **1.15×** |

Concurrent pricing completed approximately 13% faster than sequential pricing in this run.

### Phase 3 — Synchronous vs. Asynchronous Processing

| Metric | Synchronous | Asynchronous |
|---|---:|---:|
| Execution time | 3,390 ms | 11,412 ms |
| Threads observed by processor | 10 | 9 |
| ThreadPool threads before execution | 2 | 2 |
| ThreadPool threads after execution | 2 | 10 |

The asynchronous run increased the ThreadPool thread count from 2 to 10, while the synchronous run left it unchanged at 2. In this workload, the synchronous implementation completed faster; asynchronous execution does not inherently guarantee lower elapsed time.

### Phase 4 — Advanced Async & Parallelism

#### Customer Dashboard — Parallel Queries

| Operation | Load Time |
|---|---:|
| First customer dashboard load | 0.53 s |
| Second customer dashboard load | 0.59 s |

#### Fastest Shipping Quote

| Order ID | Selected Carrier | Price | Response Time |
|---:|---|---:|---:|
| #1 | NileCourier | 0.00 | 1,090 ms |
| #2 | FastShip | 7.30 | 1,325 ms |
| #3 | FastShip | 10.70 | 1,061 ms |
| #4 | FastShip | 0.00 | 1,168 ms |
| #5 | NileCourier | 18.15 | 1,109 ms |

*Prices are shown in the same currency units used by the application.*

#### Fraud Scoring — Sequential vs. Parallel

| Metric | Result |
|---|---:|
| Orders processed | 10,000 |
| Sequential flagged orders | 1,005 |
| Parallel flagged orders | 1,005 |
| Sequential execution time | 10,213 ms |
| Parallel execution time | 1,233 ms |
| **Speed-up** | **8.28×** |
| Distinct threads observed | 12 |
| Available processor count | 12 |

Parallel fraud scoring produced the same results as sequential scoring while reducing execution time by approximately 87.9%.

#### Email Re-send — Bounded Concurrency

| Metric | Result |
|---|---:|
| Expected emails | 30 |
| Saved test emails | 30 |
| Maximum observed concurrency | 4 |
| Configured concurrency limit | 4 |
| Total execution time | 9,035 ms |

The email re-send process handled all 30 test emails while respecting the configured concurrency limit of four simultaneous operations.

---

### Performance Summary

| Phase | Key Result |
|---|---|
| Phase 1 | Sync and async validation times differed by 1.3 seconds |
| Phase 2 | Concurrent pricing was approximately 13% faster |
| Phase 3 | Sync completed faster for the measured workload |
| Phase 4 — Dashboard | Both dashboard loads completed in under 0.6 seconds |
| Phase 4 — Shipping | Fastest successful carrier quotes were selected |
| Phase 4 — Fraud Scoring | Parallel execution achieved an 8.28× speed-up |
| Phase 4 — Email Re-send | All 30 emails processed with concurrency capped at 4 |


4. Problems encountered and how they were solved

- Concurrent updates could overwrite one another => Centralized file operations in JsonFileStore and used per-path synchronization for protected updates
- A crash during a write could leave a JSON file partially written =>	Used temporary files and atomic replacement for file updates
- Two orders could reserve the same remaining stock =>	Protected inventory updates and used a race-condition demonstration to expose and address shared-state problems
- Orders could fail at different processing stages =>	Added explicit statuses and failure reasons to distinguish rejected orders, payment failures, completed orders and failed post-payment processing
- A payment could succeed while saving the completed order failed =>	Added compensation logic to refund the payment, release reserved stock and record the failed order
- Payment requests could take too long =>	Used cancellation and timeout handling, with compensation when necessary
- An email provider could fail temporarily =>	Added retries with increasing delays and recorded messages that remained unsuccessful in email_retry.json
- One slow or failed dashboard query could affect the overall result =>	Loaded independent data concurrently with Task.WhenAll and aggregated failures for diagnosis
- A shipping provider could fail or respond too slowly =>	Queried providers concurrently, selected the first successful quote and implemented a timeout/fallback policy
- CPU-heavy fraud scoring could appear to freeze the application =>	Recognized the high iteration count and added progress reporting. Parallel processing was used to compare CPU-bound execution strategies
- Cancellation could occur before any order was processed =>	Added a configurable cancellation delay and inspected the resulting order, stock and payment files to understand the outcome
- Output files from different phases could overwrite one another =>	Organized results into separate phase directories and configured storage to target the relevant directory
- Thread metrics could be misleading =>	Distinguished unique thread IDs observed during a test from the process-wide ThreadPool.ThreadCount value

5. Console Screenshots: 

- Phase 1 Screenshots :

![Phase 1 - Sync vs Async](screenshots/Phase%201%20-%20Sync%20vs%20Async.png)

- Phase 2 Screenshots : 

![Phase 2 - Concurrent Order Processing](screenshots/Phase%202%20-%20Concurrent%20Order%20Processing.png)

![Phase 2 - Sequential vs Concurrent Pricing](screenshots/Phase%202%20-%20Sequential%20vs%20Concurrent%20Pricing.png)

![Phase 2 - Race Condition Demo](screenshots/Phase%202%20-Race%20Condition%20Demo.png)

- Phase 3 Screenshots : 

![Phase 3 - Order Processing.png](screenshots/Phase%203%20-%20Order%20Processing.png)

![Phase 3 - Sync vs Async 1.png](screenshots/Phase%203%20-%20Sync%20vs%20Async%201.png)

![Phase 3 - Sync vs Async 2.png](screenshots/Phase%203%20-%20Sync%20vs%20Async%202.png)

- Phase 4 Screenshots : 

![Phase 4 - Customer Dashboard](screenshots/Phase%204%20-%20Customer%20Dashboard.png)

![Phase 4 - Email Re send](screenshots/Phase%204%20-%20Email%20Re%20send.png)

![Phase 4 - Fraud Scoring](screenshots/Phase%204%20-%20Fraud%20Scoring.png)

![Phase 4 - Live Checkout](screenshots/Phase%204%20-%20Live%20Checkout.png)

![Phase 4 - Shipping quote](screenshots/Phase%204%20-%20Shipping%20quote.png)

![Canellation Run](screenshots/Cancellation%20Run.png)

Final Note: Didnt Save JSON files into sample-output/ since i read tech requirements of the projects late and had no time for refactoring
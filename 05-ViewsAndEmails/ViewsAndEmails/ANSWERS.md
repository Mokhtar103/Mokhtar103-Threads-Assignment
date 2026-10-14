# Assignment 05 — Answers

Student name:  Ahmed Mohamed Mokhtar 
Group / ID: .Net Group 2

## Failures you observed

- We can split them into 3 Failures : 

1. Dictionary Exception

- Argument Exception: An item with the same key has already been added, this exception occured
  due to more than one thread checked ContainsKey at the exact same moment,
  both saw false, and both tried to call Views.Add()

2. Queue Exception

- InvalidOperationException:  Operations that change non-concurrent collections must have exclusive access
  this exception occured due to race condition, thread 1 checks the condition,
  but before it can call Dequeue(), thread 2 gets ahead of it and dequeues the item,
  thread 1 then proceeds to call Dequeue() on an empty queue

3. Loss of Updates in the output

- Due to race condition, there is a failure in incrementing the counters accurately which results in incomplete data: 

Views expected=50000, actual=22411 (actual less than expected)
Email jobs placed=20, unique sent=19, total sends=19 (sent less than jobs placed)

## What you changed

- Used Concurrent Dictionary and queue with their built-in methods Because they are thread safe

- Concurrent Dictionary makes each call atmoic, put the whole rule in one call (like AddOrUpdate we used)

- Concurrent Queue uses trydequeue that checks and take action in one step

## Collection safety vs business-rule safety

- Using Concurrent Dictrionary and queues makes our collection calls thread safe

- But a safe collection is not safe logic, for example: every single dictionary call is safe,
  but our business rule may look like a sequence: check, then read, then write,
  another thread can run in between and results in race condition problems
  and in this case using safe collections wont prevent the problem

- When several steps must happen together, protect the whole sequence (like using lock),
  Thread safe parts do not make the whole code thread safe



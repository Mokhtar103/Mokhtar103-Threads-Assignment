# Assignment 01 — Answers

Student name:  Ahmed Mohamed Mokhtar Mohamed
Group / ID: .Net Group 2

## Problems with the current design

1. Blocking Operations: Thread.Sleep() blocks the current thread instead of allowing it to do other work

2. No Asnyc Methods for I/O bound operations: methods are being blocked while waiting for external response (from database or file)

Output with these problems is : Export finished in 6.0 s (with path printing for each file)

## What you changed

1. Removed Artificial Delay: Removed every Thread.Sleep() so the thread is not blocked anymore, which improved the preformance

2. Added Async / await  to I/O bound operations: Asynchronous operations can release the thread while waiting,
   and multiple orders can be processed concurrently

3. Added Some error handling to export method: to simulate if someone canceled the export service

Output with the changes have been changed is : Export finished in 0.0 s (with path printing for each file)

# Assignment 02 — Answers

Student name:  Ahmed Mohamed Mokhtar
Group / ID: .Net Group 2

## What is wrong

1. Race Condition: Multiple threads evaluate if condition at the same time, pass the check,
   and proceed to decrement TablesLeft and increment Booked

-Why? : Because TablesLeft and Booked live on one object used by all created threads
Check, TablesLeft--,  Booked++ are seperate steps, a thread can slip in between them while other thread is working


First 5 Outputs with these problems : 
- Booked=22, Tablesleft=4
- Booked=38, Tablesleft=-31
- Booked=10, Tablesleft=4
- Booked=40, Tablesleft=4
- Booked=36, Tablesleft=-11

## How you fixed it

1. Adding Lock: an object that coordinate which threads are allowed to proceed and which must wait,

** Simulating When a thread reaches the lock block:
- It checks if another thread is currently inside that block
- If no other thread is inside, it acquires the lock and enters
- If another thread is already inside, the current thread is blocked by the os and forced to wait in line

2. All Steps act as an atomic operation: When putting the check, increments/decrements inside the lock,
   those steps turn into a single operation

First 5 Outputs After the fix :

- Booked=5, TablesLeft=0
- Booked=5, TablesLeft=0
- Booked=5, TablesLeft=0
- Booked=5, TablesLeft=0
- Booked=5, TablesLeft=0

   

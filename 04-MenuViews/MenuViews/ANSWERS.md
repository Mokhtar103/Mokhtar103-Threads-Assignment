# Assignment 04 — Answers

Student name:  Ahmed Mohamed Mokhtar
Group / ID: .Net Group 2

## Why Expected and Actual disagree

- They Disagree because of a race condition since increment operation is not atomic

- Views++ looks like a single line of code but it consists of 3 steps (read, increment, write)

- Multiple threads are running into the loop concurrently which cause loss of updates and overlapping

## What you changed

- Used Interlocked.Increment Because it treats read, increment, write steps as one atomic step, the result will always be the same

## When would you protect the update differently

- I can use lock instead of interlocked in case i had a check condition or several variables that must change together

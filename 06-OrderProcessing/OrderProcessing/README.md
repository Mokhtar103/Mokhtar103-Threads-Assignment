1. Choosing API in JsonFileStore

- I Used a generic, type safe APIs such as ReadAllAsync<T>(), WriteAllAsync<T>()

Why?

* Reusability: the same implementation works for all entities (customers, products,etc..)
* Type safety: callers specify the type they want to work with
* Separation of concerns: JsonFileStore handles only file operations and serialization
  while repositories decide what data an operation needs
* Consistency: every collection uses the same serialization settings and file handling rules
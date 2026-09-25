### Closed tasks

- Add tests
  - Add tests for Backend
    - Collection: finish dispatcher tests
      - Add tests for the EventHandlerDispatcher
      - Solve EventCollectorContext runtime issue

- Handle failure in event dispatching
  - In the EventHandlerDispatcher there is an attempt to call the function
  delegate if it's found in the dictionary. But the execution of that method
  might fail if a channel was not properly configured. Add handling for it
  and a test case.
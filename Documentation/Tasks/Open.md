### Open Tasks

- Add terminal GUI library to improve ProcessMonitor CLI experience 
  - Currently the application is not serving well from the UX perspective.
  If a user starts up the telemetry collection, everything gets written onto 
  the same buffer which also gets completely cleared every time a new query
  is sent. This is still doable since currently the server is not running on
  the background making the telemetry easier to see due to server logs. But 
  ultimately the goal is not to have it like this and rather split the 
  terminal into segments for outputting the corresponding data stream. This
  would require adding a new dependency so the right one has to be chosen.
  Also it's not a requirement for the basic release thus this change is yet
  to come.

- Refactor IHost instance initialization
  - From the developer's perspective, the project is built using a clunky but
  working builder class for the client side. It's not as essential as other
  tasks yet refactoring the codebase would make it more tidy and ready for
  getting documented in the developer's manual.

- Add tests
  - Add tests for Backend
    - Collection: finish dispatcher tests
      - Add tests for the EventHandlerDispatcher
      - Solve EventCollectorContext runtime issue
    - Commands: add tests 
    - Hosting: add tests 
    - Processing: add tests 
    - Publishing: add tests
    - State: add tests
    - Transport: add tests
  - Add tests for CLI
    - Hosting: add tests
  - Add tests for Shared
    - Client: add tests
    - Serialization: add tests
    - Transport: add tests

- Extend the processing capabilities
  - The engine is only capable of handling very basic telemetry. Consider
  adding more features for the future releases such as: CPU registers' data,
  child processes spawned, allocated memory diagnosis, etc.

- Reconsider current WPF implementation
  - The current codebase for the WPF frontend was a result of experimenting
  with the framework and this left a mark in the code mainly in inconsistent
  namings, unclear binding and overgrowing global state class. Consider 
  taking a look at the code and find out how to reorganize it to be less 
  chaotic and more suitable for injecting custom style templates.

- Incorporate the new Result type to the Backend
  - Right now the application is based on the nullable Exception pattern.
  This is working but it makes it awkward to propagate the errors further.
  To make the application easier to extend from the logging perspective, 
  rewrite the code so that it uses the functional approach more.

- Add CI 
  - As a step towards automating integration pipeline, define the CI logic
  for the application that also uses the tests and runs on a Windows server.

- Improve README.md 
  - Current readme file is very primitive and has to be refactored showing 
  the demo of the project alongside a more detailed explanation on how the 
  application behaves, hot it's built, developed and run.

- Add Manual 
  - Nothing has any sort of documentation right now. Define a collection of
  markdown files which correspond to the files in the Source directory.

- Add Scripts folder
  - PowerShell scripts are increasing and they have to be organaized in 
  their own directory. Add the Scripts folder which would contain the 
  shared code in addition to specialized ones for building testing and
  publishing the application

- Add 'Entry' script
  - Add a script file to the root of the directory to act as a master 
  script that handles the other ones with a convenient cli interface for
  a developer to use.

- Add PMQL project
  - Extract the query parsing code into a separate project to not overgrow
  the existing codebase for the client. 

- Improve PMQL 
  - Extend the parsing by adding more commands and introducing conditionals
  and environent variables access for simplifying the UX.

- Add 'Query' mode for Desktop
  - Add a new mode to the existing ones so that a user has an editor where 
  they can write queries similar to a database management studio 

- Handle failure in event dispatching
  - In the EventHandlerDispatcher there is an attempt to call the function
  delegate if it's found in the dictionary. But the execution of that method
  might fail if a channel was not properly configured. Add handling for it
  and a test case.


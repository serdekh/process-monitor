### Open Tasks

All the open tasks can be subdivided into the following groups: 

- Fix (1st priority)
  - A change that needs to be done to the existing code. Usually due to a bug or design flaw (bad architecture for example).
- Release (2nd priority)
  - Describes a task which has to be done for the next release. Usually implying a bug fix or adding a new feature. 
- Extension (3d priority)
  - A new feature for the project which would improve it but not as essential as the 'Release' task

<hr>

`Fix`
- Refactor the engine and the collector to work together in a more clear way
  - Today I had to figure out the problem of the backend which caused no snapshots to show up. I found out that the problem was a mix of everything related with the events. Both the fact that I don't clone them and a silly mistake in a boolean property. It is visible that certain properties of the engine and the collector are shared and in the best case scenario both layers would rely on the same monitoring state object that provides enough context. In order to find out if such merge of contexts is possible, first the engine has to be broken down into modules. Just like the 'Collection' layer. Then the modules would need to get examined properly. After that, take a look at what they are and try to define a better version of the 'MonitoringSessionState' class to fit it in.

`Fix`
- Add more convenience to work with Results by introducing factors without the need to type generic init all over the places. Also apply for of that style to make some functions bindable.
 
`Fix`
- Refactor IHost instance initialization
  - From the developer's perspective, the project is built using a clunky but
  working builder class for the client side. It's not as essential as other
  tasks yet refactoring the codebase would make it more tidy and ready for
  getting documented in the developer's manual.

`Fix`
- Reconsider current WPF implementation
  - The current codebase for the WPF frontend was a result of experimenting
  with the framework and this left a mark in the code mainly in inconsistent
  namings, unclear binding and overgrowing global state class. Consider 
  taking a look at the code and find out how to reorganize it to be less 
  chaotic and more suitable for injecting custom style templates.

`Fix`
- Improve README.md 
  - Current readme file is very primitive and has to be refactored showing 
  the demo of the project alongside a more detailed explanation on how the 
  application behaves, hot it's built, developed and run.

`Release`
- Add tests
  - Add tests for Backend
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

`Release`
- Add CI 
  - As a step towards automating integration pipeline, define the CI logic
  for the application that also uses the tests and runs on a Windows server.

`Extension`
- Extend the processing capabilities
  - The engine is only capable of handling very basic telemetry. Consider
  adding more features for the future releases such as: CPU registers' data,
  child processes spawned, allocated memory diagnosis, etc.

`Extension`
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

`Extension`
- Add Manual 
  - Nothing has any sort of documentation right now. Define a collection of
  markdown files which correspond to the files in the Source directory.

`Extension`
- Add Scripts folder
  - PowerShell scripts are increasing and they have to be organaized in 
  their own directory. Add the Scripts folder which would contain the 
  shared code in addition to specialized ones for building testing and
  publishing the application

`Extension`
- Add 'Entry' script
  - Add a script file to the root of the directory to act as a master 
  script that handles the other ones with a convenient cli interface for
  a developer to use.

`Extension`
- Add PMQL project
  - Extract the query parsing code into a separate project to not overgrow
  the existing codebase for the client. 

`Extension`
- Improve PMQL 
  - Extend the parsing by adding more commands and introducing conditionals
  and environent variables access for simplifying the UX.

`Extension`
- Add 'Query' mode for Desktop
  - Add a new mode to the existing ones so that a user has an editor where 
  they can write queries similar to a database management studio 
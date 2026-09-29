<div align="center">
    <img src="../../../Images/logo.png" alt="'ProcessMonitor logo'" width="300"/>

   # ProcessMonitor Documentation: Projects 
   ## ProcessMonitor.Backend
</div>

### Sections

- 1: [_**Description**_](#description)
- 2: [_**Architecture and Data Flow**_](#architecture-and-data-flow)
- 3: [_**Project Layers**_](#project-layers)
- 4: [_**References**_](#references)

### Description

The ProcessMonitor.Backend project (later will be referred as a _**Server**_) provides the core functionality for the entire solution.  It collects process related events, builds up the metrics and sends them to the _**Client**_ applications which were previously configured by the build system. 

Without the _**Server**_, the _**Clients**_ would not be capable to perform any calculations. 

The _**Server**_ can run by itself and log the collected information directly but the default behaviour involves an [_invocation by a **Client** process whenever the metrics are requested_](#architecture-and-data-flow).

### Architecture and Data Flow

The _**Server**_ is responsible for performing the following tasks:

- _1: Running several [**background services**](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-10.0&tabs=visual-studio) using a [**generic host**](https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host?tabs=appbuilder)._
- _2: Collecting kernel provider events using an [**event tracing system or ETW**](https://learn.microsoft.com/en-us/windows/win32/etw/about-event-tracing#providers)._
- _3: [**Logging**](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging/overview?tabs=command-line) errors, warnings and other messages._
- _4: Building up the metrics from the accumulated events (aka **Telemetry**)._
- _5: Serializing and deserealizing metrics into/from the messages._
- _6: Sending the telemetry via a **Telemetry** [**pipe**](https://learn.microsoft.com/en-us/windows/win32/ipc/named-pipes)._
- _7: Receiving the _**Client**_ commands via the **Commands** [**pipe**](https://learn.microsoft.com/en-us/windows/win32/ipc/named-pipes)._
- _8: Updating the inner state and sending responses via the **Commands** [**pipe**](https://learn.microsoft.com/en-us/windows/win32/ipc/named-pipes)._

<hr>

In the _data flow_ terms, the _**Server**_ is working with two main [**pipes**](https://learn.microsoft.com/en-us/windows/win32/ipc/named-pipes) called _**Telemetry**_ and _**Commands**_

<hr>

_**Telemetry**_ refers to the whole process that starts at collecting the process events and ends up at sending the finalized and serialized metrics to the _**Client**_. The general order of actions looks like this:

- 1: Read a global state class
  - _To get a process ID._

- 2: Event Collection
  - _Once a process ID is defined, start an event session, collect the events, filter the unnecessary ones, and push them into an [**asynchronous channel**](https://learn.microsoft.com/en-us/dotnet/core/extensions/channels)._

- 3: Metrics build
  - _Read from the [**asynchronous channel**](https://learn.microsoft.com/en-us/dotnet/core/extensions/channels) that contains the raw kernel events, accumulate the data into a metrics object for a specified time duration (default 400ms) and push the final metrics class into another [**asynchronous channel**](https://learn.microsoft.com/en-us/dotnet/core/extensions/channels)._

- 4: Publishing
  - _Read the [**asynchronous channel**](https://learn.microsoft.com/en-us/dotnet/core/extensions/channels) with the finalized metrics, serialize and send them via the _**Telemetry**_ [**pipe**](https://learn.microsoft.com/en-us/windows/win32/ipc/named-pipes)._

<hr>

_**Commands**_ refer to the job the _**Server**_ performs starting at receiving the incoming requests (_**Commands**_ [**pipe**](https://learn.microsoft.com/en-us/windows/win32/ipc/named-pipes)) and finishing at updating the inner state and sending the response back to the (_**Commands**_ [**pipe**](https://learn.microsoft.com/en-us/windows/win32/ipc/named-pipes)). The general order of actions looks like this:

- 1: Reading the _**Commands**_ [**pipe**](https://learn.microsoft.com/en-us/windows/win32/ipc/named-pipes)
  - _This [**pipe**](https://learn.microsoft.com/en-us/windows/win32/ipc/named-pipes) is bidirectional and is used both for the requests and the responses._

- 2: Parsing the request and identifying the command
  - _The [**json**](https://en.wikipedia.org/wiki/JSON) file is parsed and depending on which command is defined, the mapping is performed to a dedicated handler_.

- 3: Handling the command
  - _A specified command handler is invoked, the inner state gets updated and the response object gets created_.

- 4: Serializing the response
  - Once the state is updated, the response object gets created, serialized and pushed back to the _**Commands**_ [**pipe**](https://learn.microsoft.com/en-us/windows/win32/ipc/named-pipes)_.

<hr>

As a result, the final architecture is built on top of working with two [**named pipes**](https://learn.microsoft.com/en-us/windows/win32/ipc/named-pipes). The data flow is independent and involves using [**asynchronous channels**](https://learn.microsoft.com/en-us/dotnet/core/extensions/channels) for reading and writing the temporary _**Telemetry**_ computations. In order to achieve this behaviour, the project is split into several layers where each layer is responsible for its own dedicated task:

### Project Layers

```
ProcessMonitor.Backend
│
├── Collection
├── Commands
├── Hosting
├── State
├── Models
├── Processing
├── Publishing
├── State
└── Transport
```

The layers are simply folders which are simultaneously the **C#** namespaces. Here is the breakdown for each of them:

**`Collection`**
``` 
This layer contains logic for reading the incoming events. Here is how it works step by step:
    1: Define classes which initialize a new ETW session
    2: Subscribe to the KernelProvider
    3: Read the 'State' layer classes to get the process id
    4: Filter the events and push the relevant ones to a dedicated asynchronous channel. 
       
    The logic of the class is wrapped into a 'BackgroundService' class which is then fed to the 'IHost' instance.
```

**`Commands`**
```
This layer is responsible for parsing and executing the commands. Here is how it works step by step:
    1: Read from the Commands pipe and deserealize the incoming request. 
    
    If it is a valid request (according to the protocol):
        2: Route it to a designated handler class depending on
            method (put, post, delete, …)
            path (monitoring/start, monitoring/stop…). 
    else: 
        2: Generate a response with the error message and return it via the Commands pipe back. Stop here.
        
    3: Update the inner state. 
    4: Create a response object.
    5: Initialize, serialize and send back to the Commands pipe. 
    
    The logic of the class is wrapped into a 'BackgroundService' class which is then fed to the 'IHost' instance.
```

**`Hosting`**
``` 
Contains the project layer wrappers defined as 'BackgroundService' class instances

Defines the main file called 'ProcessMonitorHostBuilder.cs' which creates an instance of the 'IHost' interface and 
configures it to use the services mentioned above.
```

**`Models`**
``` 
Defines data structures which represent internal behaviour of the server. Those include:

1: Wrapper types for the abstract TraceEvent class.
2: Custom errors and warnings.
3: Raw unparsed event representations.
4: Metrics accumulators.
```

**`Processing`**
```
The heart of the application. It takes a collection of preconfigured dependencies and perfroms
calculations for the raw unparsed events. Then the accumulated metrics get pushed into a 
separate asynchronous channel for the metrics objects. 
```

**`Publishing`**
``` 
This layer acts as a consumer for the 'Processing' layer. If the metrics objects channel is
not empty, it tries to perfrom a transportation to a client. 

The logic of the class is wrapped into a 'BackgroundService' class which is then fed to the 'IHost' instance.
```

**`State`**
``` 
A global reference service that gets injected into consumer classes for interacting with a currently 
targeted process. It defines properties for interacting with a process id. Everything is locked preventing
race conditions. 
```

**`Transport`**
``` 
Utilizes the 'ITransport' interface which is defined in the ProcessMonitor.Shared project.

That interface gets injected into the server's implementation allowing a convenient IPC handling without
the details of framing and serialization.

This layer is consumed by the 'Commands' layer since it interacts with the pipes directly.
```

<hr>

> Note: These docs are not completed. They require more details on each layer which would probably also force to create 
> separate files per layer. Expect this to be changed a lot until a stable release comes up.

### References

- [**Background services in .NET**](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-10.0&tabs=visual-studio)
- [**Generic host in .NET (IHost)**](https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host?tabs=appbuilder)
- [**Event tracing system for Windows (ETW)**](https://learn.microsoft.com/en-us/windows/win32/etw/about-event-tracing#providers)
- [**Logging in .NET**](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging/overview?tabs=command-line)
- [**Named Pipes (NPFS)**](https://learn.microsoft.com/en-us/windows/win32/ipc/named-pipes)
- [**Asynchronous channels in .NET**](https://learn.microsoft.com/en-us/dotnet/core/extensions/channels)
- [**JSON File Format**](https://en.wikipedia.org/wiki/JSON)
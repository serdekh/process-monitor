# ProcessMonitor
Client-Server based Windows Desktop Application for Process Diagnosis

## Description

ProcessMonitor is a Windows Desktop application dedicated for dynamic process diagnostics.
The goal is to provide a convenient user interface for analyzing a process' statistics.
Those include: context switches, syscall invocations, process cpu time usage, thread
cpu usage, thread starts & stops etc.

The architecture will be client-server based. It involves having two distinct programs
that talk with each other via the `IPC` using `Named Pipes` file system. 

The server program will perform reading process-related events, compute metrics and
send them to a client.

The client program will be able to create a server process, connect to it and interact
to read metrics and write instructions.

Right now, the project is in its initial development stage. There is no working prototype
and the repo is mostly empty. It is planned to add features one by one and eventually the
working application will be shipped for a release.

## Quick Start

The project does not have prebuilt binaries for now since it's being actively developed.
To use the application in its current state you have to build it yourself. Here is how:

1: Ensure that you have the .NET SDK installed. Check it by running this command

```
dotnet --version
```

If you have it installed, you will see the version number otherwise it'll be not recognized.
In that case go to the official website (`https://dotnet.microsoft.com/en-us/download`) and
download the .NET 9+ version.

Once this is setup, go to the backend project (`./Source/ProcessMonitor.Backend/`) and build
it by running this command:

```
dotnet build
```

It will generate the assemblies the client application will use to run the metrics. Take a look
at the local `bin\Debug\net9.0` folder. There you will find the `ProcessMonitor.Backend.exe` file.
Copy its filepath for the client app.

Then go to the `CLI` project (`./Source/ProcessMonitor.CLI/`) and build it the same way:

```
dotnet build
```

you will find the executable at (`ProcessMonitor.CLI/bin/Debug/net9.0/`). Run it and provide the
`--path <backend-filepath>` in the arguments:

```
.\ProcessMonitor.CLI.exe --path <your-path-to-the-backend>
```

Enjoy dealing with my profanity :)

## Goals

Here are the goals that are now defined for this project in order:

[✓] Make a basic working skeleton (Define core project structure, add the essential types
   for simply booting the server project)
   
   -- [✓] Implement proper 'Collection' layer
   
   -- [✓] Implement basic metrics computations
   
   -- [✓] Add logging
   
   -- [✓] Add exception handling for background services
   
   -- [✓] Prepare 'Commands' layer for the second goal
   
[✓] Add a cli client (Create a separate C# Console App for interacting with the server
   in a simpler way)

[-] Improve the existing skeleton implementation

    -- [-] Refactor the Transport-based layers in both the backend and the
           cli frontend to be more flexible. The initial implementation 
           considered the Transport layer to be used solely by the backend
           project but it turned out to be a shared feature. Thus it has to
           be reimplemented to support the shift to the Shared project.
           
    -- [-] Create an error-propagated system based on a union type
           to make the error handling work on the background service level.
           Instead of immediate logging right after an error occurs, force 
           all the error-prone methods to return a union based type and 
           make the consumers dispatch it.
           
    -- [-] Improve CLI Interface by introducing a UI framework for rendering
           different components (telemetry, prompt and logs) in their 
           corresponding places. For now, everything gets printed into a 
           single stdout stream making the app unresponsive when the telemetry
           data arrives.
   
    -- [-] Improve the existing metrics engine to support more computations

    -- [-] Introduce a separate project for testing purposes
   
[-] Add documentation (Create a consistent style of guidelines and manuals)

[-] Add a Desktop frontend (Create a WPF project integrate with the existing ecosystem)

[-] Define release structure (Define concrete `IPC API` protocol, type definitions,
   bundle structure and create a first stable release)
   
[-] Consider extending the project with new features (different Frontends,
   api updates, others...)

## Contributing

Before a stable release comes, no contributes are considered to be included.

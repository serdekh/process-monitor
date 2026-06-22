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

Not available until the first goal is complete

## Goals

Here are the goals that are now defined for this project in order:

[-] Make a basic working skeleton (Define core project structure, add the essential types
   for simply booting the server project)
   
[-] Add a cli client (Create a separate C# Console App for interacting with the server
   in a simpler way)
   
[-] Extend the server (Add more diagnostic parameters, improve `IPC` protocol, complete
   a set of client commands)
   
[-] Add documentation (Create a consistent style of guidelines and manuals)

[-] Add a Desktop frontend (Create a WPF project integrate with the existing ecosystem)

[-] Define release structure (Define concrete `IPC API` protocol, type definitions,
   bundle structure and create a first stable release)
   
[-] Consider extending the project with new features (Testing, different Frontends,
   api updates, others...)

## Contributing

Before a stable release comes, no contributes are considered to be included.

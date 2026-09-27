<div align="center">
    <img src="../../Images/logo.png" alt="'ProcessMonitor logo'" width="300"/>

   # ProcessMonitor Documentation: Overviews

A collection of guidelines, general descriptions and navigation though the documentation

</div>

- [**General Description**](#Description)
- [**Core Architecture**](#Architecture)
- [**Protocols Reference**](#Protocols)
<br/>

<div align="center">

## Description

</div>
<br/>

ProcessMonitor is **client-server** based **Windows** **desktop application** for tracking runtime data and statistics of a particular **process** such as:
- **Process CPU usage**
- **Threads count**
- **Thread lifetimes**
- **System calls count**
- **Context switching count**

<div align="center">

## Architecture

</div>
</br>

The architecture splits the **UI** from the **business logic** and defines two distinct applications for either task.

The **UI** application will be further referenced as a **_Client_**.
Similarly, the **business logic** implementation will be further referenced as a **_Server_**. 

In order to communicate with each other, both applications utilize the core system of the **Windows Operating System** called **Named Pipes**.

**Named Pipes** compose a separate **file system**. For security reasons, every process exists in a restricted memory space that was allocated by the operating system before the execution. If the processes could access each others memory, it would lead to data corruption and highly potential security risks since no protection for data access would exist. Thus, every modern operating system provides such restriction to prevent any bugs. But the processes still need to share data with each other. To solve this issue, an operating system defines a pipe for processes to interact with. Instead of direct communication, a process can write data into this pipe or read from it depending on the pipe configuration and the application logic. On most **UNIX**-like operating systems like **Linux** this is achieved through the **socket API** which acts as an intermediate layer. When processes interact with the **sockets**, they send **system calls** and notify the kernel that they need to share data between each other. On **Windows** this is achieved through the **Named Pipes API** as mentioned above. They are essentially identical in their behaviour with the differences being only in the technicalities such as the function signatures and the **ABI** as a whole.

It's worth mentioning that another form of data transmission exists called **Shared Memory Space**. It simplifies the solution to the fact there is no intermediate layer of **pipes** or **sockets** whatsoever. Instead, the operating system merges the memory space of a second process to the first one. This approach has its own advantages and disadvantages. The main advantage is no overhead. Since a process is no longer limited to access another process' memory space, there is no need to request a system call for it. But this comes with a price of higher risks for the race conditions. Thus, every data access is a critical section and has to be locked. Not to mention that this approach also scales hard if more clients would need to be supported which would exponentially increase the memory layout handling. 

Both the **Shared Memory Space** approach and the **Named Pipes/Sockets API's** are collectively referred as two forms of **inter-process communication** or **IPC**. 

In the **ProcessMonitor** project, we use the **Named pipes** form of **IPC**.

The whole project itself is composed of a solution (.sln file) that contains several other project references within it. Those include:

- **ProcessMonitor.Backend**
  - implements the _**Server**_ model of the core architecture. This is the heart of the application. Under the hood it is built on top of the **TraceEvent** library and ultimately the **Event Tracing System for Windows** or **ETW**. Thanks to this library, the _**Server**_ is capable of collecting system events and parse them in its engine. The _**Server**_ also exposes multiple layers for data transmission via **IPC**, commands handling, application hosting and other modules.

- **ProcessMonitor.Backend.Tests**
  - defines a collection of test files for each layer of the **ProcessMonitor.Backend** project. Built on top of the **xUnit Framework**.
    > Note: For now only the _Collection_ layer of the project is fully supported.

- **ProcessMonitor.CLI**
  - defines a console client application. It is used for testing and as a fall-back option in case the desktop application crashes or not configured to be executed. It provides a simple shell system in which a user can type different commands and manipulate with the **_Server_** process.

- **ProcessMonitor.Shared**
  - defines a collection of models and services which are not tightly coupled to a specific project and can be reused without reimplementing the same logic multiple times. This includes the **IPC** data transmission abstractions, the **_Client_** application configuration and the shell script.

- **ProcessMonitor.WPF**
  - defines a **_Client_** project based on the **Windows Presentation Framework**. It works identically to the **ProcessMonitor.CLI** project but with a convenient **GUI** interface. It uses the **ProcessMonitor.Shared** project to define a global state object which manipulates the **UI** in a responsive manner. Every user component in the project acts against that static global state.

To Find more details for each of these project, you can go the following directories: **Overviews/Projects/{Project-Name}**

<div align="center">

## Protocols

</div>
</br>

The ProcessMonitor.CLI and ProcessMonitor.WPF are the default **_Client_** implementations. In order to talk with the backend, they implement a shared interface or _protocol_ for the HTTP-like json communication. 

A **_Client_** provides information such as: _process ID, start request, stop request and settings modification requests_ (for instance, modify the delay between sending the next telemetry message). The _**Server**_ listens to those requests, updates its inner state and sends a response that includes a status. 

Both the **_Client_** requests and the **_Server_** responses are traveling in the duplex **_Commands_** pipe. The actual metrics are sent in a separate pipe called **_Telemetry_**. The **_Client_** applications react to the incoming metrics by rendering them for the user.

You can find more information about the interprocess communication and the protocols in this file: _**Overviews/Protocols/README.md**_

param(
    [Parameter(Mandatory)]
    [ValidateSet(
        "serverOnly",
        "full",
        "console",
        "desktop",
        "desktopNoConsole"
    )]
    [string]$Target,

    [switch]$Release,
    [switch]$Run
)

$buildArgs = @(
    if ($Release) {
        "--release"
    } else {
    }
)

function Log {
    param (
        [string]$type,
        [string]$message
    )

    Write-Host "[ProcessMonitor][Build]: ${type}: $message."
}

function LogInfo { param ([string]$message) Log "info" $message }
function LogError { param ([string]$message) Log "error" $message }
function LogWarning { param ([string]$message) Log "warning" $message }

function BuildProject {
    param (
        [string]$ProjectFolder,
        [string]$TargetName
    )

    LogInfo "building the '$TargetName' target"

    dotnet build @buildArgs $ProjectFolder

    if ($LASTEXITCODE -ne 0) {
        LogError "failed to build the '$TargetName' target"
        exit 1
    }
}

function RunProject {
    if (-not $Run) { return }

    switch ($Target) {
        "serverOnly"       { .\ProcessMonitor.Backend\bin\Debug\net9.0\ProcessMonitor.Backend.exe }
        "full"             { .\ProcessMonitor.CLI\bin\Debug\net9.0\ProcessMonitor.CLI.exe --path '.\ProcessMonitor.CLI\bin\Debug\net9.0\ProcessMonitor.Backend.exe' }
        "console"          { .\ProcessMonitor.CLI\bin\Debug\net9.0\ProcessMonitor.CLI.exe --path '.\ProcessMonitor.CLI\bin\Debug\net9.0\ProcessMonitor.Backend.exe' }
        "desktop"          { .\ProcessMonitor.WPF\bin\Debug\net9.0-windows\ProcessMonitor.WPF.exe --path '.\ProcessMonitor.CLI\bin\Debug\net9.0\ProcessMonitor.Backend.exe' }
        "desktopNoConsole" { LogWarning "running the desktopNoConsole target is not implemented yet" }

        default {
            LogError "could not run a target called '$Target'"
            exit 1
        }
    }
}

# todo: add the desktop target once its draft is completed 
function BuildFull {
    BuildProject '.\ProcessMonitor.Backend' 'full (server)'
    BuildProject '.\ProcessMonitor.CLI' 'full (client)'
}

function BuildConsole {
    BuildProject '.\ProcessMonitor.Backend' 'console (server)'
    BuildProject '.\ProcessMonitor.CLI' 'console (client)'
}

# todo: replace the experimental version with a stable one when it's ready
function BuildDesktop {
    BuildProject '.\ProcessMonitor.Backend' 'desktop (server)'
    BuildProject '.\ProcessMonitor.CLI' 'desktop (console-client)'
    BuildProject '.\ProcessMonitor.WPF' 'desktop (wpf-client)'
}

function BuildDesktopNoConsole {
    BuildProject '.\ProcessMonitor.Backend' 'desktopNoConsole (server)'
    BuildProject '.\ProcessMonitor.WPF' 'desktopNoConsole (wpf-client)'
}

function BuildServerOnly {
    BuildProject '.\ProcessMonitor.Backend' 'serverOnly'
}

switch ($Target) {
    "serverOnly"       { BuildServerOnly }
    "full"             { BuildFull }
    "console"          { BuildConsole }
    "desktop"          { BuildDesktop }
    "desktopNoConsole" { BuildDesktopNoConsole }

    default {
        LogError "could not find a target called '$TargetName'"
        exit 1
    }
}

LogInfo "successfully built the '$Target' target"

RunProject

exit 0
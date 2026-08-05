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

function BuildProject {
    param (
        [string]$ProjectFolder,
        [string]$TargetName
    )

    Write-Host "[ProcessMonitor][Build]: info: building the '$TargetName' target."

    dotnet build @buildArgs $ProjectFolder

    if ($LASTEXITCODE -ne 0) {
        Write-Error "[ProcessMonitor][Build]: error: failed to build the '$TargetName' target. Abort."
        exit 1
    }
}

function RunProject {
    if (-not $Run) { return }

    switch ($Target) {
        "serverOnly"       { .\ProcessMonitor.Backend\bin\Debug\net9.0\ProcessMonitor.Backend.exe }
        "full"             { .\ProcessMonitor.CLI\bin\Debug\net9.0\ProcessMonitor.CLI.exe --path '.\ProcessMonitor.CLI\bin\Debug\net9.0\ProcessMonitor.Backend.exe' }
        "console"          { .\ProcessMonitor.CLI\bin\Debug\net9.0\ProcessMonitor.CLI.exe --path '.\ProcessMonitor.CLI\bin\Debug\net9.0\ProcessMonitor.Backend.exe' }
        "desktop"          { Write-Host "[ProcessMonitor][Build]: warning: running the desktop target is not implemented yet. Ignore." }
        "desktopNoConsole" { Write-Host "[ProcessMonitor][Build]: warning: running the desktopNoConsole target is not implemented yet. Ignore." }

        default {
            Write-Error "[ProcessMonitor][Build]: error: could not run a target called '$Target'. Abort."
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
    BuildProject '.\ProcessMonitor.WPF_Experimental' 'desktop (wpf-client)'
}

function BuildDesktopNoConsole {
    BuildProject '.\ProcessMonitor.Backend' 'desktopNoConsole (server)'
    BuildProject '.\ProcessMonitor.WPF_Experimental' 'desktopNoConsole (wpf-client)'
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
        Write-Error "[ProcessMonitor][Build]: error: could not find a target called '$TargetName'. Abort."
        exit 1
    }
}

Write-Host "[ProcessMonitor][Build]: info: successfully built the '$Target' target."

RunProject

exit 0
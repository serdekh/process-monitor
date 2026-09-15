# TODO: define publishing including the console app
param(
    [string]$Target,
    [switch]$Clean
)

enum TargetKind {
    DesktopNoConsole
}

function GetTargetsList {
    $targetOptions = [System.Enum]::GetNames([TargetKind])
    $formattedList = "`n`t$($targetOptions -join "`n`t")"
    return $formattedList
}

$script:cleanFlag = $Clean
$script:targetFlagValue = $Target 
$script:targetKind = [TargetKind]::DesktopNoConsole
$script:inputParams = $PSBoundParameters
$script:releaseFolder = "Release"
$script:processMonitorExecutable = "undefined"

# TODO: Unify the project build system to avoid reduplication
# such helpers like logging and error handling by defining 
# a module system for both building and publishing
function Log {
    param (
        [string]$type,
        [string]$message
    )

    Write-Host "[ProcessMonitor][Publishing]: ${type}: $message."
}

function LogInfo { param ([string]$message) Log "info" $message }
function LogNote { param ([string]$message) Log "note" $message }
function LogError { param ([string]$message) Log "error" $message }
function LogWarning { param ([string]$message) Log "warning" $message }

function PublishProject {
    param (
        [string]$projectName,
        [string]$releaseFolderPath
    )

    New-Item -Path ".\$script:releaseFolder" -ItemType Directory -Force

    dotnet publish $projectName\$projectName.csproj `
        -c Release `
        -r win-x64 `
        --self-contained true `
        -o $releaseFolderPath

    if ($LASTEXITCODE -ne 0) {
        LogError "failed to publish the $projectName project"
        Remove-Item -Path "$script:releaseFolder\*" -Recurse -Force

        exit 1
    }
}

function PublishDesktopNoConsole {
    PublishProject .\ProcessMonitor.WPF .\$script:releaseFolder\ProcessMonitor.WPF
    PublishProject .\ProcessMonitor.Backend .\$script:releaseFolder\ProcessMonitor.Backend

    $script:processMonitorExecutable = ".\$script:releaseFolder\ProcessMonitor.WPF\ProcessMonitor.WPF.exe"
}

function PublishApplication {
    switch ($script:targetKind) {
        ([TargetKind]::DesktopNoConsole) {
            LogInfo "publishing desktopNoConsole target"
            PublishDesktopNoConsole
        }
        Default {
            LogError "unknown publishing target: '$script:targetFlagValue'"
            exit 1
        }
    }

    TryCreateShortcut
    LogInfo "successfully published '$($script:targetKind.ToString())' target"
}

function TryParseClean {
    if ($script:cleanFlag) {
        if ($script:inputParams.Count -gt 1) {
            LogWarning "'Clean' flag was passed. Other flags will be ignored"
        }

        if (Test-Path -Path $script:releaseFolder -PathType Container) {
            Remove-Item -Path "$script:releaseFolder" -Recurse -Force
        }

        exit 0
    }
}

function TryParseTarget {
    if ([string]::IsNullOrWhiteSpace($script:targetFlagValue)) {
        LogError "no publishing target specified"
        LogNote "use 'Target' flag to specify what application to publish"
        LogNote "Available options:$(GetTargetsList)"
        exit 1
    }

    switch ($script:targetFlagValue) {
        "desktopNoConsole" { $script:targetKind = [TargetKind]::DesktopNoConsole }
        Default {
            LogError "unknown publishing target: '$script:targetFlagValue'"
            LogNote "Available options:$(GetTargetsList)"
            exit 1
        }
    }
}

function TryCreateShortcut {
    try {
        $WshShell = New-Object -ComObject WScript.Shell
        
        $RawReleasePath = "$PSScriptRoot\$($script:releaseFolder)"
        $RawTargetPath  = "$PSScriptRoot\$($script:processMonitorExecutable)"
        
        $AbsoluteReleaseFolder = [System.IO.Path]::GetFullPath($RawReleasePath)
        $ShortcutPath          = Join-Path $AbsoluteReleaseFolder "ProcessMonitor.lnk"
        $AbsoluteTargetPath    = [System.IO.Path]::GetFullPath($RawTargetPath)

        if (-not (Test-Path $AbsoluteReleaseFolder)) {
            New-Item -ItemType Directory -Path $AbsoluteReleaseFolder -Force | Out-Null
        }

        if (Test-Path $ShortcutPath) {
            Remove-Item $ShortcutPath -Force -ErrorAction SilentlyContinue
        }

        $Shortcut = $WshShell.CreateShortcut($ShortcutPath)
        $Shortcut.TargetPath = $AbsoluteTargetPath
        $Shortcut.WorkingDirectory = [System.IO.Path]::GetDirectoryName($AbsoluteTargetPath)
        $Shortcut.Save()
    }
    catch {
        LogError "could not create a shortcut. Reason: $($_.Exception.Message)"
        exit 1
    }
}

function TryParseFlags {
    TryParseClean
    TryParseTarget
}

function Main {
    TryParseFlags
    PublishApplication 
}

Main
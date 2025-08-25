param(
    [switch]$Standalone,
    [switch]$Sharded,
    [switch]$Start,
    [switch]$Stop
)

if ($Standalone -and $Sharded) {
    Write-Error "You cannot use -Standalone and -Sharded together."
    exit 1
}

if (-not $Standalone -and -not $Sharded) {
    Write-Error "You must specify either -Standalone or -Sharded."
    exit 1
}

if ($Start) {
    if ($Standalone) {
        Write-Host "Starting in Standalone mode..."
        docker compose -f "$PSScriptRoot/compose/mare-standalone.yml" -p standalone up -d
    }
    elseif ($Sharded) {
        Write-Host "Starting in Sharded mode..."
        docker compose -f "$PSScriptRoot/compose/mare-sharded.yml" -p sharded up -d
    }
}
elseif ($Stop) {
    if ($Standalone) {
        Write-Host "Stopping Standalone service..."
        docker compose -f "$PSScriptRoot/compose/mare-standalone.yml" -p standalone stop
    }
    elseif ($Sharded) {
        Write-Host "Stopping Sharded service..."
        docker compose -f "$PSScriptRoot/compose/mare-sharded.yml" -p sharded stop
    }
}
else {
    # neither -Start nor -Stop supplied
    if ($Standalone) {
        Write-Host "Running other Standalone action..."
        docker compose -f "$PSScriptRoot/compose/mare-standalone.yml" -p standalone up
    }
    elseif ($Sharded) {
        Write-Host "Running other Sharded action..."
        docker compose -f "$PSScriptRoot/compose/mare-sharded.yml" -p sharded up
    }
}
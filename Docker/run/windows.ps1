param(
    # Type
    [switch]$Standalone,
    [switch]$Sharded,
    # Mode
    [switch]$Start,
    [switch]$Stop
)

function Import-DotEnv {
    param(
        [string]$Path = ".env"
    )

    if (-not (Test-Path $Path)) {
        Write-Warning "Env file not found: $Path"
        return
    }

    Get-Content $Path | ForEach-Object {
        if ($_ -match '^\s*$' -or $_.TrimStart().StartsWith('#')) {
            return # skip empty or comment lines
        }

        $idx = $_.IndexOf('=')
        if ($idx -gt -1) {
            $name  = $_.Substring(0, $idx).Trim()
            $value = $_.Substring($idx + 1).Trim()

            # Remove wrapping quotes if present
            if (($value.StartsWith('"') -and $value.EndsWith('"')) -or
                ($value.StartsWith("'") -and $value.EndsWith("'"))) {
                $value = $value.Substring(1, $value.Length - 2)
            }

            Set-Content "env:\$name" $value
            Write-Host "+ $name"
        }
    }
}

Import-DotEnv "./compose/.env.local"

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
        docker compose -f "$PSScriptRoot/compose/sinus-standalone.yml" -p standalone up -d
    }
    elseif ($Sharded) {
        Write-Host "Starting in Sharded mode..."
        docker compose -f "$PSScriptRoot/compose/sinus-sharded.yml" -p sharded up -d
    }
}
elseif ($Stop) {
    if ($Standalone) {
        Write-Host "Stopping Standalone service..."
        docker compose -f "$PSScriptRoot/compose/sinus-standalone.yml" -p standalone stop
    }
    elseif ($Sharded) {
        Write-Host "Stopping Sharded service..."
        docker compose -f "$PSScriptRoot/compose/sinus-sharded.yml" -p sharded stop
    }
}
else {
    # neither -Start nor -Stop supplied
    if ($Standalone) {
        Write-Host "Running other Standalone action..."
        docker compose -f "$PSScriptRoot/compose/sinus-standalone.yml" -p standalone up
    }
    elseif ($Sharded) {
        Write-Host "Running other Sharded action..."
        docker compose -f "$PSScriptRoot/compose/sinus-sharded.yml" -p sharded up
    }
}
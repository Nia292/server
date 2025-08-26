#!/usr/bin/env bash

STANDALONE=false
SHARDED=false
START=false
STOP=false

for arg in "$@"; do
  case $arg in
    -standalone) STANDALONE=true ;;
    -sharded)    SHARDED=true ;;
    -start)      START=true ;;
    -stop)       STOP=true ;;
    *) echo "Unknown option: $arg" && exit 1 ;;
  esac
done

if $STANDALONE && $SHARDED; then
  echo "❌ You cannot use -standalone and -sharded together."
  exit 1
fi

if ! $STANDALONE && ! $SHARDED; then
  echo "❌ You must specify either -standalone or -sharded."
  exit 1
fi

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

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

if $START; then
  if $STANDALONE; then
    echo "🚀 Starting in Standalone mode..."
    docker compose -f "$SCRIPT_DIR/compose/sinus-standalone.yml" -p standalone up -d
  elif $SHARDED; then
    echo "🚀 Starting in Sharded mode..."
    docker compose -f "$SCRIPT_DIR/compose/sinus-sharded.yml" -p sharded up -d
  fi
elif $STOP; then
  if $STANDALONE; then
    echo "🛑 Stopping Standalone service..."
    docker compose -f "$SCRIPT_DIR/compose/sinus-standalone.yml" -p standalone stop
  elif $SHARDED; then
    echo "🛑 Stopping Sharded service..."
    docker compose -f "$SCRIPT_DIR/compose/sinus-sharded.yml" -p sharded stop
  fi
else
  # neither -start nor -stop supplied
  if $STANDALONE; then
    echo "⚡ Running other Standalone action..."
    docker compose -f "$SCRIPT_DIR/compose/sinus-standalone.yml" -p standalone up
  elif $SHARDED; then
    echo "⚡ Running other Sharded action..."
    docker compose -f "$SCRIPT_DIR/compose/sinus-sharded.yml" -p sharded up
  fi
fi
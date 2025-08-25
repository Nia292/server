#!/usr/bin/env bash

# Where
GIT=false
LOCAL=false
# What
ALL=false
AUTH_SERVICE=false
SERVER=false
SERVICES=false
STATIC_FILES_SERVER=false

# Parse arguments
for arg in "$@"; do
  case $arg in
    -git)               GIT=true ;;
    -local)             LOCAL=true ;;
    -all)               ALL=true ;;
    -authservice)       AUTH_SERVICE=true ;;
    -server)            SERVER=true ;;
    -services)          SERVICES=true ;;
    -staticfilesserver) STATIC_FILES_SERVER=true ;;
    *) echo "Unknown option: $arg" && exit 1 ;;
  esac
done

if $GIT && $LOCAL; then
  echo "❌ You cannot use -git and -local together."
  exit 1
fi

if ! $GIT && ! $LOCAL; then
  echo "❌ You must specify either -git or -local."
  exit 1
fi

if $ALL && ($AUTH_SERVICE || $SERVER || $SERVICES || $STATIC_FILES_SERVER); then
  echo "❌ You cannot use -all and individual flags."
  exit 1
fi

if ! $ALL && ! $AUTH_SERVICE && ! $SERVER && ! $SERVICES && ! $STATIC_FILES_SERVER; then
  echo "❌ You must specify at least one Service using -all or -authservice, -server, -services, -staticfilesserver."
  exit 1
fi

if $GIT; then
  SUFFIX="-git"
else
  SUFFIX=""
fi

# Associative array for service mappings
declare -A MAPPED_SERVICES=(
  ["AuthService"]="sinus-synchronous-authservice"
  ["Server"]="sinus-synchronous-server"
  ["Services"]="sinus-synchronous-services"
  ["StaticFilesServer"]="sinus-synchronous-staticfilesserver"
)

build_service() {
  local name="$1"
  local tag="$2"
  
  if [[ -z "$name" || -z "$tag" ]]; then
    echo "❌ Name and Tag cannot be empty"
    return 1
  fi
  
  local docker_tag="syrilai/$tag:latest"
  local original_dir=$(pwd)
  
  if $LOCAL; then
    cd "../.."
    dockerfile="./Docker/build/Dockerfile-SinusSynchronous$name$SUFFIX"
  else
    dockerfile="./Dockerfile-SinusSynchronous$name$SUFFIX"
  fi
  
  echo "🔨 Building '$docker_tag' from '$dockerfile'..."
  
  docker build \
    -t "$docker_tag" \
    . \
    -f "$dockerfile" \
    --no-cache \
    --pull \
    --force-rm
  
  if $LOCAL; then
    cd "$original_dir"
  fi
}

if $ALL; then
  for service in "${!MAPPED_SERVICES[@]}"; do
    build_service "$service" "${MAPPED_SERVICES[$service]}"
  done
else
  if $AUTH_SERVICE; then
    build_service "AuthService" "${MAPPED_SERVICES[AuthService]}"
  fi
  if $SERVER; then
    build_service "Server" "${MAPPED_SERVICES[Server]}"
  fi
  if $SERVICES; then
    build_service "Services" "${MAPPED_SERVICES[Services]}"
  fi
  if $STATIC_FILES_SERVER; then
    build_service "StaticFilesServer" "${MAPPED_SERVICES[StaticFilesServer]}"
  fi
fi
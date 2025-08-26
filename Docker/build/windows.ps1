param(
  # Where
  [switch]$Git,
  [switch]$Local,
  # What
  [switch]$All,
  [switch]$AuthService,
  [switch]$Server,
  [switch]$Services,
  [switch]$StaticFilesServer
)

if ($Git -and $Local) {
  Write-Error "You cannot use -Git and -Local together."
  exit 1
}

if (-not $Git -and -not $Local) {
  Write-Error "You must specify either -Git or -Local."
  exit 1
}

$others = @($AuthService, $Server, $Services, $StaticFilesServer) -contains $true

if ($All -and $others) {
  Write-Error "You cannot use -All and individual flags."
  exit 1
}

if (-not $All -and -not $others) {
  Write-Error "You must specify at least one Service using -All or -AuthService, -Server, -Services, -StaticFilesServer."
  exit 1
}

if ($Git) {
  $Suffix = "-git"
} else {
  $Suffix = ""
}

git submodule update --init --remote --recursive

$MappedServices = @{
  'AuthService'      = "sinus-synchronous-authservice"
  'Server'           = "sinus-synchronous-server"
  'Services'         = "sinus-synchronous-services"
  'StaticFilesServer' = "sinus-synchronous-staticfilesserver"
}

function Build-Service {
  param(
    [Parameter(Mandatory=$true)]
    [ValidateNotNullOrEmpty()]
    [string]$Name,
    [Parameter(Mandatory=$true)]
    [ValidateNotNullOrEmpty()]
    [string]$Tag
  )
  $DockerTag = "syrilai/$($Tag):latest"
  
  if ($Local) {
    Push-Location "..\.."
    $Dockerfile = ".\Docker\build\Dockerfile-SinusSynchronous$Name$Suffix"
  } else {
    $Dockerfile = ".\Dockerfile-SinusSynchronous$Name$Suffix"
  }

  Write-Host "🔨 Building '$DockerTag' from '$Dockerfile'..."
  $DockerArgs = @(
    "build",
    "-t", $DockerTag
    "."
    "-f", $Dockerfile
    "--no-cache"
    "--pull"
    "--force-rm"
  )

  & docker @DockerArgs

  if ($Local) {
    Push-Location ".\Docker\build"
  }
}

if ($All) {
  foreach ($entry in $MappedServices.GetEnumerator()) {
    Build-Service -Name $entry.Key -Tag $entry.Value
  }
} else {
  if ($AuthService)      { Build-Service -Name "AuthService"      -Tag $MappedServices['AuthService'] }
  if ($Server)           { Build-Service -Name "Server"           -Tag $MappedServices['Server'] }
  if ($Services)         { Build-Service -Name "Services"         -Tag $MappedServices['Services'] }
  if ($StaticFilesServer) { Build-Service -Name "StaticFilesServer" -Tag $MappedServices['StaticFilesServer'] }
}
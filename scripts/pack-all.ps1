$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$pkgs = @(
    "src/Novolis.Physics.Numerics/Novolis.Physics.Numerics.csproj",
    "src/Novolis.Physics.Abstractions/Novolis.Physics.Abstractions.csproj",
    "src/Novolis.Physics.Motion/Novolis.Physics.Motion.csproj",
    "src/Novolis.Physics.Gravity/Novolis.Physics.Gravity.csproj",
    "src/Novolis.Physics.Aerodynamics/Novolis.Physics.Aerodynamics.csproj",
    "src/Novolis.Physics.Collision.Simple/Novolis.Physics.Collision.Simple.csproj",
    "src/Novolis.Physics.Ballistics/Novolis.Physics.Ballistics.csproj",
    "src/Novolis.Physics.Orbits/Novolis.Physics.Orbits.csproj",
    "src/Novolis.Physics/Novolis.Physics.csproj"
)
Push-Location $root
New-Item -ItemType Directory -Path artifacts -Force | Out-Null
foreach ($p in $pkgs) { dotnet pack $p -c Release -o artifacts }
Pop-Location
Get-ChildItem (Join-Path $root artifacts) -Filter Novolis.Physics*.nupkg | Select-Object Name

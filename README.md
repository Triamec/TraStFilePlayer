# TraStFilePlayer

TraStFilePlayer is a .NET MAUI (Windows) application for loading and playing Trajectory Streaming CSV files on Triamec motion systems.

## Prerequisites

- Windows 10/11 x64
- .NET 10 SDK
- Access to supported Triamec hardware/runtime environment

## Dependencies

The project consumes NuGet packages from [nuget.org](https://www.nuget.org):

In addition, the project consumes local NuGet packages that are released in their Beta versions. These NuGet packages are located in `local-feed` and are automatically added via `NuGet.config`.

- `Triamec.Tam.Motion.TrajectoryStreaming` (`1.1.0-beta.1`)

`Triamec.Tam.Motion` is resolved transitively by design through the package dependency graph.

## Restore and build

```powershell
dotnet restore .\TraStFilePlayer.csproj
dotnet build .\TraStFilePlayer.csproj -c Release
```

## Run

```powershell
dotnet run --project .\TraStFilePlayer.csproj -c Debug
```

## Target

- `net10.0-windows10.0.19041.0`
- `win-x64`


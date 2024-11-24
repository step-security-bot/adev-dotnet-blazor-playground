# BlazorPlayground app

## Prerequisites

- .Net 9 SDK
- Docker
- Node 22+
- Corepack

## Build

```shell
pnpm install
dotnet workload restore
dotnet build src/Web -c Debug
```

## Run
```shell
dotnet run src/Web -c Debug
```

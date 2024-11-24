FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/aspnet:9.0-noble-chiseled@sha256:d24db805712b6bc67f4f18a50d3659fc9f8014dde1b36494c98f0159f6d1542f AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM --platform=$BUILDPLATFORM node:22-alpine@sha256:b64ced2e7cd0a4816699fe308ce6e8a08ccba463c757c00c14cd372e3d2c763e AS tailwindcss
RUN corepack enable
WORKDIR /src
COPY ["package.json", "pnpm-lock.yaml", "./"]
RUN pnpm install --frozen-lockfile
ENV NODE_ENV=production
COPY . .
RUN pnpm run tailwind:build:minify

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0@sha256:7d24e90a392e88eb56093e4eb325ff883ad609382a55d42f17fd557b997022ca AS build
ARG BUILD_CONFIGURATION=Release
ARG TARGETARCH
RUN apt update; \
    apt install -y python3; \
    dotnet workload install wasm-tools;
WORKDIR /src
COPY ["Directory.Packages.props", "Directory.Build.props", "nuget.config", "./"]
COPY ["src/Web/Web.csproj", "src/Web/"]
COPY ["src/Web.Client/Web.Client.csproj", "src/Web.Client/"]
COPY ["src/Shared/Shared.csproj", "src/Shared/"]
RUN dotnet restore "src/Web/Web.csproj" -a $TARGETARCH
COPY . .
COPY --from=tailwindcss /src/src/Web/wwwroot/styles/app.css ./src/Web/wwwroot/styles/app.css
WORKDIR "/src/src/Web"
RUN dotnet build "Web.csproj" -c $BUILD_CONFIGURATION --no-restore -o /app/build -a $TARGETARCH

FROM build AS publish
ARG TARGETARCH
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Web.csproj" -c $BUILD_CONFIGURATION --no-restore -o /app/publish -a $TARGETARCH

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Web.dll"]

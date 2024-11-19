FROM mcr.microsoft.com/dotnet/aspnet:9.0-noble-chiseled AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM node:22-alpine AS tailwindcss
RUN corepack enable
WORKDIR /src
COPY ["package.json", "pnpm-lock.yaml", "./"]
RUN pnpm install --frozen-lockfile
ENV NODE_ENV=production
COPY . .
RUN pnpm run tailwind:build:minify

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Packages.props", "Directory.Build.props", "nuget.config", "./"]
COPY ["src/Web/Web.csproj", "src/Web/"]
COPY ["src/Web.Client/Web.Client.csproj", "src/Web.Client/"]
RUN dotnet restore "src/Web/Web.csproj"
COPY . .
COPY --from=tailwindcss /src/src/Web/wwwroot/styles/app.css ./src/Web/wwwroot/styles/app.css
WORKDIR "/src/src/Web"
RUN dotnet build "Web.csproj" -c $BUILD_CONFIGURATION --no-restore -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Web.dll"]

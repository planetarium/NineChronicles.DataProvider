FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
WORKDIR /app
ARG COMMIT

# Copy csproj and restore as distinct layers
COPY ./NineChronicles.Headless/Lib9c/Lib9c/Lib9c.csproj ./Lib9c/
COPY ./NineChronicles.Headless/Libplanet.Headless/Libplanet.Headless.csproj ./Libplanet.Headless/
COPY ./NineChronicles.Headless/NineChronicles.RPC.Shared/NineChronicles.RPC.Shared/NineChronicles.RPC.Shared.csproj ./NineChronicles.RPC.Shared/
COPY ./NineChronicles.Headless/NineChronicles.Headless/NineChronicles.Headless.csproj ./NineChronicles.Headless/
COPY ./NineChronicles.Headless/NineChronicles.Headless.Executable/NineChronicles.Headless.Executable.csproj ./NineChronicles.Headless.Executable/
COPY ./NineChronicles.DataProvider/NineChronicles.DataProvider.csproj ./NineChronicles.DataProvider/
COPY ./NineChronicles.DataProvider.Executable/NineChronicles.DataProvider.Executable.csproj ./NineChronicles.DataProvider.Executable/
COPY ./NineChronicles.DataProvider.Tools/NineChronicles.DataProvider.Tools.csproj ./NineChronicles.DataProvider.Tools/
RUN dotnet restore Lib9c
RUN dotnet restore Libplanet.Headless
RUN dotnet restore NineChronicles.RPC.Shared
RUN dotnet restore NineChronicles.Headless
RUN dotnet restore NineChronicles.Headless.Executable
RUN dotnet restore NineChronicles.DataProvider
RUN dotnet restore NineChronicles.DataProvider.Executable
RUN dotnet restore NineChronicles.DataProvider.Tools

# Copy everything else and build
COPY . ./
RUN dotnet publish NineChronicles.DataProvider.Executable/NineChronicles.DataProvider.Executable.csproj \
    -c Release \
    -r linux-x64 \
    -o out \
    --self-contained \
    --version-suffix $COMMIT

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
WORKDIR /app
RUN apt-get update && apt-get install -y libc6-dev jq
COPY --from=build-env /app/out .

VOLUME /data

ENTRYPOINT ["dotnet", "NineChronicles.DataProvider.Tools.dll"]

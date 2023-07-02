FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
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
RUN dotnet publish NineChronicles.DataProvider.Tools/NineChronicles.DataProvider.Tools.csproj \
    -c Release \
    -r linux-x64 \
    -o out \
    --self-contained \
    --version-suffix $COMMIT

RUN dotnet publish NineChronicles.Headless/NineChronicles.Headless.Executable/NineChronicles.Headless.Executable.csproj \
    -c Release \
    -r linux-x64 \
    -o out2 \
    --self-contained \
    --version-suffix $COMMIT

# Build runtime image
FROM mcr.microsoft.com/dotnet/sdk:6.0
WORKDIR /app
RUN dotnet tool install --global dotnet-ef
ENV PATH="${PATH}:/${HOME}/.dotnet/tools"
COPY --from=build-env /app/out .
COPY --from=build-env /app/out2 NineChronicles.Headless.Executable
COPY ./NineChronicles.DataProvider ./NineChronicles.DataProvider/NineChronicles.DataProvider/
COPY ./NineChronicles.DataProvider.Executable ./NineChronicles.DataProvider/NineChronicles.DataProvider.Executable/
COPY ./NineChronicles.Headless ./NineChronicles.DataProvider/NineChronicles.Headless/

RUN apt-get update \
    && apt-get install -y --allow-unauthenticated \
        libc6-dev jq curl \
     && rm -rf /var/lib/apt/lists/*

VOLUME /data

ENTRYPOINT ["dotnet", "NineChronicles.DataProvider.Tools.dll"]

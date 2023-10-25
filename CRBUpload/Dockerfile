FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["CRBUpload/CRBUpload.csproj", "CRBUpload/"]
RUN dotnet restore "CRBUpload/CRBUpload.csproj"
COPY . .
WORKDIR "/src/CRBUpload"
RUN dotnet build "CRBUpload.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CRBUpload.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CRBUpload.dll"]
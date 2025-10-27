FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/MeetingManagementSystem.Web/MeetingManagementSystem.Web.csproj", "src/MeetingManagementSystem.Web/"]
COPY ["src/MeetingManagementSystem.Core/MeetingManagementSystem.Core.csproj", "src/MeetingManagementSystem.Core/"]
COPY ["src/MeetingManagementSystem.Infrastructure/MeetingManagementSystem.Infrastructure.csproj", "src/MeetingManagementSystem.Infrastructure/"]
RUN dotnet restore "src/MeetingManagementSystem.Web/MeetingManagementSystem.Web.csproj"
COPY . .
WORKDIR "/src/src/MeetingManagementSystem.Web"
RUN dotnet build "MeetingManagementSystem.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MeetingManagementSystem.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create directories for logs and uploads
RUN mkdir -p /app/logs /app/uploads

ENTRYPOINT ["dotnet", "MeetingManagementSystem.Web.dll"]
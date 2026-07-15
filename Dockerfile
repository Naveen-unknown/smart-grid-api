# Use the official .NET SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["SmartGridAPI/SmartGridAPI.csproj", "SmartGridAPI/"]
RUN dotnet restore "SmartGridAPI/SmartGridAPI.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/SmartGridAPI"
RUN dotnet build "SmartGridAPI.csproj" -c Release -o /app/build

# Publish the app
FROM build AS publish
RUN dotnet publish "SmartGridAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Expose port 80/8080 depending on the environment
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SmartGridAPI.dll"]

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file
COPY src/TravelBookingSystem/TravelBookingSystem.csproj ./src/TravelBookingSystem/

# Restore dependencies
RUN dotnet restore src/TravelBookingSystem/TravelBookingSystem.csproj

# Copy the rest of the source code
COPY src/TravelBookingSystem/ ./src/TravelBookingSystem/

# Build the application
WORKDIR /src/src/TravelBookingSystem
RUN dotnet build -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Create a non-root user
RUN useradd -m -u 1000 appuser && chown -R appuser /app
USER appuser

# Copy published files
COPY --from=publish /app/publish .

# Expose port
EXPOSE 8080
EXPOSE 8081

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "TravelBookingSystem.dll"]

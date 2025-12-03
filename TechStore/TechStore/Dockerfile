# Dockerfile (Place at Solution Root)

# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 1. COPY the project files needed for restore
# We copy all project files from their folders into corresponding folders inside /src
COPY ["TechStore/TechStore.csproj", "TechStore/"]
COPY ["Application/Application.csproj", "Application/"]
COPY ["Domain/Domain.csproj", "Domain/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]

# 2. RUN dotnet restore
# Restore dependencies using the main project file
RUN dotnet restore "TechStore/TechStore.csproj" 

# 3. Copy the rest of the source code
COPY . . 

# 4. Build and Publish the main API project
# The .NET SDK version is 9.0 based on your file listing
RUN dotnet publish "TechStore/TechStore.csproj" -c Release -o /app/publish

# Stage 2: Create the final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
# Copy the published application files
COPY --from=build /app/publish .

# Internal port used by ASP.NET Core
EXPOSE 8080

# Use the name of your built DLL
ENTRYPOINT ["dotnet", "TechStore.dll"]
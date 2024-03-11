# Use the .NET SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env
WORKDIR /app

# Copy the .csproj file to restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy the entire project and publish it
COPY . ./
RUN dotnet publish API.csproj -c Release -o out

# Use the .NET runtime image as the base image
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
WORKDIR /app

# Copy the published application from the "out" folder
COPY --from=build-env /app/out .

# Expose the port used by your application
EXPOSE 80
EXPOSE 443

# Define the entry point to run your application
ENTRYPOINT ["dotnet", "API.dll"] 
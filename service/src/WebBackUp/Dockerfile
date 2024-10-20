FROM registry.access.redhat.com/ubi8/dotnet-80 AS build
EXPOSE 8080

# Set the working directory inside the container
WORKDIR /app

# Copy the project file and restore the dependencies
COPY *.csproj ./
RUN dotnet restore --verbosity detailed

# Copy the rest of the application source code
COPY . ./

# Build the project
RUN dotnet publish -c Release -o /out

# Stage 2: Create the final image
FROM registry.access.redhat.com/ubi8/dotnet-80 AS base
WORKDIR /app

# Copy the published output from the build stage
COPY --from=build /out .
ENTRYPOINT ["dotnet", "WebBackUp.dll"]
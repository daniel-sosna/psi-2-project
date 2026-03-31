# ---------- BUILD STAGE ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY KNOTS/KNOTS.csproj KNOTS/
RUN dotnet restore KNOTS/KNOTS.csproj

# Copy the rest of the source code
COPY KNOTS/ KNOTS/

# Publish the web app
WORKDIR /src/KNOTS
RUN dotnet publish -c Release -o /app/publish


# ---------- RUNTIME STAGE ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Copy published output
COPY --from=build /app/publish .

# Expose default ASP.NET port
EXPOSE 8080

# Use environment variable to force Production
ENV ASPNETCORE_ENVIRONMENT=Production

# Run the app
ENTRYPOINT ["dotnet", "KNOTS.dll"]

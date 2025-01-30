#!/bin/bash

# Wait for the database file to be accessible
until [ -w "/app/db" ]; do
    echo "Waiting for database directory to be writable..."
    sleep 1
done

# Install EF Core tools
dotnet tool install --global dotnet-ef
export PATH="$PATH:/root/.dotnet/tools"

# Apply database migrations
dotnet ef database update

# Start the application
exec dotnet GameRecommender.dll 
#!/bin/bash

# Wait for the database directory to be writable
until [ -w "/app/db" ]; do
    echo "Waiting for database directory to be writable..."
    sleep 1
done

# Create the database directory if it doesn't exist
mkdir -p /app/db

# Set proper permissions
chmod 700 /app/db

# Try to update database multiple times (sometimes first attempt fails)
max_attempts=3
attempt=1

while [ $attempt -le $max_attempts ]; do
    echo "Attempt $attempt to apply database migrations..."
    
    # Remove the database file if it exists to ensure clean state
    if [ -f "/app/db/gamerecommender.db" ]; then
        rm /app/db/gamerecommender.db
    fi
    
    # Run migrations with proper context
    cd /src/GameRecommender
    dotnet ef database update --verbose || true
    
    # Check if the migration was successful by trying to access the database
    if sqlite3 /app/db/gamerecommender.db "SELECT name FROM sqlite_master WHERE type='table' AND name='SteamGameDetails';" > /dev/null 2>&1; then
        echo "Database migrations successfully applied!"
        break
    fi
    
    echo "Attempt $attempt failed. Waiting before retry..."
    sleep 2
    attempt=$((attempt + 1))
done

if [ $attempt -gt $max_attempts ]; then
    echo "Failed to apply migrations after $max_attempts attempts"
    exit 1
fi

# Change back to app directory and start the application
cd /app
exec dotnet GameRecommender.dll 
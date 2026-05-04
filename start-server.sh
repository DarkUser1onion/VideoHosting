#!/bin/bash
docker compose up -d
echo "Запуск PostgreSQL..."
until docker compose ps postgres | grep -q "(healthy)"; do
    sleep 1
done
cd Server
dotnet restore
dotnet run --urls "http://localhost:5000"

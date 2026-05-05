#!/bin/bash
docker compose up -d
echo "Ожидание запуска PostgreSQL..."
sleep 3
cd Server
dotnet restore
dotnet run --urls "http://localhost:5000"

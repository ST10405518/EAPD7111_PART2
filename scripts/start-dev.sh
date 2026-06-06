#!/usr/bin/env bash
set -e
echo "Starting GLMS API on http://localhost:8080 ..."
dotnet run --project GLMS.Api/GLMS.Api.csproj --launch-profile GLMS.Api &
API_PID=$!
sleep 8
echo "Starting GLMS MVC ..."
dotnet run --project EAPD7111_PART2.csproj --launch-profile EAPD7111_PART2
kill $API_PID 2>/dev/null || true

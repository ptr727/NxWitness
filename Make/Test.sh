#!/bin/bash

# Stop
./Down.sh

# Update Matrix.json
dotnet run --project ../CreateMatrix/CreateMatrix.csproj -- matrix --update

# Update URL's in Dockerfile's
dotnet run --project ../CreateMatrix/CreateMatrix.csproj -- make

# Update Dockerfile's
make create

# Build docker images
make build

# Start
./Up.sh

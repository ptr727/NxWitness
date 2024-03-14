#!/bin/bash

set -e

# Create Dockerfile from M4 file
function CreateDockerfile {
    rm ../Docker/$1.Dockerfile || true
    m4 $1.m4 >../Docker/$1.Dockerfile
}

# Update Version.json and create MAtrix.json
dotnet run --project ../CreateMatrix -- matrix --update
# Update M4 snippets using Matrix.json versions
dotnet run --project ../CreateMatrix -- make

# Create Dockerfiles from M4 files
CreateDockerfile "DWSpectrum"
CreateDockerfile "DWSpectrum-LSIO"
CreateDockerfile "NxMeta"
CreateDockerfile "NxMeta-LSIO"
CreateDockerfile "NxWitness"
CreateDockerfile "NxWitness-LSIO"

#!/bin/bash

set -e

# Update Version.json and create Matrix.json
dotnet run --project ../CreateMatrix -- matrix --update --version=Version.json --matrix=Matrix.json

# Create Dockerfiles
dotnet run --project ../CreateMatrix -- make --version=Version.json --make=./ --docker=../Docker --label=Beta

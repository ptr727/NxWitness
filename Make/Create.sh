#!/bin/bash

set -e

# Update Version.json and create Matrix.json
dotnet run --project ../CreateMatrix -- matrix --updateversion --versionpath=Version.json --matrixpath=Matrix.json

# Create Dockerfiles
dotnet run --project ../CreateMatrix -- make --versionpath=Version.json --makedirectory=./ --dockerdirectory=../Docker --versionlabel=Beta

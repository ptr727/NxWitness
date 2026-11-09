#!/bin/bash

set -euo pipefail

# Create Dockerfiles
./create.sh

# Build Dockerfiles
./build.sh

# Launch compose stack
./up.sh

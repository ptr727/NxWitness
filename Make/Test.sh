#!/bin/bash

set -euo pipefail

# Create Dockerfiles
./Create.sh

# Build Dockerfiles
./Build.sh

# Launch compose stack
./Up.sh

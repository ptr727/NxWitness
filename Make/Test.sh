#!/bin/bash

set -e

# Create Dockerfile from M4
./Create.sh

# Build Dockerfile
./Build.sh

# Launch compose stack
./Up.sh

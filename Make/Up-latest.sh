#!/bin/bash

set -e

# Launch stack
docker compose --file Test-latest.yml up --detach

# Instructions
./Instructions.sh

echo "Run 'Down-latest.sh' to stop stack"

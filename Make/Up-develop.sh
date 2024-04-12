#!/bin/bash

set -e

# Launch stack
docker compose --file Test-develop.yml up --detach

# Instructions
./Instructions.sh

echo "Run 'Down-develop.sh' to stop stack"

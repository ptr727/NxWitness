#!/bin/bash

set -e

# Launch stack
docker compose --file Test.yml up --detach

# Instructions
./Instructions.sh

echo "Run 'Down.sh' to stop stack"

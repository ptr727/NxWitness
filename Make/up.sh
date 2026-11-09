#!/bin/bash

set -euo pipefail

# Launch stack
docker compose --file Test.yml up --detach

# Instructions
./instructions.sh

echo "Run 'down.sh' to stop stack"

#!/bin/bash

set -euo pipefail

# Launch stack
docker compose --file Test-latest.yml up --detach

# Instructions
./instructions.sh

echo "Run 'down-latest.sh' to stop stack"

#!/bin/bash

set -euo pipefail

# Launch stack
docker compose --file Test-develop.yml up --detach

# Instructions
./instructions.sh

echo "Run 'down-develop.sh' to stop stack"

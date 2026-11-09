#!/bin/bash

set -euo pipefail

# Stop stack and cleanup volumes
docker compose --file Test.yml down --volumes

echo "Run 'clean.sh' to delete images"

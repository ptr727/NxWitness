#!/bin/bash

set -e

# Stop stack and cleanup volumes
docker compose --file Test.yml down --volumes

echo "Run 'Clean.sh' to delete images"
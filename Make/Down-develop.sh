#!/bin/bash

set -euo pipefail

# Stop stack and cleanup volumes
docker compose --file Test-develop.yml down --volumes

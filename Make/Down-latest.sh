#!/bin/bash

set -euo pipefail

# Stop stack and cleanup volumes
docker compose --file Test-latest.yml down --volumes

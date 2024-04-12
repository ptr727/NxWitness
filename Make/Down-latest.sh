#!/bin/bash

set -e

# Stop stack and cleanup volumes
docker compose --file Test-latest.yml down --volumes

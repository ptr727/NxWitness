#!/bin/bash

set -e

# Stop stack and cleanup volumes
docker compose --file Test-develop.yml down --volumes

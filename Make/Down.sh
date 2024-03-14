#!/bin/bash

set -x
set -e

# Stop stack and cleanup volumes
docker compose --file Test.yml down --volumes

#!/bin/bash

set -e

## Dependencies:
# Docker v25+, .NET SDK v8+
# sudo apt update && sudo apt install m4 dotnet-sdk-8.0 docker-compose-plugin docker-buildx-plugin


## Test installing in container:
# docker run -it --rm ubuntu:jammy /bin/bash
# docker run -it --rm lsiobase/ubuntu:jammy /bin/bash
# export DEBIAN_FRONTEND=noninteractive
# apt-get update && apt-get upgrade --yes
# apt-get install --no-install-recommends --yes mc nano strace wget gdb
# wget --no-verbose --output-document=./vms_server.zip https://updates.networkoptix.com/metavms/37996/metavms-server_update-5.1.2.37996-linux_x64.zip
# unzip -d ./download_zip ./vms_server.zip
# cp ./download_zip/metavms-server-5.1.2.37996-linux_x64.deb ./vms_server.deb
# Install:
# apt-get install --no-install-recommends --yes ./vms_server.deb
# Extract DEB package:
# dpkg-deb -R ./vms_server.deb ./vms_server
# Debug install errors:
# dpkg --debug=72200 --install ./vms_server.deb

## Docker cleanup:
# df
# docker image prune --all
# docker system prune --all --force --volumes

## Attach to running image:
# docker exec --interactive --tty [containername] /bin/bash

## Usage:
# Create.sh : Update Version.json and Matrix.json and create Dockerfile's from M4 snippets.
# Build.sh : Build docker images from Dockerfile's.
# Test.sh : Create and build and launch compose Test.yml compose stack.
# Clean.sh : Shutdown compose stack and delete images.


# Build Dockerfile
function BuildDockerfile {
    # Build x64 and ARM64 targets
	docker buildx build --platform linux/amd64,linux/arm64 --tag test_${1,,} --file ../Docker/$1.Dockerfile ../Docker
    # Build and load x64 target
	docker buildx build --platform linux/amd64 --load --tag test_${1,,} --file ../Docker/$1.Dockerfile ../Docker
}

# Create and use multi platfiorm build environment
docker buildx create --name "nxwitness" --use || true

# Build Dockerfiles
BuildDockerfile "DWSpectrum"
BuildDockerfile "DWSpectrum-LSIO"
BuildDockerfile "NxMeta"
BuildDockerfile "NxMeta-LSIO"
BuildDockerfile "NxWitness"
BuildDockerfile "NxWitness-LSIO"

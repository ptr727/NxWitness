#!/bin/bash

set -euo pipefail

## Dependencies:
# sudo apt update && sudo apt upgrade --yes
# sudo add-apt-repository ppa:dotnet/backports --yes && sudo apt update
# sudo apt install m4 dotnet-sdk-9.0 docker-compose docker-buildx --yes
# sudo apt autoremove --yes


## Test installing in container:
# docker run -it --rm ubuntu:noble /bin/bash
# docker run -it --rm lsiobase/ubuntu:noble /bin/bash
# export DEBIAN_FRONTEND=noninteractive
# apt-get update && apt-get upgrade --yes
# apt-get install --no-install-recommends --yes ca-certificates unzip wget mc nano strace gdb
# wget --output-document=./vms_server.zip https://updates.networkoptix.com/metavms/38488/metavms-server_update-6.0.0.38488-linux_x64-beta.zip
# unzip -d ./download_zip ./vms_server.zip
# cp ./download_zip/metavms-server-6.0.0.38488-linux_x64-beta.deb ./vms_server.deb
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
# Create.sh : Update Version.json and Matrix.json and create Dockerfiles.
# Build.sh : Build docker images from Dockerfiles.
# Test.sh : Create and build and launch compose Test.yml compose stack.
# Clean.sh : Shutdown compose stack and delete images.


# Build Dockerfile
function BuildDockerfile {
    # Build x64 and ARM64 targets
	docker buildx build --platform linux/amd64,linux/arm64 --tag test_${1,,} --file ../Docker/$1.Dockerfile ../Docker
    # Build and load x64 target
	docker buildx build --platform linux/amd64 --load --tag test_${1,,} --file ../Docker/$1.Dockerfile ../Docker
}

# Build base Dockerfile
function BuildBaseDockerfile {
    # Build x64 and ARM64 targets
	docker buildx build --platform linux/amd64,linux/arm64 --tag $2 --file ../Docker/$1.Dockerfile ../Docker
    # Build and load x64 target
	docker buildx build --platform linux/amd64 --load --tag $2 --file ../Docker/$1.Dockerfile ../Docker
}

# Create and use multi platform build environment
docker buildx create --name "nxwitness" --use || true

# Build base Dockerfiles
BuildBaseDockerfile "NxBase" "docker.io/ptr727/nx-base:ubuntu-noble"
BuildBaseDockerfile "NxBase-LSIO" "docker.io/ptr727/nx-base-lsio:ubuntu-noble"

# Build Dockerfiles
BuildDockerfile "NxGo"
BuildDockerfile "NxGo-LSIO"
BuildDockerfile "NxMeta"
BuildDockerfile "NxMeta-LSIO"
BuildDockerfile "NxWitness"
BuildDockerfile "NxWitness-LSIO"
BuildDockerfile "DWSpectrum"
BuildDockerfile "DWSpectrum-LSIO"
BuildDockerfile "WisenetWAVE"
BuildDockerfile "WisenetWAVE-LSIO"

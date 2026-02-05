# Base Dockerfile for Nx Witness LSIO images
# Built from lsiobase/ubuntu:noble
FROM lsiobase/ubuntu:noble

# Prevent EULA and confirmation prompts in installers
ARG DEBIAN_FRONTEND=noninteractive

# Common packages used by all product images
# https://github.com/ptr727/NxWitness/issues/282
RUN apt-get update \
    && apt-get upgrade --yes \
    && apt-get install --no-install-recommends --yes \
        ca-certificates \
        gdb \
        libdrm2 \
        unzip \
        wget \
    && apt-get clean \
    && apt-get autoremove --purge \
    && rm -rf /var/lib/apt/lists/*
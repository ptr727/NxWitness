﻿# Dockerfile created by CreateMatrix, do not modify by hand
# Product: DWSpectrum
# Description: DW Spectrum IPVMS
# Company: digitalwatchdog
# Release: digitalwatchdog
# LSIO: False

# https://support.networkoptix.com/hc/en-us/articles/205313168-Nx-Witness-Operating-System-Support
# Latest Ubuntu supported for v6 is Noble
FROM ubuntu:noble

# Labels
ARG LABEL_NAME="DWSpectrum"
ARG LABEL_DESCRIPTION="DW Spectrum IPVMS"
ARG LABEL_VERSION="6.1.0.40638"

# Download URL and version
# Current values are defined by the build pipeline
ARG DOWNLOAD_X64_URL="https://updates.networkoptix.com/digitalwatchdog/40638/dwspectrum-server_update-6.1.0.40638-linux_x64-beta.zip"
ARG DOWNLOAD_ARM64_URL="https://updates.networkoptix.com/digitalwatchdog/40638/dwspectrum-server_update-6.1.0.40638-linux_arm64-beta.zip"
ARG DOWNLOAD_VERSION="6.1.0.40638"

# Used for ${COMPANY_NAME} setting the server user and install directory
ARG RUNTIME_NAME="digitalwatchdog"

# Global builder variables
# https://docs.docker.com/engine/reference/builder/#automatic-platform-args-in-the-global-scope
ARG \
    # Platform of the build result. Eg linux/amd64, linux/arm/v7, windows/amd64
    TARGETPLATFORM \
    # Architecture component of TARGETPLATFORM
    TARGETARCH \
    # Platform of the node performing the build
    BUILDPLATFORM

# The RUN wget command will be cached unless we change the cache tag
# Use the download version for the cache tag
ARG CACHE_DATE=${DOWNLOAD_VERSION}

# Prevent EULA and confirmation prompts in installers
ARG DEBIAN_FRONTEND=noninteractive

# Media server user and directory name
ENV COMPANY_NAME=${RUNTIME_NAME}

# Labels
LABEL name=${LABEL_NAME}-${DOWNLOAD_VERSION} \
    description=${LABEL_DESCRIPTION} \
    version=${LABEL_VERSION} \
    maintainer="Pieter Viljoen <ptr727@users.noreply.github.com>"

# Install required tools and utilities
RUN apt-get update \
    && apt-get upgrade --yes \
    && apt-get install --no-install-recommends --yes \
        ca-certificates \
        unzip \
        wget

# Download the installer file
RUN mkdir -p /temp
COPY download.sh /temp/download.sh
# Set the working directory to /temp
WORKDIR /temp
RUN chmod +x download.sh \
    && ./download.sh

# Install the mediaserver and dependencies
RUN apt-get update \
    && apt-get install --no-install-recommends --yes \
        gdb \
        sudo \
        ./vms_server.deb \
# Cleanup
    && apt-get clean \
    && apt-get autoremove --purge \
    && rm -rf /var/lib/apt/lists/* \
    && rm -rf /temp

# Add the mediaserver ${COMPANY_NAME} user to the sudoers group
# Only allow sudo no password access to the root-tool
RUN echo "${COMPANY_NAME} ALL = NOPASSWD: /opt/${COMPANY_NAME}/mediaserver/bin/root-tool" > /etc/sudoers.d/${COMPANY_NAME}

# Copy the entrypoint.sh launch script
# entrypoint.sh will run the mediaserver and root-tool
COPY entrypoint.sh /opt/entrypoint.sh
RUN chmod +x /opt/entrypoint.sh

# Run the entrypoint as the mediaserver ${COMPANY_NAME} user
# Note that this user exists in the container and does not directly map to a user on the host
USER ${COMPANY_NAME}

# Runs entrypoint.sh on container start
ENTRYPOINT ["/opt/entrypoint.sh"]

# Expose port 7001
EXPOSE 7001

# Link volumes directly, e.g.
# /mnt/config/etc:opt/digitalwatchdog/mediaserver/etc
# /mnt/config/nx_ini:/home/digitalwatchdog/.config/nx_ini
# /mnt/config/var:/opt/digitalwatchdog/mediaserver/var
# /mnt/media:/media
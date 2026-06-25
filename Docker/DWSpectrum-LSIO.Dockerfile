# Dockerfile created by CreateMatrix, do not modify by hand
# Product: DWSpectrum
# Description: DW Spectrum IPVMS
# Company: digitalwatchdog
# Release: digitalwatchdog
# LSIO: True

# https://support.networkoptix.com/hc/en-us/articles/205313168-Nx-Witness-Operating-System-Support
# Latest Ubuntu supported for v6 is Noble
# Base images are built in this repo, see Docker/NxBase*.Dockerfile
FROM docker.io/ptr727/nx-base-lsio:ubuntu-noble

# Labels
ARG LABEL_NAME="DWSpectrum-LSIO"
ARG LABEL_DESCRIPTION="DW Spectrum IPVMS"
ARG LABEL_VERSION="6.1.2.42997"

# Download URL and version
# Current values are defined by the build pipeline
ARG DOWNLOAD_X64_URL="https://updates.networkoptix.com/digitalwatchdog/42997/dwspectrum-server_update-6.1.2.42997-linux_x64.zip"
ARG DOWNLOAD_ARM64_URL="https://updates.networkoptix.com/digitalwatchdog/42997/dwspectrum-server_update-6.1.2.42997-linux_arm64.zip"
ARG DOWNLOAD_VERSION="6.1.2.42997"

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

# Prevent EULA and confirmation prompts in installers
ARG DEBIAN_FRONTEND=noninteractive

# Media server user and directory name
ENV COMPANY_NAME=${RUNTIME_NAME}

# Labels
LABEL name=${LABEL_NAME}-${DOWNLOAD_VERSION} \
    description=${LABEL_DESCRIPTION} \
    version=${LABEL_VERSION} \
    maintainer="Pieter Viljoen <ptr727@users.noreply.github.com>"

# Download the installer file
WORKDIR /temp
COPY download.sh /temp/download.sh
RUN /bin/bash /temp/download.sh "${DOWNLOAD_X64_URL}" "${DOWNLOAD_ARM64_URL}" "${TARGETPLATFORM:-}"

# Rename the LSIO "abc" user and group to ${COMPANY_NAME}
COPY lsio-rename-user.sh /temp/lsio-rename-user.sh
RUN /bin/bash /temp/lsio-rename-user.sh "${COMPANY_NAME}"

# Install the mediaserver
RUN apt-get update \
    && apt-get install --no-install-recommends --yes \
        ./vms_server.deb \
    # Cleanup
    && apt-get clean \
    && apt-get autoremove --purge --yes \
    && rm -rf /var/lib/apt/lists/* \
    && rm -rf /temp

# Set ownership permissions
RUN chown --verbose ${COMPANY_NAME}:${COMPANY_NAME} /opt/${COMPANY_NAME}/mediaserver/bin \
    && chown --verbose ${COMPANY_NAME}:${COMPANY_NAME} /opt/${COMPANY_NAME}/mediaserver/bin/external.dat

# Copy etc init and services files
# https://www.linuxserver.io/blog/how-is-container-formed
COPY s6-overlay /etc/s6-overlay

# Expose port 7001
EXPOSE 7001

# Create mount points
# Config links will be created at runtime, see LSIO/etc/s6-overlay/s6-rc.d/init-nx-relocate/run
# /opt/${COMPANY_NAME}/mediaserver/etc -> /config/etc
# /opt/${COMPANY_NAME}/mediaserver/var -> /config/var
# /root/.config/nx_ini links -> /config/ini
# /config is for configuration
# /media is for recordings
# /backup is for backups
# /analytics is for analytics
VOLUME /config /media /backup /analytics

# Use Ubuntu Bionic
FROM ubuntu:bionic

# Labels
ARG LABEL_NAME="NxMeta"
ARG LABEL_DESCRIPTION="Nx Witness VMS Docker"

# Download URL and version
ARG DOWNLOAD_URL="https://updates.networkoptix.com/default/30917/linux/nxwitness-server-4.0.0.30917-linux64.deb"
ARG DOWNLOAD_VERSION="4.0.0.30917"

# NxWitness (networkoptix) or DWSpectrum (digitalwatchdog) or NxMeta (networkoptix-metavms)
ARG RUNTIME_NAME="networkoptix"

# The RUN wget command will be cached unless we change the cache tag
# Use the download version for the cache tag
ARG CACHE_DATE=${DOWNLOAD_VERSION}

# Prevent EULA and confirmation prompts in installers
ENV DEBIAN_FRONTEND=noninteractive

# Media server user and directory name
ENV COMPANY_NAME=${RUNTIME_NAME}

# Labels
LABEL name=${LABEL_NAME} \
    description=${LABEL_DESCRIPTION} \
    version=${DOWNLOAD_VERSION} \
    download=${DOWNLOAD_URL} \
    maintainer="Pieter Viljoen <ptr727@users.noreply.github.com>"

# Install tools
RUN apt-get update \
    && apt-get install --no-install-recommends --yes \
        mc \
        nano \
        strace \
        wget \
    && apt-get clean \
    && apt-get autoremove --purge \
    && rm -rf /var/lib/apt/lists/*

# Download the DEB installer file
RUN wget -nv -O ./vms_server.deb ${DOWNLOAD_URL}

# Install the mediaserver
# Add missing dependencies (gdb)
# Remove the root tool to prevent it from being used in service mode
RUN apt-get update \
    && apt-get install --no-install-recommends --yes \
        gdb \
        ./vms_server.deb \
    && apt-get clean \
    && apt-get autoremove --purge \
    && rm -rf /opt/${COMPANY_NAME}/mediaserver/bin/root-tool-bin \
    && rm -rf /var/lib/apt/lists/* \
    && rm -rf ./vms_server.deb

# Expose port 7001
EXPOSE 7001

# Create mount points
# Links will be created at runtime in the etc/cont-init.d/50-relocate-files script
# /opt/digitalwatchdog/mediaserver/etc -> /config/etc
# /opt/digitalwatchdog/mediaserver/var -> /config/var
# /opt/digitalwatchdog/mediaserver/var/data -> /media
# /config is for configuration
# /media is for media recording
# /archive is for media backups
VOLUME /config /media /archive


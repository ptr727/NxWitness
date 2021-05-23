# Use Ubuntu Focal Fossa 20.04 LTS
FROM ubuntu:focal


# Labels
ARG LABEL_NAME="DWSpectrum"
ARG LABEL_DESCRIPTION="DW Spectrum IPVMS Docker"

# Download URL and version
# Current values are defined by the build pipeline
ARG DOWNLOAD_URL="https://updates.networkoptix.com/digitalwatchdog/32842/linux/dwspectrum-server-4.2.0.32842-linux64.deb"
ARG DOWNLOAD_VERSION="4.2.0.32842"

# NxWitness (networkoptix) or DWSpectrum (digitalwatchdog) or NxMeta (networkoptix-metavms)
ARG RUNTIME_NAME="digitalwatchdog"

# The RUN wget command will be cached unless we change the cache tag
# Use the download version for the cache tag
ARG CACHE_DATE=${DOWNLOAD_VERSION}

# Prevent EULA and confirmation prompts in installers
ARG DEBIAN_FRONTEND=noninteractive

# Media server user and directory name
ENV COMPANY_NAME=${RUNTIME_NAME}

# Labels
LABEL name=${LABEL_NAME} \
    description=${LABEL_DESCRIPTION} \
    version=${DOWNLOAD_VERSION} \
    download=${DOWNLOAD_URL} \
    maintainer="Pieter Viljoen <ptr727@users.noreply.github.com>"

# Install tools and pre-requisites
RUN apt-get update \
    && apt-get upgrade --yes \
    && apt-get install --no-install-recommends --yes \
        gdb \
        mc \
        nano \
        strace \
        wget \
    && apt-get clean \
    && apt-get autoremove --purge \
    && rm -rf /var/lib/apt/lists/*

# Download the DEB installer file
RUN wget --no-verbose --no-check-certificate --output-document=./vms_server.deb ${DOWNLOAD_URL}

# Install the mediaserver
RUN apt-get update \
    && apt-get install --no-install-recommends --yes \
        ./vms_server.deb \
    && apt-get clean \
    && apt-get autoremove --purge \
    && rm -rf /var/lib/apt/lists/* \
    && rm -rf ./vms_server.deb

# TODO: Consider matching the Nx docker config
# https://github.com/networkoptix/nx_open_integrations/blob/master/docker/Dockerfile
# For now we'll just bypass the systemd dependency but still run as root

# Copy the entrypoint.sh launch script
# entrypoint.sh will run the mediaserver and root-tool
COPY root/entrypoint.sh /opt/entrypoint.sh
RUN chmod +x /opt/entrypoint.sh

# Runs entrypoint.sh on container start
ENTRYPOINT ["/opt/entrypoint.sh"]

# Expose port 7001
EXPOSE 7001

# Mount points
# /opt/${COMPANY_NAME}/mediaserver/etc -> /config
# /opt/${COMPANY_NAME}/mediaserver/var -> /media

# https://support.networkoptix.com/hc/en-us/articles/205313168-Nx-Witness-Operating-System-Support
# Latest supported for v5.1 is Jammy
# https://hub.docker.com/r/lsiobase/ubuntu/tags?page=1&name=jammy
FROM lsiobase/ubuntu:jammy


# Labels
ARG LABEL_NAME="NxMeta-LSIO"
ARG LABEL_DESCRIPTION="Nx Meta VMS Docker based on LinuxServer"

# Download URL and version
# Current values are defined by the build pipeline
ARG DOWNLOAD_X64_URL="https://updates.networkoptix.com/metavms/37996/metavms-server_update-5.1.2.37996-linux_x64.zip"
ARG DOWNLOAD_ARM64_URL="https://updates.networkoptix.com/metavms/37996/metavms-server_update-5.1.2.37996-linux_arm64.zip"
ARG DOWNLOAD_VERSION="5.1.2.37996"

# NxWitness (networkoptix) or DWSpectrum (digitalwatchdog) or NxMeta (networkoptix-metavms)
ARG RUNTIME_NAME="networkoptix-metavms"

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

# Build tool version set as build argument
ARG LABEL_VERSION="1.0.0.0"

# LABEL_NAME and LABEL_DESCRIPTION set in specific variant of build

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
        mc \
        nano \
        unzip \
        wget

# Download the installer file
RUN mkdir -p /temp
COPY Download.sh /temp/Download.sh
# Set the working directory to /temp
WORKDIR /temp
RUN chmod +x Download.sh \
    && ./Download.sh

# LSIO maps the host PUID and PGID environment variables to "abc" in the container.
# The mediaserver calls "chown ${COMPANY_NAME}" at runtime
# We have to match the ${COMPANY_NAME} username with the LSIO "abc" usernames
# LSIO does not officially support changing the "abc" username
# https://discourse.linuxserver.io/t/changing-abc-container-user/3208
# https://github.com/linuxserver/docker-baseimage-ubuntu/blob/jammy/root/etc/s6-overlay/s6-rc.d/init-adduser/run
# Change user "abc" to ${COMPANY_NAME}
RUN usermod -l ${COMPANY_NAME} abc \
# Change group "abc" to ${COMPANY_NAME}
    && groupmod -n ${COMPANY_NAME} abc \
# Replace "abc" with ${COMPANY_NAME}
    && sed -i "s/abc/\${COMPANY_NAME}/g" /etc/s6-overlay/s6-rc.d/init-adduser/run

# Install the mediaserver and dependencies
RUN apt-get update \
    && apt-get install --no-install-recommends --yes \
        gdb \
        ./vms_server.deb \
# Cleanup        
    && apt-get clean \
    && apt-get autoremove --purge \
    && rm -rf /var/lib/apt/lists/* \
    && rm -rf /temp

# Set ownership permissions
RUN chown --verbose ${COMPANY_NAME}:${COMPANY_NAME} /opt/${COMPANY_NAME}/mediaserver/bin \
    && chown --verbose ${COMPANY_NAME}:${COMPANY_NAME} /opt/${COMPANY_NAME}/mediaserver/bin/external.dat

# Copy etc init and services files
# https://github.com/just-containers/s6-overlay#container-environment
# https://www.linuxserver.io/blog/how-is-container-formed
COPY root/etc /etc

# Expose port 7001
EXPOSE 7001

# Create mount points
# Links will be created at runtime in LSIO/etc/s6-overlay/s6-rc.d/init-nx-relocate/run
# /opt/${COMPANY_NAME}/mediaserver/etc -> /config/etc
# /opt/${COMPANY_NAME}/mediaserver/var -> /config/var
# /root/.config/nx_ini links -> /config/ini
# /config is for configuration
# /media is for media recording
VOLUME /config /media


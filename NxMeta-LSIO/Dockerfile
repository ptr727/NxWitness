FROM lsiobase/ubuntu:focal


# Labels
ARG LABEL_NAME="NxMeta-LSIO"
ARG LABEL_DESCRIPTION="Nx Meta VMS Docker based on LinuxServer"

# Download URL and version
# Current values are defined by the build pipeline
ARG DOWNLOAD_URL="https://updates.networkoptix.com/metavms/5.0.0.35062%20R9/linux/metavms-server-5.0.0.35062-linux_x64.deb"
ARG DOWNLOAD_VERSION="5.0.0.35062"

# NxWitness (networkoptix) or DWSpectrum (digitalwatchdog) or NxMeta (networkoptix-metavms)
ARG RUNTIME_NAME="networkoptix-metavms"

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

# Install tools, don't cleanup, do cleanup after install mediaserver
RUN apt-get update \
    && apt-get upgrade --yes \
    && apt-get install --no-install-recommends --yes \
        mc \
        nano \
        wget

# Download the DEB installer file
RUN wget --no-verbose --no-check-certificate --output-document=./vms_server.deb ${DOWNLOAD_URL}

# LSIO maps the host PUID and PGID environment variables to "abc" in the container.
# The mediaserver calls "chown ${COMPANY_NAME}" at runtime
# We have to match the ${COMPANY_NAME} username with the LSIO "abc" usernames
# https://github.com/linuxserver/docker-baseimage-alpine/blob/master/root/etc/cont-init.d/10-adduser
# Change user "abc" to ${COMPANY_NAME}
RUN usermod -l ${COMPANY_NAME} abc \
# Change group "abc" to ${COMPANY_NAME}
    && groupmod -n ${COMPANY_NAME} abc \
# Replace "abc" with ${COMPANY_NAME}
    && sed -i "s/abc/\${COMPANY_NAME}/g" /etc/cont-init.d/10-adduser

# Install the mediaserver and dependencies
RUN apt-get update \
    && apt-get install --no-install-recommends --yes \
        gdb \
        ./vms_server.deb \
# Cleanup        
    && apt-get clean \
    && apt-get autoremove --purge \
    && rm -rf /var/lib/apt/lists/* \
    && rm -rf ./vms_server.deb

# Set ownership permissions
RUN chown --verbose ${COMPANY_NAME}:${COMPANY_NAME} /opt/${COMPANY_NAME}/mediaserver/bin \
    && chown --verbose ${COMPANY_NAME}:${COMPANY_NAME} /opt/${COMPANY_NAME}/mediaserver/bin/external.dat

# Copy etc init and services files
# https://github.com/just-containers/s6-overlay#container-environment
COPY root/etc /etc

# Expose port 7001
EXPOSE 7001

# Create mount points
# Links will be created at runtime in the etc/cont-init.d/50-relocate-files script
# /opt/${COMPANY_NAME}/mediaserver/etc -> /config/etc
# /opt/${COMPANY_NAME}/mediaserver/var -> /config/var
# /opt/${COMPANY_NAME}/mediaserver/var/data -> /media
# /config is for configuration
# /media is for media recording
# /archive is for media backups
VOLUME /config /media /archive


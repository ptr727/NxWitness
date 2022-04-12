FROM ubuntu:focal


# Labels
ARG LABEL_NAME="NxMeta"
ARG LABEL_DESCRIPTION="Nx Witness VMS Docker"

# Download URL and version
# Current values are defined by the build pipeline
ARG DOWNLOAD_URL="https://updates.networkoptix.com/default/4.2.0.34633/linux/nxwitness-server-4.2.0.34633-linux64-patch.deb"
ARG DOWNLOAD_VERSION="4.2.0.34633"

# NxWitness (networkoptix) or DWSpectrum (digitalwatchdog) or NxMeta (networkoptix-metavms)
ARG RUNTIME_NAME="networkoptix"

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

# Install tools
RUN apt-get update \
    && apt-get upgrade --yes \
    && apt-get install --no-install-recommends --yes \
        mc \
        nano \
        wget \
    && apt-get clean \
    && apt-get autoremove --purge \
    && rm -rf /var/lib/apt/lists/*

# Download the DEB installer file
RUN wget --no-verbose --no-check-certificate --output-document=./vms_server.deb ${DOWNLOAD_URL}

# Install the mediaserver and dependencies
RUN apt-get update \
    && apt-get install --no-install-recommends --yes \
        gdb \
        sudo \
        ./vms_server.deb \
    && apt-get clean \
    && apt-get autoremove --purge \
    && rm -rf /var/lib/apt/lists/* \
    && rm -rf ./vms_server.deb

# Add the mediaserver ${COMPANY_NAME} user to the sudoers group
# Only allow sudo no password access to the root-tool
# Add v4.2 and v4.3 flavors of the root tool naming
RUN echo "${COMPANY_NAME} ALL = NOPASSWD: /opt/${COMPANY_NAME}/mediaserver/bin/root-tool, /opt/${COMPANY_NAME}/mediaserver/bin/root-tool-bin" > /etc/sudoers.d/${COMPANY_NAME}

# Copy the entrypoint.sh launch script
# entrypoint.sh will run the mediaserver and root-tool
COPY root/entrypoint.sh /opt/entrypoint.sh
RUN chmod +x /opt/entrypoint.sh

# Run the entrypoint as the mediaserver ${COMPANY_NAME} user
# Note that this user exists in the container and does not directly map to a user on the host
USER ${COMPANY_NAME}

# Runs entrypoint.sh on container start
ENTRYPOINT ["/opt/entrypoint.sh"]

# Expose port 7001
EXPOSE 7001

# Mount points
# /opt/${COMPANY_NAME}/mediaserver/etc -> /config
# /opt/${COMPANY_NAME}/mediaserver/var -> /media


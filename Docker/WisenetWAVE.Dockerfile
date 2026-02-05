# Dockerfile created by CreateMatrix, do not modify by hand
# Product: WisenetWAVE
# Description: Wisenet WAVE VMS
# Company: hanwha
# Release: hanwha
# LSIO: False

# https://support.networkoptix.com/hc/en-us/articles/205313168-Nx-Witness-Operating-System-Support
# Latest Ubuntu supported for v6 is Noble
# Base images are built in this repo, see Docker/NxBase*.Dockerfile
FROM docker.io/ptr727/nx-base:ubuntu-noble

# Labels
ARG LABEL_NAME="WisenetWAVE"
ARG LABEL_DESCRIPTION="Wisenet WAVE VMS"
ARG LABEL_VERSION="6.1.0.42176"

# Download URL and version
# Current values are defined by the build pipeline
ARG DOWNLOAD_X64_URL="https://updates.networkoptix.com/hanwha/42176/wave-server_update-6.1.0.42176-linux_x64.zip"
ARG DOWNLOAD_ARM64_URL="https://updates.networkoptix.com/hanwha/42176/wave-server_update-6.1.0.42176-linux_arm64.zip"
ARG DOWNLOAD_VERSION="6.1.0.42176"

# Used for ${COMPANY_NAME} setting the server user and install directory
ARG RUNTIME_NAME="hanwha"

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

# Base image includes required tools and utilities.
# Download the installer file
WORKDIR /temp
RUN /bin/bash -euo pipefail -c '\
    echo "Cache: ${CACHE_DATE}"; \
    DEB_FILE="./vms_server.deb"; \
    TARGET_PLATFORM="${TARGETPLATFORM:-}"; \
    DOWNLOAD_URL="${DOWNLOAD_X64_URL:?DOWNLOAD_X64_URL is required}"; \
    if [ "${TARGET_PLATFORM}" = "linux/arm64" ]; then \
        DOWNLOAD_URL="${DOWNLOAD_ARM64_URL:?DOWNLOAD_ARM64_URL is required}"; \
    fi; \
    echo "Download URL: ${DOWNLOAD_URL}"; \
    DOWNLOAD_FILENAME="$(basename -- "${DOWNLOAD_URL}")"; \
    echo "Download Filename: ${DOWNLOAD_FILENAME}"; \
    wget --no-verbose --tries=5 --timeout=30 --retry-connrefused "${DOWNLOAD_URL}"; \
    case "${DOWNLOAD_FILENAME}" in \
        *.zip) \
            echo "Downloaded ZIP: ${DOWNLOAD_FILENAME}"; \
            DOWNLOAD_DIR="./download_zip"; \
            rm -rf "${DOWNLOAD_DIR}"; \
            mkdir -p "${DOWNLOAD_DIR}"; \
            unzip -q -d "${DOWNLOAD_DIR}" "${DOWNLOAD_FILENAME}"; \
            DEB_ZIP_FILE="$(find "${DOWNLOAD_DIR}" -maxdepth 1 -type f -name "*.deb" -print -quit)"; \
            if [ -z "${DEB_ZIP_FILE}" ]; then \
                echo "No .deb found in ${DOWNLOAD_DIR}" >&2; \
                exit 1; \
            fi; \
            echo "DEB in ZIP: ${DEB_ZIP_FILE}"; \
            mv "${DEB_ZIP_FILE}" "${DEB_FILE}"; \
            rm -rf "${DOWNLOAD_DIR}" "${DOWNLOAD_FILENAME}"; \
            ;; \
        *.deb) \
            echo "Downloaded DEB: ${DOWNLOAD_FILENAME}"; \
            mv "${DOWNLOAD_FILENAME}" "${DEB_FILE}"; \
            ;; \
        *) \
            echo "Unsupported download type: ${DOWNLOAD_FILENAME}" >&2; \
            exit 1; \
            ;; \
    esac; \
    echo "DEB File: ${DEB_FILE}"'

# Install the mediaserver
RUN apt-get update \
    && apt-get install --no-install-recommends --yes \
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

# Create mount points
# Link config directly to internal paths
# /mnt/config/etc:opt/hanwha/mediaserver/etc
# /mnt/config/nx_ini:/home/hanwha/.config/nx_ini
# /mnt/config/var:/opt/hanwha/mediaserver/var
# /media is for recordings
# /backup is for backups
# /analytics is for analytics
VOLUME /media /backup /analytics

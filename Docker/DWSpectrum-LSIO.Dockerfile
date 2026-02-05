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
ARG LABEL_VERSION="6.1.0.42176"

# Download URL and version
# Current values are defined by the build pipeline
ARG DOWNLOAD_X64_URL="https://updates.networkoptix.com/digitalwatchdog/42176/dwspectrum-server_update-6.1.0.42176-linux_x64.zip"
ARG DOWNLOAD_ARM64_URL="https://updates.networkoptix.com/digitalwatchdog/42176/dwspectrum-server_update-6.1.0.42176-linux_arm64.zip"
ARG DOWNLOAD_VERSION="6.1.0.42176"

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

# Base image includes required tools and utilities.
# Download the installer file
WORKDIR /temp
RUN /bin/bash -euo pipefail -c '\
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

# LSIO maps the host PUID and PGID environment variables to "abc" in the container.
# https://docs.linuxserver.io/misc/non-root/
# LSIO does not officially support changing the "abc" username
# https://discourse.linuxserver.io/t/changing-abc-container-user/3208
# https://github.com/linuxserver/docker-baseimage-ubuntu/blob/noble/root/etc/s6-overlay/s6-rc.d/init-adduser/run
# The mediaserver calls "chown ${COMPANY_NAME}" at runtime
# Change LSIO user "abc" to ${COMPANY_NAME}
RUN usermod -l ${COMPANY_NAME} abc \
    # Change group "abc" to ${COMPANY_NAME}
    && groupmod -n ${COMPANY_NAME} abc \
    # Replace "abc" with ${COMPANY_NAME}
    && sed -i "s/abc/\${COMPANY_NAME}/g" /etc/s6-overlay/s6-rc.d/init-adduser/run

# Install the mediaserver
RUN apt-get update \
    && apt-get install --no-install-recommends --yes \
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

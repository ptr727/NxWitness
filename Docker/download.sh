#!/bin/bash

# Download and stage the mediaserver .deb installer into the current working directory.
# Arguments:
#   $1 - x64 download URL
#   $2 - arm64 download URL
#   $3 - target platform, e.g. linux/amd64 or linux/arm64 (optional, defaults to x64)

set -euo pipefail

DOWNLOAD_X64_URL="${1:?x64 download URL is required}"
DOWNLOAD_ARM64_URL="${2:?arm64 download URL is required}"
TARGET_PLATFORM="${3:-}"

DEB_FILE="./vms_server.deb"
DOWNLOAD_URL="${DOWNLOAD_X64_URL}"
if [ "${TARGET_PLATFORM}" = "linux/arm64" ]; then
    DOWNLOAD_URL="${DOWNLOAD_ARM64_URL}"
fi

echo "Download URL: ${DOWNLOAD_URL}"
DOWNLOAD_FILENAME="$(basename -- "${DOWNLOAD_URL}")"
echo "Download Filename: ${DOWNLOAD_FILENAME}"
wget --no-verbose --tries=5 --timeout=30 --retry-connrefused "${DOWNLOAD_URL}"

case "${DOWNLOAD_FILENAME}" in
    *.zip)
        echo "Downloaded ZIP: ${DOWNLOAD_FILENAME}"
        DOWNLOAD_DIR="./download_zip"
        rm -rf "${DOWNLOAD_DIR}"
        mkdir -p "${DOWNLOAD_DIR}"
        unzip -q -d "${DOWNLOAD_DIR}" "${DOWNLOAD_FILENAME}"
        DEB_ZIP_FILE="$(find "${DOWNLOAD_DIR}" -maxdepth 1 -type f -name "*.deb" -print -quit)"
        if [ -z "${DEB_ZIP_FILE}" ]; then
            echo "No .deb found in ${DOWNLOAD_DIR}" >&2
            exit 1
        fi
        echo "DEB in ZIP: ${DEB_ZIP_FILE}"
        mv "${DEB_ZIP_FILE}" "${DEB_FILE}"
        rm -rf "${DOWNLOAD_DIR}" "${DOWNLOAD_FILENAME}"
        ;;
    *.deb)
        echo "Downloaded DEB: ${DOWNLOAD_FILENAME}"
        mv "${DOWNLOAD_FILENAME}" "${DEB_FILE}"
        ;;
    *)
        echo "Unsupported download type: ${DOWNLOAD_FILENAME}" >&2
        exit 1
        ;;
esac

echo "DEB File: ${DEB_FILE}"

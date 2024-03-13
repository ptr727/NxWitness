#!/bin/bash

# Installer expected filename
DEB_FILE="./vms_server.deb"

# Use X64 or ARM64 URL
DOWNLOAD_URL=${DOWNLOAD_X64_URL}
if [ "${TARGETPLATFORM}" == "linux/arm64" ]; then
        DOWNLOAD_URL=${DOWNLOAD_ARM64_URL};
    fi

# Get the filename fom the URL
echo "Download URL: ${DOWNLOAD_URL}"
DOWNLOAD_FILENAME=$(basename "$DOWNLOAD_URL")
echo "Download Filename: ${DOWNLOAD_FILENAME}"

# Download the file
wget --no-verbose ${DOWNLOAD_URL}

# Test if the filename is a DEB file or a ZIP file
if [ "${DOWNLOAD_FILENAME: -4}" == ".zip" ]
then
    echo "Downloaded ZIP: ${DOWNLOAD_FILENAME}"
    # Extract the zip contents
    unzip -d ./download_zip ${DOWNLOAD_FILENAME}
    # The DEB file is not always the same name as the ZIP file, get the DEB filename
    DEB_FILES=( ./download_zip/*.deb )
    DEB_ZIP_FILE=${DEB_FILES[0]}
    echo "DEB in ZIP: ${DEB_ZIP_FILE}"
    # Copy
    cp ${DEB_ZIP_FILE} ${DEB_FILE}
else
    echo "Downloaded DEB: ${DOWNLOAD_FILENAME}"
    # Copy
    cp ${DOWNLOAD_FILENAME} ./vms_server.deb
fi
echo "DEB File: ${DEB_FILE}"

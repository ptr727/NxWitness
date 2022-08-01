#!/bin/bash

# Installer expected filename
DEB_FILE="./vms_server.deb"

# Get the filename fom the URL
# export DOWNLOAD_URL="https://updates.networkoptix.com/metavms/35151/metavms-server_update-5.1.0.35151-linux_x64-beta.zip"
echo "Download URL: ${DOWNLOAD_URL}"
DOWNLOAD_FILENAME=$(basename "$DOWNLOAD_URL")
echo "Download Filename: ${DOWNLOAD_FILENAME}"

# Downlaod the file
wget ${DOWNLOAD_URL}

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
    # Cleanup
    rm ${DOWNLOAD_FILENAME}
    rm -r ./download_zip
else
    echo "Downloaded DEB: ${DOWNLOAD_FILENAME}"
    # Copy
    cp ${DOWNLOAD_FILENAME} ./vms_server.deb
    # Cleanup
    rm ${DOWNLOAD_FILENAME}
fi
echo "DEB File: ${DEB_FILE}"

#!/bin/bash

# Launch the root-tool as root in the background using &
echo "Launching root-tool"
sudo /opt/${COMPANY_NAME}/mediaserver/bin/root-tool &

# Tell mediaserver it is running under Docker so it reports its OS variant correctly
# https://github.com/networkoptix/nxvms-docker/commit/54bbd16
# Inject at runtime rather than build time because the recommended non-LSIO setup
# bind-mounts /opt/${COMPANY_NAME}/mediaserver/etc from the host, which hides any
# build-time edit. Check for any `currentOsVariantOverride=` key (regardless of
# value) so we never duplicate the line on subsequent starts and never override
# a value the user explicitly set themselves.
MEDIASERVER_CONF="/opt/${COMPANY_NAME}/mediaserver/etc/mediaserver.conf"
if ! grep -q "^currentOsVariantOverride=" "${MEDIASERVER_CONF}" 2>/dev/null
then
    echo "Adding currentOsVariantOverride=docker to ${MEDIASERVER_CONF}"
    echo "currentOsVariantOverride=docker" >> "${MEDIASERVER_CONF}"
fi

# Launch the mediaserver using exec so it receives shutdown commands
echo "Launching mediaserver"
exec /opt/${COMPANY_NAME}/mediaserver/bin/mediaserver -e

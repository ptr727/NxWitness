#!/bin/bash

# Launch the root-tool as root in the background using &
echo "Launching root-tool"
sudo /opt/${COMPANY_NAME}/mediaserver/bin/root-tool &

# Launch the mediaserver using exec so it receives shutdown commands
echo "Launching mediaserver"
exec /opt/${COMPANY_NAME}/mediaserver/bin/mediaserver -e

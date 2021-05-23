#!/bin/bash

# Version 4.3 renamed the binary files, conditionally launch the right binary

# Launch the root-tool in the background using &
if [[ -e /opt/${COMPANY_NAME}/mediaserver/bin/root-tool-bin ]]
then
    echo "Launching root-tool-bin"
    sudo /opt/${COMPANY_NAME}/mediaserver/bin/root-tool-bin &
else
    echo "Launching root-tool"
    sudo /opt/${COMPANY_NAME}/mediaserver/bin/root-tool &
fi


# Launch the mediaserver using exec so it receives shutdown commands
if [[ -e /opt/${COMPANY_NAME}/mediaserver/bin/mediaserver-bin ]]
then
    echo "Launching mediaserver-bin"
    exec /opt/${COMPANY_NAME}/mediaserver/bin/mediaserver-bin -e
else
    echo "Launching mediaserver"
    exec /opt/${COMPANY_NAME}/mediaserver/bin/mediaserver -e
fi

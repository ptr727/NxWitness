include(`base-lsio.docker')

# Labels
ARG LABEL_NAME="NxMeta-LSIO"
ARG LABEL_DESCRIPTION="Nx Meta VMS Docker based on LinuxServer"

include(`nxmeta.docker')
include(`body.docker')
include(`body-lsio.docker')

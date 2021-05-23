include(`base-lsio.docker')

# Labels
ARG LABEL_NAME="NxWitness-LSIO"
ARG LABEL_DESCRIPTION="Nx Witness VMS Docker based on LinuxServer"

include(`nxwitness.docker')
include(`body.docker')
include(`body-lsio.docker')

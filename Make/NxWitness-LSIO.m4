# Use LSIO Ubuntu Focal Fossa 20.04 LTS
FROM lsiobase/ubuntu:focal

# Labels
ARG LABEL_NAME="NxWitness-LSIO"
ARG LABEL_DESCRIPTION="Nx Witness VMS Docker based on LinuxServer"

include(`nxwitness.docker')
include(`body-lsio.docker')

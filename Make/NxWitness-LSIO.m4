# Use LSIO Ubuntu Bionic
FROM lsiobase/ubuntu:bionic

# Labels
ARG LABEL_NAME="NxWitness-LSIO"
ARG LABEL_DESCRIPTION="Nx Witness VMS Docker based on LinuxServer"

include(`nxwitness.docker')
include(`body-lsio.docker')

# Use LSIO Ubuntu Focal Fossa 20.04 LTS
FROM lsiobase/ubuntu:focal

# Labels
ARG LABEL_NAME="NxMeta-LSIO"
ARG LABEL_DESCRIPTION="Nx Meta VMS Docker based on LinuxServer"

include(`nxmeta.docker')
include(`body-lsio.docker')

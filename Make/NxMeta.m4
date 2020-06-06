# Use Ubuntu Bionic
FROM ubuntu:bionic

# Labels
ARG LABEL_NAME="NxMeta"
ARG LABEL_DESCRIPTION="Nx Meta VMS Docker"

include(`nxmeta.docker')
include(`body.docker')

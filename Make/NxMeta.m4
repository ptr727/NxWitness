# Use Ubuntu Focal Fossa 20.04 LTS
FROM ubuntu:focal

# Labels
ARG LABEL_NAME="NxMeta"
ARG LABEL_DESCRIPTION="Nx Meta VMS Docker"

include(`nxmeta.docker')
include(`body.docker')

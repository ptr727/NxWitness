# Use Ubuntu Focal Fossa 20.04 LTS
FROM ubuntu:focal

# Labels
ARG LABEL_NAME="NxMeta"
ARG LABEL_DESCRIPTION="Nx Witness VMS Docker"

include(`nxwitness.docker')
include(`body.docker')

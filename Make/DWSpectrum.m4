# Use Ubuntu Focal Fossa 20.04 LTS
FROM ubuntu:focal

# Labels
ARG LABEL_NAME="DWSpectrum"
ARG LABEL_DESCRIPTION="DW Spectrum IPVMS Docker"

include(`dwspectrum.docker')
include(`body.docker')

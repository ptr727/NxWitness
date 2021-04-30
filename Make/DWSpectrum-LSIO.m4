# Use LSIO Ubuntu Focal Fossa 20.04 LTS
FROM lsiobase/ubuntu:focal

# Labels
ARG LABEL_NAME="DWSpectrum-LSIO"
ARG LABEL_DESCRIPTION="DW Spectrum IPVMS Docker based on LinuxServer"

include(`dwspectrum.docker')
include(`body-lsio.docker')

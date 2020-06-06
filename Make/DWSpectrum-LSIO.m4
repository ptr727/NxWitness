# Use LSIO Ubuntu Bionic
FROM lsiobase/ubuntu:bionic

# Labels
ARG LABEL_NAME="DWSpectrum-LSIO"
ARG LABEL_DESCRIPTION="DW Spectrum IPVMS Docker based on LinuxServer"

include(`dwspectrum.docker')
include(`body-lsio.docker')

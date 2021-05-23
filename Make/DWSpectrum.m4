include(`base.docker')

# Labels
ARG LABEL_NAME="DWSpectrum"
ARG LABEL_DESCRIPTION="DW Spectrum IPVMS Docker"

include(`dwspectrum.docker')
include(`body.docker')
include(`body-entrypoint.docker')
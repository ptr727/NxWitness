include(`base.docker')

# Labels
ARG LABEL_NAME="NxMeta"
ARG LABEL_DESCRIPTION="Nx Witness VMS Docker"

include(`nxwitness.docker')
include(`body.docker')
include(`body-entrypoint.docker')
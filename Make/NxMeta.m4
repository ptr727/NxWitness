include(`base.docker')

# Labels
ARG LABEL_NAME="NxMeta"
ARG LABEL_DESCRIPTION="Nx Meta VMS Docker"

include(`nxmeta.docker')
include(`body.docker')
include(`body-entrypoint.docker')
#!/bin/bash

set -euo pipefail

# Delete images
function DeleteImage {
    docker image rm test_${1,,} || true
}

# Down stack
./Down.sh

# Delete images
DeleteImage "NxGo"
DeleteImage "NxGo-LSIO"
DeleteImage "NxMeta"
DeleteImage "NxMeta-LSIO"
DeleteImage "NxWitness"
DeleteImage "NxWitness-LSIO"
DeleteImage "DWSpectrum"
DeleteImage "DWSpectrum-LSIO"
DeleteImage "WisenetWAVE"
DeleteImage "WisenetWAVE-LSIO"

#!/bin/bash

set -x
set -e

# Delete images
function DeleteImage {
    docker image rm test_${1,,} || true
}

# Down stack
./Down.sh

# Delete images
DeleteImage "DWSpectrum"
DeleteImage "DWSpectrum-LSIO"
DeleteImage "NxMeta"
DeleteImage "NxMeta-LSIO"
DeleteImage "NxWitness"
DeleteImage "NxWitness-LSIO"

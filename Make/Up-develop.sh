#!/bin/bash

set -e

# Launch stack
docker compose --file Test-develop.yml up --detach

# Instructions
echo "Ctrl-Click on links to launch web UI in browser"
echo "DW Spectrum:" "https://$HOSTNAME:7101/"
echo "DW Spectrum-LSIO:" "https://$HOSTNAME:7111/"
echo "Nx Witness:" "https://$HOSTNAME:7102/"
echo "Nx Witness-LSIO:" "https://$HOSTNAME:7112/"
echo "Nx Meta:" "https://$HOSTNAME:7103/"
echo "Nx Meta-LSIO:" "https://$HOSTNAME:7113/"
echo "Run 'Down-develop.sh' to stop stack"

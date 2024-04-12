#!/bin/bash

set -e

# Launch stack
docker compose --file Test.yml up --detach

# Instructions
echo "Ctrl-Click on links to launch web UI in browser"
echo "Nx Meta:" "https://$HOSTNAME:7101/"
echo "Nx Meta LSIO:" "https://$HOSTNAME:7111/"
echo "Nx Witness:" "https://$HOSTNAME:7102/"
echo "Nx Witness LSIO:" "https://$HOSTNAME:7112/"
echo "DW Spectrum:" "https://$HOSTNAME:7103/"
echo "DW Spectrum LSIO:" "https://$HOSTNAME:7113/"
echo "Wisenet WAVE:" "https://$HOSTNAME:7104/"
echo "Wisenet WAVE LSIO:" "https://$HOSTNAME:7114/"
echo "Run 'Down-develop.sh' to stop stack"

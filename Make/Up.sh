./Down.sh

docker-compose --file Test.yml down --volumes
docker-compose --file Test.yml up --detach

echo "DW Spectrum:" "https://$HOSTNAME:7101/"
echo "DW Spectrum-LSIO:" "https://$HOSTNAME:7111/"
echo "Nx Witness:" "https://$HOSTNAME:7102/"
echo "Nx Witness-LSIO:" "https://$HOSTNAME:7112/"
echo "Nx Meta:" "https://$HOSTNAME:7103/"
echo "Nx Meta-LSIO:" "https://$HOSTNAME:7113/"

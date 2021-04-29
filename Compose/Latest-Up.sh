./Stable-Down.sh

docker-compose --file LSIO-Latest.yml down --volumes
docker-compose --file LSIO-Latest.yml pull
docker-compose --file LSIO-Latest.yml up --detach

echo "https://$HOSTNAME:7101/"
echo "https://$HOSTNAME:7102/"
echo "https://$HOSTNAME:7103/"

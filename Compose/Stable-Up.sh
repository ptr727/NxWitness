./Latest-Down.sh

docker-compose --file LSIO-Stable.yml down --volumes
docker-compose --file LSIO-Stable.yml pull
docker-compose --file LSIO-Stable.yml up --detach

echo "https://$HOSTNAME:7101/"
echo "https://$HOSTNAME:7102/"
echo "https://$HOSTNAME:7103/"

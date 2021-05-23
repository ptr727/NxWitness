./Down.sh

docker-compose --file Test.yml down --volumes
docker-compose --file Test.yml up --detach

echo "https://$HOSTNAME:7101/"
echo "https://$HOSTNAME:7111/"
echo "https://$HOSTNAME:7102/"
echo "https://$HOSTNAME:7112/"
echo "https://$HOSTNAME:7103/"
echo "https://$HOSTNAME:7113/"

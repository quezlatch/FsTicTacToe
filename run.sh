[ ! -z $(docker images -q fsharp) ] || docker build -t fsharp base-fsharp
docker-compose build
docker-compose up

deploy-gateway:
	docker build -f Dockerfile_gw -t bookgateway:latest .
	docker tag bookgateway:latest localhost:5000/bookgateway:latest
	docker push localhost:5000/bookgateway:latest
	docker-compose -f docker-compose-gw-micro.yml up -d --force-recreate bookgateway

deploy-admingrpcservice:
	docker build -f Dockerfile_grpc_admin -t admingrpcservice:latest .
	docker tag admingrpcservice:latest localhost:5000/admingrpcservice:latest
	docker push localhost:5000/admingrpcservice:latest
	docker-compose -f docker-compose-gw-micro.yml up -d --force-recreate admingrpcservice

deploy-usergrpcservice:
	docker build -f Dockerfile_grpc_user -t usergrpcservice:latest .
	docker tag usergrpcservice:latest localhost:5000/usergrpcservice:latest
	docker push localhost:5000/usergrpcservice:latest
	docker-compose -f docker-compose-gw-micro.yml up -d --force-recreate usergrpcservice

start-registry:
	docker run -d -p 5000:5000 --restart=always --name local-registry registry:2

start-kibana:
	docker-compose -f docker-compose-gw-micro.yml up -d kibana

start-elastic:
	docker-compose -f docker-compose-gw-micro.yml up -d elasticsearch

start-mongo:
	docker-compose -f ./dockers/docker-compose-mongo.yml up -d

build:
	docker-compose -f docker-compose-gw-micro.yml up --build

up:
	docker-compose -f docker-compose-gw-micro.yml up

up-scale:
	docker-compose -f docker-compose-gw-micro.yml up --scale elasticsearch=0 --scale kibana=0

down:
	docker-compose -f docker-compose-gw-micro.yml down
.PHONY=build,run,push,init,deploy

include .env

NAME=harambe-trader
REMOTE_DOCKER_REPO=jeremyaherzog
VERSION?=$(shell git rev-parse head)

build:
	dotnet publish -c build
	docker build -t ${NAME} .

run:
	docker run --rm \
		--name ${NAME} \
		--env-file .env \
		${NAME}

push:
	docker tag ${NAME} ${REMOTE_DOCKER_REPO}/${NAME}:latest
	docker tag ${NAME} ${REMOTE_DOCKER_REPO}/${NAME}:${VERSION}
	docker push ${REMOTE_DOCKER_REPO}/${NAME}:latest
	docker push ${REMOTE_DOCKER_REPO}/${NAME}:${VERSION}

init:
	terraform init \
		-backend-config "access_key=${TF_STATE_ACCESS_KEY}" \
		-backend-config "secret_key=${TF_STATE_SECRET_KEY}"

deploy:
	terraform apply \
		-var "do_token=${DO_TOKEN}" \
		-var "version_tag"=${VERSION} \
		-var "broker_api_key_id=${BROKER_API_KEY_ID}" \
		-var "broker_api_key_secret=${BROKER_API_KEY_SECRET}" \
		-var "datasource_token=${DATASOURCE_TOKEN}" \
		-var "database_connection_string=${DATABASE_CONNECTION_STRING}"

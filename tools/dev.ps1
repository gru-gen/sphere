# Development helpers:  . .\tools\dev.ps1

function Start-SphereDb {
    docker run -d --name sphere-db `
        -e POSTGRES_USER=sphere `
        -e POSTGRES_PASSWORD=sphere-dev `
        -e POSTGRES_DB=sphere `
        -p 5432:5432 `
        -v sphere-db-data:/var/lib/postgresql/data `
        postgres:17.5-alpine
}

function Add-CatalogDb {
    docker exec sphere-db psql -U sphere -d sphere `
        -c "CREATE DATABASE catalog_db OWNER sphere"
}

function Add-BasketDb {
    docker exec sphere-db psql -U sphere -d sphere `
        -c "CREATE DATABASE basket_db OWNER sphere"
}

function Add-OrderingDb {
    docker exec sphere-db psql -U sphere -d sphere `
        -c "CREATE DATABASE ordering_db OWNER sphere"
}

function Stop-SphereDb {
    docker rm -f sphere-db
}

function Start-SphereKafka {
    docker run -d --name sphere-kafka `
        -p 9094:9094 `
        -e KAFKA_NODE_ID=1 `
        -e KAFKA_PROCESS_ROLES=broker,controller `
        -e KAFKA_CONTROLLER_QUORUM_VOTERS=1@localhost:9093 `
        -e KAFKA_LISTENERS=INTERNAL://:9092,CONTROLLER://:9093,HOST://:9094 `
        -e KAFKA_ADVERTISED_LISTENERS=INTERNAL://localhost:9092,HOST://localhost:9094 `
        -e KAFKA_LISTENER_SECURITY_PROTOCOL_MAP=INTERNAL:PLAINTEXT,CONTROLLER:PLAINTEXT,HOST:PLAINTEXT `
        -e KAFKA_CONTROLLER_LISTENER_NAMES=CONTROLLER `
        -e KAFKA_INTER_BROKER_LISTENER_NAME=INTERNAL `
        -e KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR=1 `
        -e KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR=1 `
        -e KAFKA_TRANSACTION_STATE_LOG_MIN_ISR=1 `
        -e KAFKA_AUTO_CREATE_TOPICS_ENABLE=false `
        apache/kafka:4.1.0
}
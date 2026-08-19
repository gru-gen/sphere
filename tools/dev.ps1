# Development helpers. Dot-source once per shell:  . .\tools\dev.ps1

function Start-SphereDb {
    docker run -d --name sphere-db `
        -e POSTGRES_USER=sphere `
        -e POSTGRES_PASSWORD=sphere-dev `
        -e POSTGRES_DB=sphere `
        -p 5432:5432 `
        -v sphere-db-data:/var/lib/postgresql/data `
        postgres:17.5-alpine
}

function Stop-SphereDb {
    docker rm -f sphere-db
}
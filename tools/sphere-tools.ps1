# Client tools as pinned, throwaway containers (ADR-002).
# Dot-source this file in PowerShell:  . .\tools\sphere-tools.ps1

function psql      { docker run --rm -it postgres:17.5-alpine psql @args }
function redis-cli { docker run --rm -it redis:7.4.2-alpine redis-cli @args }
function mongosh   { docker run --rm -it mongo:8.0.6 mongosh @args }
function kcat      { docker run --rm -it edenhill/kcat:1.7.1 @args }
function jq        { $input | docker run --rm -i ghcr.io/jqlang/jq:1.7.1 @args }

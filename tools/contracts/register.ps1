param(
    [string]$RegistryUrl = "http://localhost:8082"
)

$ErrorActionPreference = "Stop"
$group = "sphere"
$base = "$RegistryUrl/apis/registry/v3"

# why: the group first (409 = already there is fine)...
try {
    Invoke-RestMethod -Method Post -Uri "$base/groups" `
        -ContentType "application/json" `
        -Body (@{ groupId = $group } | ConvertTo-Json) | Out-Null
} catch { if ($_.Exception.Response.StatusCode.value__ -ne 409) { throw } }

# ...then the RULE: every new version must be BACKWARD compatible - old
# readers must still parse new writers. The registry now refuses the break.
try {
    Invoke-RestMethod -Method Post -Uri "$base/groups/$group/rules" `
        -ContentType "application/json" `
        -Body (@{ ruleType = "COMPATIBILITY"; config = "BACKWARD" } | ConvertTo-Json) | Out-Null
} catch { if ($_.Exception.Response.StatusCode.value__ -ne 409) { throw } }

Get-ChildItem "$PSScriptRoot/../../contracts/*.json" | ForEach-Object {
    $artifactId = $_.BaseName
    $schema = Get-Content $_.FullName -Raw
    $body = @{
        artifactId   = $artifactId
        artifactType = "JSON"
        firstVersion = @{ content = @{ content = $schema; contentType = "application/json" } }
    } | ConvertTo-Json -Depth 10

    # why: ifExists=CREATE_VERSION - the first run creates the artifact, every
    # later run submits a NEW VERSION, and the rule above judges it.
    Invoke-RestMethod -Method Post `
        -Uri "$base/groups/$group/artifacts?ifExists=CREATE_VERSION" `
        -ContentType "application/json" -Body $body | Out-Null
    Write-Host "registered $artifactId"
}

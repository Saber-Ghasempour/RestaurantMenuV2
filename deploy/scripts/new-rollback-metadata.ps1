[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $Environment,
    [Parameter(Mandatory)] [string] $PreviousImage,
    [Parameter(Mandatory)] [string] $TargetImage,
    [Parameter(Mandatory)] [string] $BackupReference,
    [Parameter(Mandatory)] [string] $Migration,
    [Parameter(Mandatory)] [string] $OutputPath
)

$ErrorActionPreference = 'Stop'
$digestPattern = '^ghcr\.io/[a-z0-9._/-]+@sha256:[0-9a-f]{64}$'

if ($PreviousImage -notmatch $digestPattern) {
    throw 'PreviousImage must be pinned by sha256 digest.'
}

if ($TargetImage -notmatch $digestPattern) {
    throw 'TargetImage must be pinned by sha256 digest.'
}

if ([string]::IsNullOrWhiteSpace($BackupReference)) {
    throw 'BackupReference is required before migrations can run.'
}

if ([string]::IsNullOrWhiteSpace($Migration)) {
    throw 'Migration release identifier is required.'
}

$metadata = [ordered]@{
    schemaVersion = 1
    environment = $Environment
    capturedAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
    previousImage = $PreviousImage
    targetImage = $TargetImage
    backupReference = $BackupReference
    migration = $Migration
    databaseRollbackStrategy = 'restore-or-roll-forward'
}

$parent = Split-Path -Parent $OutputPath
if ($parent) {
    [System.IO.Directory]::CreateDirectory($parent) | Out-Null
}

[System.IO.File]::WriteAllText(
    [System.IO.Path]::GetFullPath($OutputPath),
    ($metadata | ConvertTo-Json) + [Environment]::NewLine)

Write-Output ([System.IO.Path]::GetFullPath($OutputPath))

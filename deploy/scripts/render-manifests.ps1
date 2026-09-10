[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $ImageReference,

    [Parameter(Mandatory)]
    [ValidatePattern('^[a-z0-9]([-a-z0-9]*[a-z0-9])?$')]
    [string] $Namespace,

    [Parameter(Mandatory)]
    [string] $OutputDirectory
)

$ErrorActionPreference = 'Stop'

if ($ImageReference -notmatch '^ghcr\.io/[a-z0-9._/-]+@sha256:[0-9a-f]{64}$') {
    throw 'ImageReference must be a lowercase GHCR reference pinned by sha256 digest.'
}

$templateDirectory = Join-Path $PSScriptRoot '../kubernetes'
$resolvedOutput = [System.IO.Path]::GetFullPath($OutputDirectory)
[System.IO.Directory]::CreateDirectory($resolvedOutput) | Out-Null

Get-ChildItem -LiteralPath $templateDirectory -Filter '*.yaml' | ForEach-Object {
    $rendered = (Get-Content -LiteralPath $_.FullName -Raw).
        Replace('IMAGE_REFERENCE', $ImageReference).
        Replace('DEPLOYMENT_NAMESPACE', $Namespace)

    if ($rendered.Contains('IMAGE_REFERENCE') -or
        $rendered.Contains('DEPLOYMENT_NAMESPACE')) {
        throw "Unresolved deployment token in $($_.Name)."
    }

    $destination = Join-Path $resolvedOutput $_.Name
    [System.IO.File]::WriteAllText($destination, $rendered)
}

Write-Output $resolvedOutput

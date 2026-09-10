$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$failures = [System.Collections.Generic.List[string]]::new()

function Assert-FileContains {
    param(
        [Parameter(Mandatory)] [string] $Path,
        [Parameter(Mandatory)] [string[]] $Patterns
    )

    $fullPath = Join-Path $repositoryRoot $Path
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        $failures.Add("Missing required file: $Path")
        return
    }

    $content = Get-Content -LiteralPath $fullPath -Raw
    foreach ($pattern in $Patterns) {
        if ($content -notmatch $pattern) {
            $failures.Add("$Path does not satisfy contract: $pattern")
        }
    }
}

Assert-FileContains '.github/workflows/delivery.yml' @(
    'packages:\s*write',
    'id-token:\s*write',
    'docker/build-push-action',
    'push:\s*true',
    'sbom',
    'trivy',
    'exit-code:\s*["'']?1',
    'attest-build-provenance',
    'environment:\s*staging',
    'environment:\s*production',
    'run-migration',
    'smoke-test',
    'rollback-metadata'
)

Assert-FileContains 'deploy/kubernetes/api.yaml' @(
    'kind:\s*Deployment',
    'IMAGE_REFERENCE',
    'maxUnavailable:\s*0',
    'readinessProbe:',
    'livenessProbe:',
    'runAsNonRoot:\s*true',
    'readOnlyRootFilesystem:\s*true',
    'secretRef:'
)

Assert-FileContains 'deploy/kubernetes/migration-job.yaml' @(
    'kind:\s*Job',
    'backoffLimit:\s*0',
    '--migrate',
    'IMAGE_REFERENCE',
    'secretRef:'
)

Assert-FileContains 'deploy/scripts/render-manifests.ps1' @(
    'sha256:',
    'IMAGE_REFERENCE',
    'throw'
)

Assert-FileContains 'deploy/scripts/new-rollback-metadata.ps1' @(
    'PreviousImage',
    'TargetImage',
    'BackupReference',
    'Migration',
    'throw'
)

Assert-FileContains 'deploy/scripts/smoke-test.ps1' @(
    '/health/live',
    '/health/ready',
    'throw'
)

Assert-FileContains 'src/RestaurantMenu.Api/Program.cs' @(
    'args\.Contains',
    '"--migrate"',
    'ApplyDatabaseMigrationsAsync',
    'if \(migrationOnly\)[\s\S]*return;'
)

Assert-FileContains 'docs/runbooks/deployment.md' @(
    'rollback',
    'roll forward',
    'migration',
    'approval'
)

Assert-FileContains 'docs/runbooks/backup-and-restore.md' @(
    'point-in-time',
    'encrypted',
    'restore drill',
    'checksum',
    'BACKUP_REFERENCE'
)

$renderScript = Join-Path $repositoryRoot 'deploy/scripts/render-manifests.ps1'
$metadataScript = Join-Path $repositoryRoot 'deploy/scripts/new-rollback-metadata.ps1'
$temporaryDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ("restaurant-menu-delivery-" + [guid]::NewGuid())
$digestA = 'ghcr.io/example/restaurant-menu@sha256:' + ('a' * 64)
$digestB = 'ghcr.io/example/restaurant-menu@sha256:' + ('b' * 64)

try {
    if ((Test-Path -LiteralPath $renderScript) -and
        (Test-Path -LiteralPath $metadataScript)) {
        & $renderScript -ImageReference $digestB -Namespace 'restaurant-menu-staging' -OutputDirectory $temporaryDirectory | Out-Null
        $renderedApi = Get-Content -LiteralPath (Join-Path $temporaryDirectory 'api.yaml') -Raw
        if ($renderedApi -notmatch [regex]::Escape($digestB) -or
            $renderedApi -match 'IMAGE_REFERENCE|DEPLOYMENT_NAMESPACE') {
            $failures.Add('Manifest rendering did not produce resolved, digest-pinned output.')
        }

        try {
            & $renderScript -ImageReference 'ghcr.io/example/restaurant-menu:latest' -Namespace 'restaurant-menu-staging' -OutputDirectory $temporaryDirectory | Out-Null
            $failures.Add('Manifest rendering accepted a mutable image tag.')
        }
        catch {
            # Expected: deployments must be pinned by digest.
        }

        $metadataPath = Join-Path $temporaryDirectory 'rollback.json'
        & $metadataScript -Environment staging -PreviousImage $digestA -TargetImage $digestB -BackupReference 'pitr:2026-09-10T12:00:00Z' -Migration 'release-sha' -OutputPath $metadataPath | Out-Null
        $metadata = Get-Content -LiteralPath $metadataPath -Raw | ConvertFrom-Json
        if ($metadata.previousImage -ne $digestA -or
            $metadata.targetImage -ne $digestB -or
            $metadata.backupReference -ne 'pitr:2026-09-10T12:00:00Z') {
            $failures.Add('Rollback metadata did not preserve the required recovery inputs.')
        }

        try {
            & $metadataScript -Environment staging -PreviousImage $digestA -TargetImage $digestB -BackupReference '' -Migration 'release-sha' -OutputPath $metadataPath | Out-Null
            $failures.Add('Rollback metadata accepted a missing backup reference.')
        }
        catch {
            # Expected: migration must be blocked without recovery evidence.
        }
    }
}
finally {
    if (Test-Path -LiteralPath $temporaryDirectory) {
        [System.IO.Directory]::Delete($temporaryDirectory, $true)
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ -ErrorAction Continue }
    throw "Delivery contract failed with $($failures.Count) error(s)."
}

Write-Output 'Delivery contract passed.'

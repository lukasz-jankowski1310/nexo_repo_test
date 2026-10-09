param(
    [string]$PackagePath = 'artifacts/module.zip'
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$package = [IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $PackagePath).Path)
try {
    $marker = $package.GetEntry('##Dll')
    if (-not $marker) {
        throw 'Module ZIP must contain ##Dll at its root.'
    }

    $reader = [IO.StreamReader]::new($marker.Open())
    try {
        $assemblies = @($reader.ReadToEnd() -split '\r?\n' | ForEach-Object { $_.Trim() } | Where-Object { $_ })
    }
    finally {
        $reader.Dispose()
    }

    if ($assemblies.Count -ne 1 -or $assemblies[0] -notmatch '^[^/\\]+\.dll$' -or -not $package.GetEntry($assemblies[0])) {
        throw '##Dll must name exactly one existing module DLL at the root of the ZIP.'
    }

    foreach ($entry in $package.Entries) {
        if ($entry.Name -match '^(Zapqio\.Runner\.Module\.Core|LocalRunner|.*\.Tests|testhost|xunit.*)\.dll$') {
            throw "A host or test assembly was included in the module ZIP: $($entry.FullName)"
        }

        if ($entry.FullName -match '^(tests|tools|examples)/' -or $entry.FullName -match '\.(cs|csproj)$') {
            throw "Development files were included in the module ZIP: $($entry.FullName)"
        }
    }

    Write-Host "PASS module ZIP: $($assemblies[0]); no host, test or source files."
}
finally {
    $package.Dispose()
}

$files = Get-ChildItem -Path 'Script' -Recurse -Filter '*.cs'
foreach ($f in $files) {
    $content = Get-Content $f.FullName -Raw
    $len = $content.Length
    if ($len -le 1) {
        Write-Host "EMPTY   : $($f.Name)"
    }
    elseif ($content -match 'namespace\s+\S+') {
        Write-Host "HAS_NS  : $($f.Name)"
    }
    else {
        Write-Host "CLEAN   : $($f.Name)"
    }
}
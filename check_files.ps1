$files = Get-ChildItem -Path 'Script' -Recurse -Filter '*.cs'
foreach ($f in $files) {
    $count = (Get-Content $f.FullName).Count
    if ($count -le 1) {
        Write-Host "EMPTY: $($f.FullName)"
    } else {
        Write-Host "OK: $($f.Name) ($count lines)"
    }
}
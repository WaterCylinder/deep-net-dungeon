$files = Get-ChildItem -Path 'Script' -Recurse -Filter '*.cs'

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    if ($content -notmatch 'namespace\s+\S+') {
        continue
    }
    
    $lines = Get-Content $file.FullName
    $newLines = @()
    $inNamespace = $false
    $braceDepth = 0
    $foundOpeningBrace = $false
    
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        
        if ($line -match '^\s*namespace\s+\S+') {
            $inNamespace = $true
            $foundOpeningBrace = $false
            # Check if opening brace is on same line
            if ($line -match '\{') {
                $foundOpeningBrace = $true
                $braceDepth = 1
            }
            continue
        }
        
        if ($inNamespace -and -not $foundOpeningBrace) {
            if ($line -match '^\s*\{') {
                $foundOpeningBrace = $true
                $braceDepth = 1
                continue
            }
            # Skip lines between namespace and opening brace (shouldn't happen normally)
            continue
        }
        
        if ($inNamespace) {
            # Count braces to track depth
            $openBraces = ($line.ToCharArray() | Where-Object { $_ -eq '{' }).Count
            $closeBraces = ($line.ToCharArray() | Where-Object { $_ -eq '}' }).Count
            $braceDepth += $openBraces
            $braceDepth -= $closeBraces
            
            if ($braceDepth -le 0) {
                # We've closed the namespace - don't add the closing brace
                continue
            }
            
            # Remove one level of indentation (4 spaces)
            $newLine = $line -replace '^\s{4}', ''
            $newLines.Add($newLine)
        } else {
            $newLines.Add($line)
        }
    }
    
    # Remove trailing empty lines at the start and end
    while ($newLines.Count -gt 0 -and $newLines[0].Trim() -eq '') {
        $newLines.RemoveAt(0)
    }
    while ($newLines.Count -gt 0 -and $newLines[-1].Trim() -eq '') {
        $newLines.RemoveAt($newLines.Count - 1)
    }
    
    $newContent = ($newLines -join "`r`n") + "`r`n"
    [System.IO.File]::WriteAllText($file.FullName, $newContent, [System.Text.UTF8Encoding]::new($false))
    Write-Host "Removed namespace from: $($file.Name)"
}

Write-Host "Done! Processed $($files.Count) files."
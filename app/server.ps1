# Budget Buddy - tiny local helper.
# Serves budget.html and saves your data to ..\data\budget-data.json (on the USB drive).
# Only listens on localhost; nothing leaves this computer.

$ErrorActionPreference = 'Stop'
$AppDir    = Split-Path -Parent $MyInvocation.MyCommand.Path
$Root      = Split-Path -Parent $AppDir
$DataDir   = if ($env:BUDGET_DATA_DIR) { $env:BUDGET_DATA_DIR } else { Join-Path $Root 'data' }
$BackupDir = Join-Path $DataDir 'backups'
$DataFile  = Join-Path $DataDir 'budget-data.json'
$HtmlFile  = Join-Path $AppDir 'budget.html'
$StmtDir   = Join-Path $DataDir 'statements'
$Utf8      = New-Object System.Text.UTF8Encoding $false

New-Item -ItemType Directory -Force -Path $BackupDir | Out-Null

$listener = $null
foreach ($port in 8765..8790) {
    $l = New-Object System.Net.HttpListener
    $l.Prefixes.Add("http://localhost:$port/")
    try { $l.Start(); $listener = $l; break } catch { $l.Close() }
}
if (-not $listener) {
    Write-Host 'Could not start Budget Buddy (no free port between 8765 and 8790).' -ForegroundColor Red
    Read-Host 'Press Enter to close'
    exit 1
}

$url = "http://localhost:$port/"
$Host.UI.RawUI.WindowTitle = 'Budget Buddy - keep this window open'
Write-Host ''
Write-Host '  Budget Buddy is running.' -ForegroundColor Green
Write-Host "  Address:   $url"
Write-Host "  Data file: $DataFile"
Write-Host ''
Write-Host '  Keep this window open while you use the app.' -ForegroundColor Yellow
Write-Host '  Close this window when you are done (your data is saved automatically).'
Write-Host ''
if (-not $env:BUDGET_NO_BROWSER) { Start-Process $url }

function Send-Response($ctx, [int]$code, [string]$type, [byte[]]$bytes) {
    $res = $ctx.Response
    $res.StatusCode = $code
    $res.ContentType = $type
    $res.Headers.Add('Cache-Control', 'no-store')
    $res.ContentLength64 = $bytes.Length
    if ($bytes.Length -gt 0) { $res.OutputStream.Write($bytes, 0, $bytes.Length) }
    $res.OutputStream.Close()
}
function Send-Text($ctx, [int]$code, [string]$type, [string]$text) {
    Send-Response $ctx $code $type ($Utf8.GetBytes($text))
}

while ($listener.IsListening) {
    try { $ctx = $listener.GetContext() } catch { break }
    try {
        $req  = $ctx.Request
        $path = $req.Url.AbsolutePath
        $method = $req.HttpMethod

        if ($method -eq 'GET' -and ($path -eq '/' -or $path -eq '/index.html' -or $path -eq '/budget.html')) {
            Send-Response $ctx 200 'text/html; charset=utf-8' ([System.IO.File]::ReadAllBytes($HtmlFile))
        }
        elseif ($method -eq 'GET' -and $path -eq '/api/data') {
            if (Test-Path $DataFile) {
                Send-Response $ctx 200 'application/json; charset=utf-8' ([System.IO.File]::ReadAllBytes($DataFile))
            } else {
                Send-Text $ctx 200 'application/json; charset=utf-8' ''
            }
        }
        elseif ($method -eq 'GET' -and $path -eq '/api/info') {
            $info = @{ dataFile = $DataFile; backups = $BackupDir } | ConvertTo-Json -Compress
            Send-Text $ctx 200 'application/json; charset=utf-8' $info
        }
        elseif ($method -eq 'POST' -and $path -eq '/api/data') {
            # Custom header blocks other websites from writing here (forces a CORS preflight we never approve).
            if ($req.Headers['X-Budget'] -ne '1') { Send-Text $ctx 403 'text/plain' 'forbidden'; continue }
            $reader = New-Object System.IO.StreamReader($req.InputStream, $Utf8)
            $body = $reader.ReadToEnd()
            $reader.Close()
            if (-not $body.TrimStart().StartsWith('{')) { Send-Text $ctx 400 'text/plain' 'bad data'; continue }

            # One backup per day, keep the newest 30.
            $backup = Join-Path $BackupDir ('budget-data-' + (Get-Date -Format 'yyyy-MM-dd') + '.json')
            if ((Test-Path $DataFile) -and -not (Test-Path $backup)) {
                Copy-Item $DataFile $backup
                Get-ChildItem $BackupDir -Filter 'budget-data-*.json' | Sort-Object Name -Descending |
                    Select-Object -Skip 30 | Remove-Item -Force
            }

            # Write to a temp file first so a yanked drive can't leave a half-written data file.
            $tmp = "$DataFile.tmp"
            [System.IO.File]::WriteAllText($tmp, $body, $Utf8)
            Copy-Item $tmp $DataFile -Force
            Remove-Item $tmp -Force
            Send-Text $ctx 200 'text/plain' 'ok'
        }
        elseif ($path -eq '/api/statement' -or $path -eq '/api/statement-delete') {
            # Statement archive: data\statements\YYYY-MM\<file>. Paths are validated so nothing escapes that folder.
            if ($method -eq 'POST' -and $req.Headers['X-Budget'] -ne '1') { Send-Text $ctx 403 'text/plain' 'forbidden'; continue }
            if ($method -eq 'POST' -and $path -eq '/api/statement') {
                $month = [string]$req.QueryString['month']
                if ($month -notmatch '^\d{4}-\d{2}$') { Send-Text $ctx 400 'text/plain' 'bad month'; continue }
                $name = ([string]$req.QueryString['name'] -replace '[^\w.\- ()]', '_').Trim(' .')
                if (-not $name) { $name = 'statement.csv' }
                $dir = Join-Path $StmtDir $month
                New-Item -ItemType Directory -Force -Path $dir | Out-Null
                $base = [IO.Path]::GetFileNameWithoutExtension($name); $ext = [IO.Path]::GetExtension($name)
                $file = Join-Path $dir $name; $n = 2
                while (Test-Path -LiteralPath $file) { $file = Join-Path $dir "$base ($n)$ext"; $n++ }
                $reader = New-Object System.IO.StreamReader($req.InputStream, $Utf8)
                $body = $reader.ReadToEnd(); $reader.Close()
                [System.IO.File]::WriteAllText($file, $body, $Utf8)
                Send-Text $ctx 200 'text/plain' ($month + '/' + (Split-Path $file -Leaf))
                continue
            }
            $rel = [string]$req.QueryString['path']
            if ($rel -notmatch '^\d{4}-\d{2}/[^/\\:*?"<>|]+$' -or $rel.Contains('..')) { Send-Text $ctx 400 'text/plain' 'bad path'; continue }
            $full = Join-Path $StmtDir ($rel -replace '/', '\')
            if (-not (Test-Path -LiteralPath $full)) { Send-Text $ctx 404 'text/plain' 'not found'; continue }
            if ($method -eq 'GET' -and $path -eq '/api/statement') { Send-Response $ctx 200 'text/plain; charset=utf-8' ([System.IO.File]::ReadAllBytes($full)) }
            elseif ($method -eq 'POST') { Remove-Item -LiteralPath $full -Force; Send-Text $ctx 200 'text/plain' 'ok' }
            else { Send-Text $ctx 405 'text/plain' 'method' }
        }
        elseif ($method -eq 'POST' -and $path -eq '/api/purge-backups') {
            # After a password is set: remove backups that were saved before it (not encrypted).
            if ($req.Headers['X-Budget'] -ne '1') { Send-Text $ctx 403 'text/plain' 'forbidden'; continue }
            $old = @(Get-ChildItem $BackupDir -Filter '*.json' | Where-Object { -not (Select-String -Path $_.FullName -Pattern 'pocketBudgetEncrypted' -SimpleMatch -Quiet) })
            $old | Remove-Item -Force
            Send-Text $ctx 200 'text/plain' ([string]$old.Count)
        }
        elseif ($method -eq 'POST' -and $path -eq '/api/quit') {
            if ($req.Headers['X-Budget'] -ne '1') { Send-Text $ctx 403 'text/plain' 'forbidden'; continue }
            Send-Text $ctx 200 'text/plain' 'bye'
            Write-Host '  Closed from the app. Safe to remove the drive.' -ForegroundColor Green
            $listener.Stop()
            break
        }
        else {
            Send-Text $ctx 404 'text/plain' 'not found'
        }
    } catch {
        Write-Host ("  Error: " + $_.Exception.Message) -ForegroundColor Red
        try { Send-Text $ctx 500 'text/plain' $_.Exception.Message } catch {}
    }
}

$BASE_URL = "http://localhost:5066"

$NORMAL_ENDPOINTS = @(
    "/api/products",
    "/api/products/1",
    "/api/products/2",
    "/api/products/3",
    "/api/products/search?categoryId=1",
    "/api/products/search?categoryId=2",
    "/api/products/search?inStockOnly=true",
    "/api/products/search?categoryId=3&inStockOnly=true"
)

$SLOW_ENDPOINTS = @(
    "/api/products/search?query=a",
    "/api/products/search?minPrice=0&maxPrice=9999",
    "/api/products/search?query=e&inStockOnly=true"
)

function Invoke-Request {
    param($Path, $Label)
    $url = "$BASE_URL$Path"
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $response = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 30 -ErrorAction Stop
        $stopwatch.Stop()
        $ms = $stopwatch.ElapsedMilliseconds
        if ($ms -gt 1000) {
            Write-Host "  [SLOW] $Label => $($response.StatusCode) in ${ms}ms  $Path" -ForegroundColor Yellow
        } else {
            Write-Host "  [OK]   $Label => $($response.StatusCode) in ${ms}ms  $Path" -ForegroundColor Green
        }
    } catch {
        $stopwatch.Stop()
        $ms = $stopwatch.ElapsedMilliseconds
        Write-Host "  [ERR]  $Label => $($_.Exception.Message) in ${ms}ms  $Path" -ForegroundColor Red
    }
}

function Run-NormalTraffic {
    Write-Host "`n>>> PHASE 1: Normal Traffic (90 seconds)" -ForegroundColor Cyan
    Write-Host "  Simulating steady baseline traffic at ~1-2 req/sec..." -ForegroundColor Gray
    $endTime = (Get-Date).AddSeconds(90)
    $requestNum = 0
    while ((Get-Date) -lt $endTime) {
        $requestNum++
        $endpoint = $NORMAL_ENDPOINTS | Get-Random
        Invoke-Request -Path $endpoint -Label "REQ-$requestNum"
        $delay = Get-Random -Minimum 500 -Maximum 1500
        Start-Sleep -Milliseconds $delay
    }
    Write-Host "  Phase 1 complete. $requestNum requests sent." -ForegroundColor Gray
}

function Run-SlowQueries {
    Write-Host "`n>>> PHASE 2: Slow Database Queries (90 seconds)" -ForegroundColor Cyan
    Write-Host "  Sending expensive queries mixed with normal traffic..." -ForegroundColor Gray
    $endTime = (Get-Date).AddSeconds(90)
    $requestNum = 0
    while ((Get-Date) -lt $endTime) {
        $requestNum++
        $roll = Get-Random -Minimum 1 -Maximum 100
        if ($roll -le 60) {
            $endpoint = $SLOW_ENDPOINTS | Get-Random
            Invoke-Request -Path $endpoint -Label "SLOW-$requestNum"
            Start-Sleep -Milliseconds (Get-Random -Minimum 800 -Maximum 2000)
        } else {
            $endpoint = $NORMAL_ENDPOINTS | Get-Random
            Invoke-Request -Path $endpoint -Label "REQ-$requestNum"
            Start-Sleep -Milliseconds (Get-Random -Minimum 300 -Maximum 800)
        }
    }
    Write-Host "  Phase 2 complete. $requestNum requests sent." -ForegroundColor Gray
}

function Run-TrafficSpike {
    Write-Host "`n>>> PHASE 3: Traffic Spike (90 seconds)" -ForegroundColor Cyan
    Write-Host "  Firing bursts of 10 concurrent requests..." -ForegroundColor Gray
    $endTime = (Get-Date).AddSeconds(90)
    $burstNum = 0
    while ((Get-Date) -lt $endTime) {
        $burstNum++
        Write-Host "  --- Burst $burstNum ---" -ForegroundColor Gray
        $jobs = @()
        for ($i = 1; $i -le 10; $i++) {
            $endpoint = $NORMAL_ENDPOINTS | Get-Random
            $url = "$BASE_URL$endpoint"
            $jobs += Start-Job -ScriptBlock {
                param($u)
                try {
                    $sw = [System.Diagnostics.Stopwatch]::StartNew()
                    $r = Invoke-WebRequest -Uri $u -UseBasicParsing -TimeoutSec 30 -ErrorAction Stop
                    $sw.Stop()
                    "$($r.StatusCode) in $($sw.ElapsedMilliseconds)ms => $u"
                } catch {
                    "ERR $($_.Exception.Message) => $u"
                }
            } -ArgumentList $url
        }
        $jobs | Wait-Job | ForEach-Object {
            $result = Receive-Job $_
            if ($result -match "^2") {
                Write-Host "  [OK]   $result" -ForegroundColor Green
            } else {
                Write-Host "  [ERR]  $result" -ForegroundColor Red
            }
            Remove-Job $_
        }
        $pause = Get-Random -Minimum 2000 -Maximum 4000
        Write-Host "  Next burst in $($pause)ms..." -ForegroundColor Gray
        Start-Sleep -Milliseconds $pause
    }
    Write-Host "  Phase 3 complete. $burstNum bursts sent." -ForegroundColor Gray
}

Write-Host "=====================================================" -ForegroundColor Magenta
Write-Host "  McpDemo.Api Traffic Simulator" -ForegroundColor Magenta
Write-Host "  Target: $BASE_URL" -ForegroundColor Magenta
Write-Host "  Total duration: ~5 minutes" -ForegroundColor Magenta
Write-Host "=====================================================" -ForegroundColor Magenta

Write-Host "`n  Checking API is reachable..." -ForegroundColor Gray
try {
    Invoke-WebRequest -Uri "$BASE_URL/swagger" -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop | Out-Null
    Write-Host "  API is reachable at $BASE_URL" -ForegroundColor Green
} catch {
    Write-Host "`n[ERROR] Cannot reach $BASE_URL - make sure dotnet run is active" -ForegroundColor Red
    exit 1
}

Run-NormalTraffic
Run-SlowQueries
Run-TrafficSpike

Write-Host "`n=====================================================" -ForegroundColor Magenta
Write-Host "  Simulation complete!" -ForegroundColor Magenta
Write-Host "  Now try in Claude Code:" -ForegroundColor Magenta
Write-Host "  'Using the Elasticsearch MCP, analyse traffic" -ForegroundColor Magenta
Write-Host "   patterns for McpDemo.Api over the last 10 mins'" -ForegroundColor Magenta
Write-Host "=====================================================" -ForegroundColor Magenta

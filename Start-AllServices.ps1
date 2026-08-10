$rootDir = $PSScriptRoot
if ([string]::IsNullOrEmpty($rootDir)) {
    $rootDir = "C:\Users\sagar\Desktop\CAP_PROJ"
}

$services = @(
    @{ Name = "FlightService";     Path = Join-Path $rootDir "Services\FlightService";     Port = 5002 },
    @{ Name = "BookingService";    Path = Join-Path $rootDir "Services\BookingService";    Port = 5003 },
    @{ Name = "PaymentService";    Path = Join-Path $rootDir "Services\PaymentService";    Port = 5004 },
    @{ Name = "CheckInService";    Path = Join-Path $rootDir "Services\CheckInService";    Port = 5005 },
    @{ Name = "BaggageService";    Path = Join-Path $rootDir "Services\BaggageService";    Port = 5006 },
    @{ Name = "PassengerService";  Path = Join-Path $rootDir "Services\PassengerService";  Port = 5007 },
    @{ Name = "AdminService";      Path = Join-Path $rootDir "Services\AdminService";      Port = 5010 },
    @{ Name = "StaffService";      Path = Join-Path $rootDir "Services\StaffService";      Port = 5011 },
    @{ Name = "ApiGateway";        Path = Join-Path $rootDir "ApiGateway";                 Port = 5000 }
)

$wtCmd = Get-Command wt -ErrorAction SilentlyContinue

if ($wtCmd) {
    Write-Host "Opening all services in Windows Terminal (all CMD tabs in 1 window)..." -ForegroundColor Cyan
    $wtArgs = @()
    $isFirst = $true

    foreach ($svc in $services) {
        if (Test-Path $svc.Path) {
            if (-not $isFirst) {
                $wtArgs += ";"
                $wtArgs += "new-tab"
            }
            $isFirst = $false

            $wtArgs += "-p"
            $wtArgs += "Command Prompt"
            $wtArgs += "--title"
            $wtArgs += "$($svc.Name) [:$($svc.Port)]"
            $wtArgs += "-d"
            $wtArgs += $svc.Path
            $wtArgs += "cmd"
            $wtArgs += "/k"
            $wtArgs += "dotnet run"

            Write-Host "  Added tab for: $($svc.Name) -> http://localhost:$($svc.Port)" -ForegroundColor Green
        } else {
            Write-Host "  Skipping: $($svc.Name) (Folder not found)" -ForegroundColor Yellow
        }
    }

    Start-Process -FilePath "wt.exe" -ArgumentList $wtArgs
    Write-Host "`nAll service tabs opened in 1 Windows Terminal window!" -ForegroundColor Cyan
    Write-Host "API Gateway Swagger: http://localhost:5000/swagger`n" -ForegroundColor Cyan
} else {
    Write-Host "Windows Terminal (wt.exe) not found. Opening individual CMD windows..." -ForegroundColor Yellow
    foreach ($svc in $services) {
        if (Test-Path $svc.Path) {
            Write-Host "  Starting $($svc.Name) in CMD window..." -ForegroundColor Green
            Start-Process cmd -ArgumentList "/k", "title $($svc.Name) [:$($svc.Port)] && cd /d `"$($svc.Path)`" && dotnet run"
            Start-Sleep -Milliseconds 800
        }
    }
}



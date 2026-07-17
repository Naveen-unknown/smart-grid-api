$loginReq = @{ usernameOrEmail = "admin"; password = "Admin@123" } | ConvertTo-Json
$loginResp = Invoke-WebRequest -Method Post -Uri "https://smart-grid-api-z8wk.onrender.com/api/auth/login" -Body $loginReq -ContentType "application/json"
$token = ($loginResp.Content | ConvertFrom-Json).data.token

for ($node = 1; $node -le 5; $node++) {
    for ($i = 1; $i -le 3; $i++) {
        $reading = @{
            NodeId = $node
            UserId = 1
            Consumption = (Get-Random -Minimum 50 -Maximum 300)
            Production = (Get-Random -Minimum 0 -Maximum 50)
            Voltage = (Get-Random -Minimum 215 -Maximum 235)
            Current = (Get-Random -Minimum 10 -Maximum 45)
            PowerFactor = 0.95
            Frequency = 50.0
        } | ConvertTo-Json

        Invoke-WebRequest -Method Post -Uri "https://smart-grid-api-z8wk.onrender.com/api/energy/readings" -Headers @{"Authorization" = "Bearer $token"} -Body $reading -ContentType "application/json" | Out-Null
        Write-Output "Node $node seeded reading $i."
    }
}
Write-Output "Done!"

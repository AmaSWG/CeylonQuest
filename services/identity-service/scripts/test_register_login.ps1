$reg = @{ 
  firstName='E2'
  lastName='Test'
  email='e2test@example.com'
  phoneNumber='123456'
  nationality='LK'
  password='Password123!'
  confirmPassword='Password123!'
  registrationType='Visitor'
}
$regJson = $reg | ConvertTo-Json -Depth 5
try {
  $r = Invoke-RestMethod -Uri 'http://localhost:5000/api/auth/register' -Method Post -Body $regJson -ContentType 'application/json'
  Write-Output 'REGISTER_OK:'
  $r | ConvertTo-Json -Depth 5 | Write-Output
} catch {
  Write-Output 'REGISTER_FAILED:'
  Write-Output $_.Exception
}
Start-Sleep -Milliseconds 300
$login = @{ email='e2test@example.com'; password='Password123!' }
$loginJson = $login | ConvertTo-Json
try {
  $l = Invoke-RestMethod -Uri 'http://localhost:5000/api/auth/login' -Method Post -Body $loginJson -ContentType 'application/json'
  Write-Output 'LOGIN_OK:'
  $l | ConvertTo-Json -Depth 5 | Write-Output
} catch {
  Write-Output 'LOGIN_FAILED:'
  Write-Output $_.Exception
}

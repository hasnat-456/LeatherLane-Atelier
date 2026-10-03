$loginBody = @{
    email = "admin@leatherlaneatelier.store"
    password = "admin123"
} | ConvertTo-Json

$loginResponse = Invoke-RestMethod -Uri "https://leatherlaneatelier.store/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
$token = $loginResponse.token

Write-Host "Got token: $token"

$boundary = [System.Guid]::NewGuid().ToString()
$imageBytes = [System.Text.Encoding]::UTF8.GetBytes("fake image content")

$bodyLines = @(
    "--$boundary",
    "Content-Disposition: form-data; name=`"imageFile`"; filename=`"test.jpg`"",
    "Content-Type: image/jpeg",
    "",
    "fake image content",
    "--$boundary--"
)
$bodyString = $bodyLines -join "`r`n"
$bodyBytes = [System.Text.Encoding]::UTF8.GetBytes($bodyString)

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "multipart/form-data; boundary=$boundary"
}

try {
    $uploadResponse = Invoke-RestMethod -Uri "https://leatherlaneatelier.store/api/adminapi/slider-image" -Method Post -Headers $headers -Body $bodyBytes
    Write-Host "Upload success:"
    $uploadResponse | ConvertTo-Json
} catch {
    Write-Host "Upload failed: $_"
    $reader = new-object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
    Write-Host $reader.ReadToEnd()
}

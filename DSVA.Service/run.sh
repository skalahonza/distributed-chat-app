# Node 0 = default

# Node 1
$export ASPNETCORE_URLS="https://*:5002"
$export Node__address="https://localhost:5002"
$export Node__next="https://localhost:5001"
$export Node__id=1
dotnet .\DSVA.Service.dll

# Node 2
$export ASPNETCORE_URLS="https://*:5003"
$export Node__address="https://localhost:5003"
$export Node__next="https://localhost:5001"
$export Node__id=2
dotnet .\DSVA.Service.dll

# Node 3
$export ASPNETCORE_URLS="https://*:5004"
$export Node__address="https://localhost:5004"
$export Node__next="https://localhost:5001"
$export Node__id=3
dotnet .\DSVA.Service.dll

# Node 4
$export ASPNETCORE_URLS="https://*:5005"
$export Node__address="https://localhost:5005"
$export Node__next="https://localhost:5001"
$export Node__id=4
dotnet .\DSVA.Service.dll
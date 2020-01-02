# Node 0 = default

# Node 1
$env:ASPNETCORE_URLS="https://*:5002"
$env:Node:address="https://localhost:5002"
$env:Node:next="https://localhost:5001"
$env:Node:id=1

# Node 2
$env:ASPNETCORE_URLS="https://*:5003"
$env:Node:address="https://localhost:5003"
$env:Node:next="https://localhost:5001"
$env:Node:id=2

# Node 3
$env:ASPNETCORE_URLS="https://*:5004"
$env:Node:address="https://localhost:5004"
$env:Node:next="https://localhost:5001"
$env:Node:id=3

# Node 4
$env:ASPNETCORE_URLS="https://*:5005"
$env:Node:address="https://localhost:5005"
$env:Node:next="https://localhost:5001"
$env:Node:id=4
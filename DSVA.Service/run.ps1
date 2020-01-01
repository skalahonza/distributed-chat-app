$env:ASPNETCORE_URLS="https://*:5002"
$env:Node:address="https://localhost:5002"
$env:Node:next="https://localhost:5001"
$env:Node:nextId=0
$env:Node:neighboursCount=2
$env:Node:id=1

dotnet run

$env:ASPNETCORE_URLS="https://*:5003"
$env:Node:address="https://localhost:5003"
$env:Node:next="https://localhost:5001"
$env:Node:nextId=0
$env:Node:nextNext="https://localhost:5002"
$env:Node:nextNextId=1
$env:Node:neighboursCount=3
$env:Node:id=2
﻿# Node 0 = default

# Node 1
$env:ASPNETCORE_URLS="https://*:5002"
$env:Node:address="https://localhost:5002"
$env:Node:next="https://localhost:5001"
$env:Node:id=1

# Node 2
$env:ASPNETCORE_URLS="https://*:5003"
$env:Node:address="https://localhost:5003"
$env:Node:next="https://localhost:5001"
$env:Node:nextNext="https://localhost:5002"
$env:Node:id=2
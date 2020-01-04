# Chat app
* Send message
* Receive message
* Sign in
* Sign out
* Message history (ordered, confirmed journal)
* Detect disconnect

## Topology and communication
* One way ring (with extra fallback node)
* Chang Roberts
* gRPC

## Task
* [x] Programy musí podporovat interaktivní i dávkové řízení (např. přidání a odebrání procesu).
* [x] Srozumitelné výpisy logujte na konzoli i do souboru/ů.
* [x] Výpisy budou opatřeny časovým razítkem logického času.
* [x] Každý uzel bude mít jednoznačnou identifikaci.
* [x] Zprávy bude vždy možné v rámci celého systému úplně uspořádat

## Vagrant configuration
* Five nodes
* Private network
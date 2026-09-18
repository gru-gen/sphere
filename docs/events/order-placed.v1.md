## Payload

| field | type | notes |
|---|---|---|
| `eventId` | uuid | unique per event — dedupe key for rigorous consumers |
| `orderId` | uuid | the order that now exists |
| `customerId` | uuid | who placed it |
| `total` | number | order total, two decimals |
| `currency` | string(3) | ISO 4217, `EUR` today |
| `placedAtUtc` | date-time | producer clock, UTC |
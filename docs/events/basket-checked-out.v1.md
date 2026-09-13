# Event: shopsphere.basket.checked-out.v1

Payload (JSON, camelCase):

| Field | Type | Notes |
|---|---|---|
| eventId | GUID (UUIDv7) | unique per fact; consumers dedupe on it |
| checkoutId | GUID (UUIDv7) | the id the resulting order carries; chosen at checkout, promised in the 202 reply |
| customerId | GUID | also the message key |
| lines[].productId | GUID | contents at the moment of checkout |
| lines[].quantity | int | |
| occurredAtUtc | ISO-8601 offset | producer clock |
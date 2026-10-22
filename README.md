# Nopwara Webhooks for nopCommerce

Signed webhooks for nopCommerce. Connect your store to Zapier, Make, n8n, an ERP or any HTTP endpoint without writing a plugin.

## Events

| Event | When | `data` |
|---|---|---|
| `order.placed` | An order is placed | order (status, totals, currency, items with sku/name/qty/prices) |
| `order.paid` | An order is marked paid | order |
| `order.status_changed` | The order status changes | `{ previousOrderStatus, order }` |
| `order.refunded` | Refund (full or partial) | `{ amount, order }` |
| `customer.registered` | A customer registers | id, guid, email, username, names |
| `product.updated` | A product is saved (admin, import, stock changes…) | id, name, sku, price, stock, published |
| `product.stock_changed` | A stock quantity history row is written | productId, combinationId, warehouseId, adjustment, new quantity, message |
| `shipment.sent` | A shipment is marked shipped | id, orderId, tracking number |
| `ping` | "Send test" in admin | message |

Every request is a `POST` with a JSON body:

```json
{ "id": "6f1c…", "type": "order.placed", "createdUtc": "2026-10-22T10:00:00Z", "storeId": 1, "data": { … } }
```

Headers:
- `X-Nopwara-Event`
- `X-Nopwara-Delivery` (unique id; use it to ignore duplicates)
- `X-Nopwara-Timestamp`
- `X-Nopwara-Signature`: `sha256=` + lowercase hex HMAC-SHA256 of the **raw body** with the endpoint secret

Verify the signature, for example in Node:

```js
const expected = 'sha256=' + crypto.createHmac('sha256', secret).update(rawBody).digest('hex');
crypto.timingSafeEqual(Buffer.from(expected), Buffer.from(req.headers['x-nopwara-signature']));
```

## Delivery and retries

- **Queueing:** events are written to a delivery log (table `NopwaraWebhookDelivery`).
- **Sending:** the schedule task **Nopwara webhooks: send pending deliveries** sends them every minute. Admin also has **Send pending now**.
- **Success:** any 2xx response.
- **Retries:** otherwise the delivery is retried after 1 min, 5 min, 30 min, 2 h and 12 h, and then marked *Failed*.
- **Delivery log** (on the endpoint edit page): event, status, attempts, last response, the payload, and **Redeliver**.
- **Cleanup:** succeeded deliveries are deleted after *Keep log (days)* (default 30).

## Security

- Redirects are not followed: a 3xx response counts as a failed delivery.
- Endpoints are set by admins with the "Manage plugins" permission. Any http(s) URL is allowed, including private and localhost addresses for local testing. Only point endpoints at services you trust.

## Admin

Configuration → Webhooks:
- add endpoints: name, URL, secret (auto-generated), all events or a selection, active, store
- **Send test** (`ping`)
- the delivery log

## Install

1. Copy this folder to `src/Plugins/`, add the project to `NopCommerce.sln` and build.
2. Install **Nopwara webhooks** (Configuration → Local plugins) and restart.

## License

Copyright (C) 2026 Sohel Rana

Licensed under the [GNU Affero General Public License v3.0](LICENSE), the license nopCommerce itself is built on (the [nopCommerce Public License](https://www.nopcommerce.com/license) is AGPL v3 plus its additional terms). Parts of this project are adapted from nopCommerce source code. A store that uses it must keep nopCommerce's "Powered by nopCommerce" link, as the nopCommerce Public License requires.

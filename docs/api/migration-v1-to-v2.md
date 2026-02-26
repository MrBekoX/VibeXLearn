# API Migration Guide: V1 to V2

## Sunset Date

API V1 will be sunset **6 months from the initial deployment date**. After this date, V1 endpoints will return `410 Gone`.

## Response Headers

When using V1 endpoints, you will see the following headers in every response:

| Header | Value | Description |
|--------|-------|-------------|
| `Sunset` | RFC 7231 date | The date after which V1 will be removed |
| `Deprecation` | `true` | Indicates the endpoint is deprecated |
| `Link` | Migration guide URL | Link to this migration guide |

## Breaking Changes

### 1. Course Endpoint Response Format
- **V1**: `GET /api/v1/courses` returns a flat array
- **V2**: `GET /api/v2/courses` returns paginated result with metadata

### 2. Error Response Format
- **V1**: `{ "error": "message" }`
- **V2**: `{ "type": "ErrorType", "message": "message", "correlationId": "..." }`

### 3. Authentication
- **V1**: `Authorization: Bearer <token>`
- **V2**: Same, but `X-Api-Version: 2.0` header is recommended

## Migration Steps

1. Update API base URL from `/api/v1/` to `/api/v2/`
2. Update response parsing for paginated endpoints
3. Update error handling for the new error format
4. Test all endpoints with V2
5. Remove `/api/v1/` references from your codebase

## Support

For migration support, contact: **api@vibexlearn.com**

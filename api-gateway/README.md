# API Gateway

**Gateway:** http://localhost:5000  
**Identity Service:** http://localhost:5278

All routes are proxied via [YARP](https://microsoft.github.io/reverse-proxy/) and configured in `ApiGateway/appsettings.json`.

---

## Routes

### 🔐 Auth

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/api/auth/register` | Register a new user (Visitor or Provider) |
| `POST` | `/api/auth/login` | Log in and receive a session cookie |
| `POST` | `/api/auth/logout` | Log out (clear session) |
| `POST` | `/api/auth/forgot-password` | Send password reset email |
| `POST` | `/api/auth/reset-password` | Reset password using token |
| `POST` | `/api/auth/provider/activate` | Activate a provider account |
| `*`    | `/api/auth/{**remainder}` | Catch-all for remaining auth routes |

---

### 👤 Users

| Method | Path | Description |
|--------|------|-------------|
| `GET`  | `/api/users/me` | Get currently authenticated user profile |
| `PUT`  | `/api/users/me` | Update currently authenticated user profile |

---

### 📋 Provider Applications

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/api/provider-applications` | Submit a new provider application |
| `GET`  | `/api/provider-applications/status?email=` | Check provider application status by email |

---

### 🏪 Provider Dashboard

| Method | Path | Description |
|--------|------|-------------|
| `GET`, `PUT` | `/api/provider/info` | Get or update provider profile info |
| `GET`, `POST` | `/api/provider/timeslots` | List or create availability timeslots |
| `PUT`, `DELETE` | `/api/provider/timeslots/{id}` | Update or delete a specific timeslot |
| `GET`, `POST` | `/api/provider/prices` | List or create service prices |
| `PUT`, `DELETE` | `/api/provider/prices/{id}` | Update or delete a specific price entry |

---

### 👑 Admin

| Method | Path | Description |
|--------|------|-------------|
| `GET`  | `/api/admin/stats` | Get platform-wide statistics |
| `GET`  | `/api/admin/users` | List all users |
| `PUT`  | `/api/admin/users/{id}/status` | Enable or disable a user account |
| `GET`  | `/api/admin/provider-applications` | List all provider applications |
| `GET`  | `/api/admin/provider-applications/{id}/document` | Download application document |
| `GET`  | `/api/admin/reports` | Get admin reports (supports query string filters) |
| `*`    | `/api/admin/{**remainder}` | Catch-all for remaining admin routes |

---

## Clusters

| Cluster | Destination |
|---------|-------------|
| `identityCluster` | `http://localhost:5278/` |

---

## How to Run

1. Run the **Identity Service** (must be on `http://localhost:5278`)
2. Run the **API Gateway** (listens on `http://localhost:5000`)
3. Run the **Frontend** dev server (default: `http://localhost:5173`)

All frontend `/api/...` requests are automatically proxied through the gateway to the Identity Service.

---

## Example Requests

**Register:**
```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"firstName":"Test","lastName":"Visitor","email":"test@example.com","phoneNumber":"0771234567","nationality":"Sri Lankan","password":"Test@12345","confirmPassword":"Test@12345","registrationType":"Visitor"}'
```

**Login:**
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test@12345"}'
```

**Check provider application status:**
```bash
curl "http://localhost:5000/api/provider-applications/status?email=test@example.com"
```

**Get current user:**
```bash
curl http://localhost:5000/api/users/me \
  -H "Cookie: <your-session-cookie>"
```

---

> ⚠️ **Note:** `ProviderApplication.jsx` currently calls `http://localhost:5000/api/provider-applications` directly (hardcoded). This should be updated to use the relative path `/api/provider-applications` to be consistent with all other frontend API calls.

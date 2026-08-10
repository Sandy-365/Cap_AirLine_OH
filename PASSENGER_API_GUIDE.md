# 🛫 Passenger Service API Guide & Workflow Manual

Welcome to the **Passenger Service** technical guide. This guide explains how to authenticate, manage passenger profiles, query loyalty/reward points balances, earn & redeem reward points, and perform administrative user management.

---

## 📌 Base URLs

- **API Gateway (Recommended)**: `http://localhost:5000/api`
- **Direct Downstream Microservice**: `http://localhost:5007/api`
- **Swagger Documentation**: `http://localhost:5000/swagger/index.html`

---

## 🔒 Role-Based Access Control (RBAC) Matrix

| Endpoint | HTTP Method | Allowed Roles | Description |
| :--- | :--- | :--- | :--- |
| `/identity/passenger/login` | `POST` | **Public** (None) | Authenticate passenger credentials & obtain Bearer JWT token |
| `/identity/passenger/register` | `POST` | **Public** (None) | Register a new passenger account |
| `/identity/passenger/reset-password` | `POST` | **Public** (None) | Reset password using a valid OTP token |
| `/passengers/users` | `GET` | `Admin`, `SuperAdmin`, `HR`, `Staff` | Retrieve all passenger accounts in the system |
| `/passengers/users/{userId}` | `GET` | `Passenger` (own profile), `Admin`, `SuperAdmin`, `Staff` | Retrieve passenger profile by ID |
| `/passengers/users/{userId}/profile` | `PUT` | `Passenger` (own profile), `Admin`, `SuperAdmin` | Update passenger profile details (name, email) |
| `/passengers/users/{userId}/status` | `PUT` | `Admin`, `SuperAdmin`, `HR` | Activate or deactivate a passenger account |
| `/passengers/users/{userId}` | `DELETE` | `Admin`, `SuperAdmin` | Permanently delete a passenger profile |
| `/passengers/rewards/{userId}/balance` | `GET` | `Passenger` (own balance), `Admin`, `SuperAdmin` | Get total loyalty/reward points balance |
| `/passengers/rewards/{userId}/history` | `GET` | `Passenger` (own history), `Admin`, `SuperAdmin` | View reward transaction history |
| `/passengers/rewards/earn` | `POST` | `Admin`, `SuperAdmin`, `Staff` | Award reward points to a passenger |
| `/passengers/rewards/redeem` | `POST` | `Passenger` (own points), `Admin`, `SuperAdmin` | Deduct reward points for booking redemption |

---

## 🚀 Step-by-Step API Usage Workflow

```mermaid
flowchart TD
    A[Public User] -->|1. POST /identity/passenger/register| B(Receive Registration Confirmation)
    B -->|2. POST /identity/passenger/login| C{Receive JWT Token}
    C -->|3. Set Authorization Header: Bearer token| D[Authenticated Passenger]
    D -->|GET /passengers/users/{userId}| E[View Profile]
    D -->|PUT /passengers/users/{userId}/profile| F[Update Profile]
    D -->|GET /passengers/rewards/{userId}/balance| G[Check Reward Points]
    D -->|POST /passengers/rewards/redeem| H[Redeem Points for Booking]
    Admin[Admin / SuperAdmin / Staff] -->|POST /passengers/rewards/earn| I[Award Points to Passenger]
```

---

### Step 1: Register a New Passenger Account

**Endpoint:** `POST http://localhost:5000/api/identity/passenger/register`  
**Authentication:** Public (No token required)

#### Request Body:
```json
{
  "email": "jane.doe@example.com",
  "password": "Password123!",
  "firstName": "Jane",
  "lastName": "Doe",
  "phoneNumber": "+1234567890"
}
```

#### Response (200 OK):
```json
{
  "message": "Registration successful. Check your email for the OTP."
}
```

---

### Step 2: Authenticate & Obtain JWT Token

**Endpoint:** `POST http://localhost:5000/api/identity/passenger/login`  
**Authentication:** Public (No token required)

#### Request Body:
```json
{
  "email": "jane.doe@example.com",
  "password": "Password123!"
}
```

#### Response (200 OK):
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMDAxIiwicm9sZSI6IlBhc3NlbmdlciIsImVtYWlsIjoiamFuZS5kb2VAZXhhbXBsZS5jb20iLCJleHAiOjE3NzA0MDAwMDB9...",
  "userId": 1001,
  "email": "jane.doe@example.com",
  "firstName": "Jane",
  "lastName": "Doe",
  "role": "Passenger"
}
```

> [!TIP]
> Copy the returned `token` string and paste it into Swagger UI (**Authorize** button: `Bearer <token>`) or set it as a HTTP Header in Postman:  
> `Authorization: Bearer <token>`

---

### Step 3: View & Update Profile

#### View Own Profile
**Endpoint:** `GET http://localhost:5000/api/passengers/users/1001`  
**Authentication:** `Bearer <JWT_TOKEN>` (Passenger, Admin, Staff)

#### Response (200 OK):
```json
{
  "id": 1001,
  "email": "jane.doe@example.com",
  "firstName": "Jane",
  "lastName": "Doe",
  "phoneNumber": "+1234567890",
  "passportNumber": "A12345678",
  "dateOfBirth": "1995-05-15T00:00:00Z",
  "isActive": true
}
```

#### Update Profile Details
**Endpoint:** `PUT http://localhost:5000/api/passengers/users/1001/profile`  
**Authentication:** `Bearer <JWT_TOKEN>` (Passenger, Admin)

#### Request Body:
```json
{
  "firstName": "Jane",
  "lastName": "Smith",
  "phoneNumber": "+1987654321",
  "passportNumber": "B98765432",
  "dateOfBirth": "1995-05-15T00:00:00Z"
}
```

#### Response (200 OK):
```json
{
  "id": 1001,
  "email": "jane.doe@example.com",
  "firstName": "Jane",
  "lastName": "Smith",
  "phoneNumber": "+1987654321",
  "passportNumber": "B98765432",
  "dateOfBirth": "1995-05-15T00:00:00Z",
  "isActive": true
}
```

---

### Step 4: Loyalty & Reward Points Operations

#### Check Reward Points Balance
**Endpoint:** `GET http://localhost:5000/api/passengers/rewards/1001/balance`  
**Authentication:** `Bearer <JWT_TOKEN>` (Passenger, Admin)

#### Response (200 OK):
```json
{
  "userId": 1001,
  "totalPoints": 4500
}
```

#### View Reward Transaction History
**Endpoint:** `GET http://localhost:5000/api/passengers/rewards/1001/history`  
**Authentication:** `Bearer <JWT_TOKEN>` (Passenger, Admin)

#### Response (200 OK):
```json
[
  {
    "id": 501,
    "userId": 1001,
    "points": 5000,
    "transactionType": "Earned",
    "description": "Flight booking bonus - PNR: AB1234",
    "createdAt": "2026-08-01T10:00:00Z"
  },
  {
    "id": 502,
    "userId": 1001,
    "points": -500,
    "transactionType": "Redeemed",
    "description": "Discount applied on booking PNR: CD5678",
    "createdAt": "2026-08-05T14:30:00Z"
  }
]
```

#### Earn Reward Points (Admin/Staff Only)
**Endpoint:** `POST http://localhost:5000/api/passengers/rewards/earn`  
**Authentication:** `Bearer <JWT_TOKEN>` (Admin, SuperAdmin, Staff)

#### Request Body:
```json
{
  "userId": 1001,
  "points": 1000,
  "transactionType": "Earned",
  "bookingId": 204
}
```

#### Response (200 OK):
```json
{
  "id": 503,
  "userId": 1001,
  "points": 1000,
  "transactionType": "Earned",
  "bookingId": 204,
  "createdAt": "2026-08-06T11:00:00Z"
}
```

#### Redeem Reward Points
**Endpoint:** `POST http://localhost:5000/api/passengers/rewards/redeem`  
**Authentication:** `Bearer <JWT_TOKEN>` (Passenger, Admin)

#### Request Body:
```json
{
  "userId": 1001,
  "points": 500
}
```

#### Response (200 OK):
```json
{
  "id": 504,
  "userId": 1001,
  "points": -500,
  "transactionType": "Redeemed",
  "createdAt": "2026-08-06T11:15:00Z"
}
```

---

### Step 5: Password Reset via OTP

**Endpoint:** `POST http://localhost:5000/api/identity/passenger/reset-password`  
**Authentication:** Public (No token required)

#### Request Body:
```json
{
  "email": "jane.doe@example.com",
  "newPassword": "NewStrongPassword123!"
}
```

#### Response (200 OK):
```json
{
  "message": "Password reset successfully."
}
```

---

### Step 6: Admin Management Operations

#### List All Passengers
**Endpoint:** `GET http://localhost:5000/api/passengers/users`  
**Authentication:** `Bearer <JWT_TOKEN>` (Admin, SuperAdmin, HR, Staff)

#### Update Passenger Status (Activate/Deactivate)
**Endpoint:** `PUT http://localhost:5000/api/passengers/users/1001/status`  
**Authentication:** `Bearer <JWT_TOKEN>` (Admin, SuperAdmin, HR)

#### Request Body:
```json
{
  "isActive": false
}
```

#### Response (200 OK):
```json
{
  "message": "Status updated"
}
```

#### Delete Passenger Profile
**Endpoint:** `DELETE http://localhost:5000/api/passengers/users/1001`  
**Authentication:** `Bearer <JWT_TOKEN>` (Admin, SuperAdmin)

#### Response (200 OK):
```json
{
  "message": "User deleted"
}
```

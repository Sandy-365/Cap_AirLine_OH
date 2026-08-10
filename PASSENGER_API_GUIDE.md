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
| `/identity/passenger/forgot-password` | `POST` | **Public** (None) | Generate and email a password reset OTP token |
| `/identity/passenger/reset-password` | `POST` | **Public** (None) | Reset password using a valid OTP token |
| `/passengers/users` | `GET` | `Admin`, `SuperAdmin`, `HR`, `Staff` | Retrieve all passenger accounts in the system |
| `/passengers/users/{userId}` | `GET` | `Passenger` (own profile), `Admin`, `SuperAdmin`, `Staff` | Retrieve passenger profile by ID |
| `/passengers/users/{userId}/profile` | `PUT` | `Passenger` (own profile), `Admin`, `SuperAdmin` | Update passenger profile details (name, email) |
| `/passengers/users/{userId}/status` | `PUT` | `Admin`, `SuperAdmin`, `HR` | Activate or deactivate a passenger account |
| `/passengers/users/{userId}` | `DELETE` | `Admin`, `SuperAdmin` | Permanently delete a passenger profile |

---

## 🚀 Step-by-Step API Usage Workflow

```mermaid
flowchart TD
    A[Public User] -->|1. POST /identity/passenger/register| B(Receive Registration Confirmation)
    B -->|2. POST /identity/passenger/login| C{Receive JWT Token}
    C -->|3. Set Authorization Header: Bearer token| D[Authenticated Passenger]
    D -->|GET /passengers/users/{userId}| E[View Profile]
    D -->|PUT /passengers/users/{userId}/profile| F[Update Profile]
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

### Step 4: Password Reset via OTP

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

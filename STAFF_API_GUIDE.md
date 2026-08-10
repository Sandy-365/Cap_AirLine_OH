# 👨‍✈️ Staff Service API Guide & Workflow Manual

Welcome to the **Staff Service** technical guide. This guide details staff authentication, profile management, status updates, and administrative staff management.

---

## 📌 Base URLs

- **API Gateway (Recommended)**: `http://localhost:5000/api`
- **Direct Downstream Microservice**: `http://localhost:5011/api`
- **Swagger Documentation**: `http://localhost:5000/swagger/index.html`

---

## 🔒 Role-Based Access Control (RBAC) Matrix

| Endpoint | HTTP Method | Allowed Roles | Description |
| :--- | :--- | :--- | :--- |
| `/identity/staff/login` | `POST` | **Public** (None) | Authenticate staff credentials & obtain Bearer JWT token |
| `/identity/staff/register` | `POST` | **Public** (None) | Register a new staff account |
| `/identity/staff/reset-password` | `POST` | **Public** (None) | Reset staff password using a valid OTP token |
| `/staff/users` | `GET` | `Admin`, `SuperAdmin`, `HR` | Retrieve all staff profiles in the system |
| `/staff/users/{userId}` | `GET` | `Staff` (own profile), `Admin`, `SuperAdmin`, `HR` | Retrieve staff profile by ID |
| `/staff/users/{userId}/profile` | `PUT` | `Staff` (own profile), `Admin`, `SuperAdmin`, `HR` | Update staff name and email |
| `/staff/users/{userId}/status` | `PUT` | `Admin`, `SuperAdmin`, `HR` | Activate or deactivate a staff account |
| `/staff/users/{userId}` | `DELETE` | `Admin`, `SuperAdmin`, `HR` | Permanently delete a staff profile |

---

## 🚀 Step-by-Step API Usage Workflow

```mermaid
flowchart TD
    A[Public User / Staff Member] -->|1. POST /identity/staff/register| B(Receive Registration Confirmation)
    B -->|2. POST /identity/staff/login| C{Receive JWT Token}
    C -->|3. Set Authorization Header: Bearer token| D[Authenticated Staff]
    D -->|GET /staff/users/{userId}| E[View Own Profile]
    D -->|PUT /staff/users/{userId}/profile| F[Update Own Profile]
    HR[Admin / SuperAdmin / HR] -->|GET /staff/users| G[List All Staff]
    HR -->|PUT /staff/users/{userId}/status| H[Activate / Deactivate Staff Account]
    HR -->|DELETE /staff/users/{userId}| I[Delete Staff Account]
```

---

### Step 1: Register a New Staff Account

**Endpoint:** `POST http://localhost:5000/api/identity/staff/register`  
**Authentication:** Public (No token required)

#### Request Body:
```json
{
  "email": "captain.rogers@skypass.com",
  "password": "StaffPassword123!",
  "firstName": "Steve",
  "lastName": "Rogers",
  "department": "Flight Operations",
  "roleTitle": "Senior Pilot",
  "assignedAirportCode": "JFK",
  "role": "Staff"
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

**Endpoint:** `POST http://localhost:5000/api/identity/staff/login`  
**Authentication:** Public (No token required)

#### Request Body:
```json
{
  "email": "captain.rogers@skypass.com",
  "password": "StaffPassword123!"
}
```

#### Response (200 OK):
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIyMDAxIiwicm9sZSI6IlN0YWZmIiwiZW1haWwiOiJjYXB0YWluLnJvZ2Vyc0Bza3lwYXNzLmNvbSIsImV4cCI6MTc3MDQwMDAwMH0...",
  "userId": 2001,
  "email": "captain.rogers@skypass.com",
  "firstName": "Steve",
  "lastName": "Rogers",
  "role": "Staff"
}
```

> [!TIP]
> Copy the returned `token` string and paste it into Swagger UI (**Authorize** button: `Bearer <token>`) or set it as a HTTP Header in Postman:  
> `Authorization: Bearer <token>`

---

### Step 3: View & Update Staff Profile

#### View Staff Profile
**Endpoint:** `GET http://localhost:5000/api/staff/users/2001`  
**Authentication:** `Bearer <JWT_TOKEN>` (Staff, Admin, HR)

#### Response (200 OK):
```json
{
  "id": 2001,
  "email": "captain.rogers@skypass.com",
  "firstName": "Steve",
  "lastName": "Rogers",
  "department": "Flight Operations",
  "roleTitle": "Senior Pilot",
  "assignedAirportCode": "JFK",
  "role": "Staff",
  "isActive": true
}
```

#### Update Staff Profile Details
**Endpoint:** `PUT http://localhost:5000/api/staff/users/2001/profile`  
**Authentication:** `Bearer <JWT_TOKEN>` (Staff, Admin, HR)

#### Request Body:
```json
{
  "firstName": "Steve",
  "lastName": "Rogers",
  "department": "Flight Operations",
  "roleTitle": "Captain / Flight Commander"
}
```

#### Response (200 OK):
```json
{
  "id": 2001,
  "email": "captain.rogers@skypass.com",
  "firstName": "Steve",
  "lastName": "Rogers",
  "department": "Flight Operations",
  "roleTitle": "Captain / Flight Commander",
  "assignedAirportCode": "JFK",
  "role": "Staff",
  "isActive": true
}
```

---

### Step 4: Password Reset via OTP

**Endpoint:** `POST http://localhost:5000/api/identity/staff/reset-password`  
**Authentication:** Public (No token required)

#### Request Body:
```json
{
  "email": "captain.rogers@skypass.com",
  "token": "921405",
  "newPassword": "NewStaffPassword123!"
}
```

#### Response (200 OK):
```json
{
  "message": "Password reset successfully."
}
```

---

### Step 5: HR / Admin Management Operations

#### List All Staff Members
**Endpoint:** `GET http://localhost:5000/api/staff/users`  
**Authentication:** `Bearer <JWT_TOKEN>` (Admin, SuperAdmin, HR)

#### Response (200 OK):
```json
[
  {
    "id": 2001,
    "email": "captain.rogers@skypass.com",
    "firstName": "Steve",
    "lastName": "Rogers",
    "department": "Flight Operations",
    "roleTitle": "Captain",
    "role": "Staff",
    "isActive": true
  },
  {
    "id": 2002,
    "email": "carol.danvers@skypass.com",
    "firstName": "Carol",
    "lastName": "Danvers",
    "department": "Ground Operations",
    "roleTitle": "Ground Operations Manager",
    "role": "GroundStaff",
    "isActive": true
  }
]
```

#### Activate / Deactivate Staff Account
**Endpoint:** `PUT http://localhost:5000/api/staff/users/2001/status`  
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

#### Delete Staff Profile
**Endpoint:** `DELETE http://localhost:5000/api/staff/users/2001`  
**Authentication:** `Bearer <JWT_TOKEN>` (Admin, SuperAdmin, HR)

#### Response (200 OK):
```json
{
  "message": "User deleted"
}
```

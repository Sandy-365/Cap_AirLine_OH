# SkyPass Admin Portal - Complete Step-by-Step API Guide & Role Matrix

This guide provides a comprehensive, step-by-step walkthrough for using all **Admin APIs** in the SkyPass Microservices system, broken down by **Role Permissions**.

---

## 📋 Table of Contents
1. [Overview & Prerequisites](#1-overview--prerequisites)
2. [Role Permission Matrix](#2-role-permission-matrix)
3. [APIs Accessible by Public (No Auth Required)](#3-apis-accessible-by-public-no-auth-required)
4. [APIs Accessible by All Admin Roles (`SuperAdmin`, `Admin`, `HR`, `FinancialAdmin`)](#4-apis-accessible-by-all-admin-roles-superadmin-admin-hr-financialadmin)
5. [APIs Accessible by `SuperAdmin` & `HR` Only](#5-apis-accessible-by-superadmin--hr-only)
6. [APIs Accessible Exclusively by `SuperAdmin` Only](#6-apis-accessible-exclusively-by-superadmin-only)

---

## 1. Overview & Prerequisites

- **API Gateway Address**: `http://localhost:5000`
- **Swagger UI**: Available at `http://localhost:5000/swagger`
- **Required Header for Protected Endpoints**:
  ```http
  Authorization: Bearer <your_jwt_token>
  Content-Type: application/json
  ```

---

## 2. Role Permission Matrix

| HTTP Method | Endpoint | Description | Allowed Roles |
| :--- | :--- | :--- | :--- |
| `POST` | `/identity/admin/login` | Log in & obtain Bearer JWT Token | **Public** |
| `POST` | `/admin/register` | Create a new admin profile | **`SuperAdmin` Only** |
| `POST` | `/identity/admin/forgot-password` | Generate & email password reset OTP token | **Public** |
| `POST` | `/identity/admin/reset-password` | Reset password using OTP token | **Public** |
| `GET` | `/admin/dashboard` | View live system metrics & counts | `SuperAdmin`, `Admin`, `HR`, `FinancialAdmin` |
| `GET` | `/admin/booking-report` | Generate booking reports by date range | `SuperAdmin`, `Admin`, `HR`, `FinancialAdmin` |
| `GET` | `/admin/revenue-report` | Generate revenue reports by date range | `SuperAdmin`, `Admin`, `HR`, `FinancialAdmin` |
| `GET` | `/admin/users/{userId}` | View profile details | `SuperAdmin` (all users), others (own profile) |
| `PUT` | `/admin/users/{userId}/profile` | Update profile name and email | `SuperAdmin` (all users), others (own profile) |
| `GET` | `/admin/users` | List all admin user accounts | `SuperAdmin`, `HR` |
| `PUT` | `/admin/users/{userId}/status` | Activate or deactivate account | `SuperAdmin`, `HR` |
| `DELETE` | `/admin/users/{userId}` | Permanently delete user profile | **`SuperAdmin` Only** |

---

## 3. APIs Accessible by Public (No Auth Required)

### 1. Log in (`POST /identity/admin/login`)
Authenticates credentials and returns user ID, email, name, role, and Bearer JWT token.
```json
{
  "email": "superadmin@airline.com",
  "password": "admin123"
}
```

### 2. Forgot Password (`POST /identity/admin/forgot-password`)
Generates a 6-digit password reset OTP token and sends it to the specified email address.
```json
{
  "email": "superadmin@airline.com"
}
```

### 3. Reset Password (`POST /identity/admin/reset-password`)
```json
{
  "email": "superadmin@airline.com",
  "token": "258972",
  "newPassword": "NewSecurePassword@123"
}
```

---

## 4. APIs Accessible by SuperAdmin Only

### 1. Register Admin User (`POST /admin/register`)
Creates a new admin/HR profile in the system. Requires `Bearer <SuperAdmin_JWT_Token>`.
```json
{
  "name": "Sarah Connor",
  "email": "sarah.hr@airline.com",
  "password": "TempPassword@123",
  "department": "Human Resources",
  "role": "HR"
}
```

**Response (200 OK):**
```json
{
  "message": "Admin registered successfully."
}
```

---

## 5. APIs Accessible by All Admin Roles (`SuperAdmin`, `Admin`, `HR`, `FinancialAdmin`)

### 1. Dashboard Metrics (`GET /admin/dashboard`)
Retrieves live aggregated metrics (Total Bookings, Revenue, Active Flights, Total Users).

### 2. Booking Report (`GET /admin/booking-report`)
`GET /admin/booking-report?startDate=2026-08-01T00:00:00Z&endDate=2026-08-31T23:59:59Z`

### 3. Revenue Report (`GET /admin/revenue-report`)
`GET /admin/revenue-report?startDate=2026-08-01T00:00:00Z&endDate=2026-08-31T23:59:59Z`

### 4. View Own Profile (`GET /admin/users/{userId}`)
Users can view their own profile; `SuperAdmin` can view any user's profile.

### 5. Update Own Profile (`PUT /admin/users/{userId}/profile`)
Users can update their own profile; `SuperAdmin` can update any user's profile.

---

## 5. APIs Accessible by `SuperAdmin` & `HR` Only

### 1. List All Admin Users (`GET /admin/users`)
Lists all admin accounts in the system. Supports optional filtering by `roles` (e.g. `GET /admin/users?roles=Admin,HR`).

### 2. Activate / Deactivate Admin Account (`PUT /admin/users/{userId}/status`)
Toggles account status (`isActive: true` or `false`) to grant or suspend access.
```json
{
  "isActive": false
}
```

---

## 6. APIs Accessible Exclusively by `SuperAdmin` Only

### 1. Delete Admin Account (`DELETE /admin/users/{userId}`)
Permanently deletes an admin profile from the system.
- **Allowed Role**: **`SuperAdmin` Only**
- **HTTP Method**: `DELETE`
- **Gateway Endpoint**: `/admin/users/4135411e-969e-4a83-b16b-29ece7904f7c`

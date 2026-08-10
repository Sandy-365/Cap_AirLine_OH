# SkyPass Airline Management System - Complete API Documentation

Welcome to the **SkyPass Microservices API Documentation**. This document covers all endpoints, workflows, authentication mechanisms, request/response formats, and role permissions across the entire system.

---

## 1. System Architecture & Gateway Routing

All external requests should go through the **API Gateway** running at:
`http://localhost:5000`

### Microservice Port Mapping
| Microservice | Downstream Base URL | Upstream Route Prefix | Swagger Key |
| :--- | :--- | :--- | :--- |
| **Api Gateway** | `http://localhost:5000` | `/` | Aggregated Swagger UI |
| **Admin Service** | `http://localhost:5010` | `/identity/admin/*`, `/admin/*` | `admin` |
| **Passenger Service** | `http://localhost:5007` | `/identity/passenger/*`, `/passengers/*` | `passengers` |
| **Staff Service** | `http://localhost:5011` | `/identity/staff/*`, `/staff/*` | `staff` |
| **Flight Service** | `http://localhost:5002` | `/flights/*` | `flights` |
| **Booking Service** | `http://localhost:5003` | `/bookings/*` | `bookings` |
| **Payment Service** | `http://localhost:5004` | `/payments/*` | `payments` |
| **CheckIn Service** | `http://localhost:5005` | `/checkin/*`, `/baggage/*` | `checkins` |

---

## 2. Authentication & Headers

Requests to protected endpoints require a **JSON Web Token (JWT)** in the HTTP Header:

```http
Authorization: Bearer <your_jwt_token>
Content-Type: application/json
```

---

## 3. API Endpoint Reference (Grouped & Ordered Serially)

---

### A. Admin Service (`Admin` & `AdminAuth`)

#### `Admin` Group (`/admin/*`)
1. **`GET /admin/dashboard`** (Aggregated system metrics)
2. **`GET /admin/booking-report`** (Filter bookings by date range)
3. **`GET /admin/revenue-report`** (Daily revenue breakdown)
4. **`GET /admin/users`** (List all admin accounts)
5. **`GET /admin/users/{userId}`** (Get single admin profile by Guid ID)
6. **`PUT /admin/users/{userId}/profile`** (Update admin profile name/email)
7. **`PUT /admin/users/{userId}/status`** (Activate or deactivate admin account)
8. **`DELETE /admin/users/{userId}`** (Permanently delete admin account)

#### `AdminAuth` Group (`/identity/admin/*`)
1. **`POST /identity/admin/login`** (Log in as admin to get JWT Bearer token)
2. **`POST /identity/admin/register`** (Register self or provision admin account)
3. **`POST /identity/admin/reset-password`** (Reset password using OTP token)

---

### B. Passenger Service (`Passengers` & `PassengerAuth`)

#### `Passengers` Group (`/passengers/*`)
1. **`GET /passengers/users`** (List all passenger user accounts)
2. **`GET /passengers/users/{userId}`** (Get passenger profile by ID)
3. **`PUT /passengers/users/{userId}/profile`** (Update passenger profile)
4. **`PUT /passengers/users/{userId}/status`** (Activate/deactivate passenger account)
5. **`DELETE /passengers/users/{userId}`** (Delete passenger profile)

#### `PassengerAuth` Group (`/identity/passenger/*`)
1. **`POST /identity/passenger/login`** (Log in as passenger)
2. **`POST /identity/passenger/register`** (Register new passenger)
3. **`POST /identity/passenger/reset-password`** (Reset passenger password)

---

### C. Staff Service (`Staff` & `StaffAuth`)

#### `Staff` Group (`/staff/*`)
1. **`GET /staff`** (List staff records)
2. **`GET /staff/{id}`** (Get staff record by ID)
3. **`POST /staff`** (Create staff profile)
4. **`PUT /staff/{id}`** (Update staff profile)
5. **`DELETE /staff/{id}`** (Delete staff profile)
6. **`GET /staff/users`** (List all staff accounts)
7. **`GET /staff/users/{userId}`** (Get staff user profile)
8. **`PUT /staff/users/{userId}/profile`** (Update staff user profile)
9. **`PUT /staff/users/{userId}/status`** (Activate/deactivate staff account)
10. **`DELETE /staff/users/{userId}`** (Delete staff account)

#### `StaffAuth` Group (`/identity/staff/*`)
1. **`POST /identity/staff/login`**
2. **`POST /identity/staff/register`**
3. **`POST /identity/staff/reset-password`**

---

### D. Flight Management Service (`/flights/*`)
1. **`GET /flights`**
2. **`GET /flights/{id}`**
3. **`POST /flights`**
4. **`PUT /flights/{id}`**
5. **`DELETE /flights/{id}`**
6. **`GET /flights/schedules`**
7. **`POST /flights/schedules`**

---

### E. Booking Service (`/bookings/*`)
1. **`GET /bookings`**
2. **`GET /bookings/{id}`**
3. **`POST /bookings`**
4. **`POST /bookings/{bookingId}/passengers`**
5. **`GET /bookings/{bookingId}/passengers`**
6. **`POST /bookings/{id}/cancel`**
7. **`POST /bookings/passengers/{passengerId}/cancel`**
8. **`GET /bookings/occupied-seats`**

---

### F. Payment Service (`/payments/*`)
1. **`GET /payments/{id}`**
2. **`POST /payments`**
3. **`POST /payments/{id}/refund`**

---

### G. Check-In & Baggage Service (`/checkin/*`, `/baggage/*`)
1. **`POST /checkin/online`**
2. **`POST /checkin/staff`**
3. **`GET /checkin/{id}/boarding-pass`**
4. **`GET /checkin`**
5. **`POST /baggage`**
6. **`PUT /baggage/{id}/status`**
7. **`GET /baggage`**

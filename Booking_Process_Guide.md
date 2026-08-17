# 🛫 Complete Flight Booking Roadmap
## Delhi → Mumbai | August 23, 2026

> **SkyPass Airlines — End-to-End API Journey**  
> All requests go through **API Gateway**: `http://localhost:5000`  
> JWT Token required for protected steps — obtained at **Step 2**

---

## 📋 Journey Overview

```
STEP 1  →  Register as Passenger          (PassengerService)
STEP 2  →  Login → Get JWT Token          (PassengerService)
STEP 3  →  Search Flights DEL→BOM Aug 23  (FlightOpsService)
STEP 4  →  View Schedules for Aug 23      (FlightOpsService)
STEP 5  →  Create Booking                 (FlightOpsService)
STEP 6  →  Add Passengers to Booking      (FlightOpsService)
STEP 7  →  Process Payment                (PaymentService)
STEP 8  →  Online Check-In               (FlightOpsService)
STEP 9  →  Get Boarding Pass             (FlightOpsService)
```

---

## 🔑 Service Port Reference

| Service | Direct Port | Via Gateway Prefix |
|---|---|---|
| PassengerService | `:5007` | `/passenger/...` → `localhost:5000` |
| FlightOpsService | `:5002` | `/flights/...` → `localhost:5000` |
| PaymentService | `:5004` | `/payment/...` → `localhost:5000` |

> [!TIP]
> Use **direct ports** (`:5007`, `:5002`, `:5004`) during development. The Gateway is for production/frontend use.

---

## ─────────────────────────────────────────────────
## STEP 1 — Register as Passenger
## ─────────────────────────────────────────────────

**Service:** PassengerService  
**Auth Required:** ❌ None (Public)

### Request

```http
POST http://localhost:5007/api/auth/register
Content-Type: application/json
```

```json
{
  "name": "Ravi Kumar",
  "email": "ravi.kumar@gmail.com",
  "password": "Ravi@12345",
  "phone": "9876543210",
  "dateOfBirth": "1994-06-15",
  "aadhar": "123456789012"
}
```

### Field Reference

| Field | Type | Required | Notes |
|---|---|---|---|
| `name` | `string` | ✅ | Full name |
| `email` | `string` | ✅ | Must be unique |
| `password` | `string` | ✅ | Min 6 characters |
| `phone` | `string` | ❌ | 10-digit mobile |
| `dateOfBirth` | `string` | ❌ | Format: `YYYY-MM-DD` |
| `aadhar` | `string` | ❌ | 12-digit Aadhar number |

### ✅ Success Response — `200 OK`

```json
{
  "message": "Registration successful. Check your email for the OTP."
}
```

> [!NOTE]
> Registration automatically activates and verifies the account. You can proceed immediately to **Step 2** to log in.

---

## ─────────────────────────────────────────────────
## STEP 2 — Login & Get JWT Token
## ─────────────────────────────────────────────────

**Service:** PassengerService  
**Auth Required:** ❌ None (Public)

> [!IMPORTANT]
> **Save the `token` from this response — you will need it in every step from Step 5 onwards.**

### Request

```http
POST http://localhost:5007/api/auth/login
Content-Type: application/json
```

```json
{
  "email": "ravi.kumar@gmail.com",
  "password": "Ravi@12345"
}
```

### ✅ Success Response — `200 OK`

```json
{
  "userId": 42,
  "email": "ravi.kumar@gmail.com",
  "name": "Ravi Kumar",
  "role": "Passenger",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiI0MiIs..."
}
```

### 📌 What to Save

| Value | Where to Use |
|---|---|
| `userId` | `42` — needed in Step 5 (CreateBooking) |
| `token` | Add as `Authorization: Bearer <token>` header in Steps 5-9 |

### ❌ Error — `401 Unauthorized`

```json
{
  "message": "Invalid email or password."
}
```

---

## ─────────────────────────────────────────────────
## STEP 3 — Search Flights (Delhi → Mumbai, Aug 23)
## ─────────────────────────────────────────────────

**Service:** FlightOpsService  
**Auth Required:** ❌ None (Public)

### Request

```http
GET http://localhost:5002/api/flights?source=Delhi&destination=Mumbai&departureDate=2026-08-23
```

### ✅ Success Response — `200 OK`

```json
[
  {
    "id": 1,
    "flightNumber": "SP-101",
    "source": "Delhi",
    "destination": "Mumbai",
    "departureTime": "2026-08-23T00:30:00Z",
    "arrivalTime": "2026-08-23T02:45:00Z",
    "gate": "A1",
    "aircraft": "Boeing 737",
    "status": "Scheduled",
    "totalSeats": 180,
    "availableSeats": 180,
    "economySeats": 140,
    "businessSeats": 30,
    "firstSeats": 10,
    "economyPrice": 4500.00,
    "businessPrice": 12000.00,
    "firstClassPrice": 28000.00
  }
]
```

> [!NOTE]
> **Time Conversion:** `departureTime: 2026-08-23T00:30:00Z` (UTC) = **06:00 AM IST** on Aug 23.  
> Depart from Delhi at **6:00 AM IST**, arrive Mumbai at **8:15 AM IST**.

### 📌 What to Save

| Value | Where to Use |
|---|---|
| `id` → `1` | `flightId` in Step 5 (CreateBooking) |
| `economyPrice` → `4500.00` | Used to calculate `totalAmount` in Step 5 |

---

## ─────────────────────────────────────────────────
## STEP 4 — Get Schedules for Aug 23
## ─────────────────────────────────────────────────

**Service:** FlightOpsService  
**Auth Required:** ❌ None (Public)

> Get the specific **schedule instance** for Aug 23. Each schedule has its own seat counts and `scheduleId`.

### Request

```http
GET http://localhost:5002/api/flights/schedules?source=Delhi&destination=Mumbai&departureDate=2026-08-23
```

### Alternative — Get by flightId

```http
GET http://localhost:5002/api/flights/schedules?flightId=1
```

### ✅ Success Response — `200 OK`

```json
[
  {
    "id": 81,
    "flightId": 1,
    "flightNumber": "SP-101",
    "source": "Delhi",
    "destination": "Mumbai",
    "aircraft": "Boeing 737",
    "departureTime": "2026-08-23T00:30:00Z",
    "arrivalTime": "2026-08-23T02:45:00Z",
    "gate": "A1",
    "status": "Scheduled",
    "totalSeats": 180,
    "availableSeats": 180,
    "economySeats": 140,
    "businessSeats": 30,
    "firstSeats": 10,
    "economyPrice": 4500.00,
    "businessPrice": 12000.00,
    "firstClassPrice": 28000.00,
    "createdAt": "2026-08-15T08:31:00Z"
  }
]
```

> [!IMPORTANT]
> **The `id` here is the `scheduleId`** — NOT the `flightId`. Save both:
> - `scheduleId` = `81` (the specific Aug 23 departure)
> - `flightId` = `1` (the flight template)

### Schedule ID Calculation

The schedules were seeded from Aug 15 → Aug 30. Each day has 10 schedules.  
SP-101 (Flight 1, Delhi→Mumbai) schedule IDs:

| Date | Schedule ID |
|---|---|
| Aug 15 | 1 |
| Aug 16 | 11 |
| Aug 17 | 21 |
| ... | ... |
| **Aug 23** | **81** |
| Aug 24 | 91 |

---

## ─────────────────────────────────────────────────
## STEP 5 — Create Booking
## ─────────────────────────────────────────────────

**Service:** FlightOpsService  
**Auth Required:** ✅ `Bearer <JWT>` (Role: `Passenger` or `Dealer`)

### Request

```http
POST http://localhost:5002/api/bookings
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json
```

```json
{
  "flightId": 1,
  "scheduleId": 81,
  "seatClass": "Economy",
  "baggageWeight": 23.0,
  "passengerCount": 2,
  "totalAmount": 9000.00
}
```

> [!TIP]
> `userId`, `userEmail`, and `userName` are **automatically extracted from your JWT Bearer token claims** by the backend! You do not need to manually pass them.

### Field Reference

| Field | Type | Required | Value | Notes |
|---|---|---|---|---|
| `flightId` | `int` | ✅ | `1` | SP-101 from Step 3 |
| `scheduleId` | `int?` | ✅ | `81` | Aug 23 schedule from Step 4 |
| `seatClass` | `string` | ✅ | `"Economy"` | `"Economy"`, `"Business"`, or `"First"` |
| `baggageWeight` | `decimal` | ✅ | `23.0` | Total bag weight in KG |
| `passengerCount` | `int` | ✅ | `2` | Number of passengers |
| `totalAmount` | `decimal` | ✅ | `9000.00` | `economyPrice × passengerCount` = 4500 × 2 |
| `userId` | `int?` | ❌ Optional | Auto | Extracted from JWT token (`sub`) |
| `userEmail` | `string?` | ❌ Optional | Auto | Extracted from JWT token (`email`) |
| `userName` | `string?` | ❌ Optional | Auto | Extracted from JWT token (`name`) |

### ✅ Success Response — `201 Created`

```json
{
  "id": 1001,
  "userId": 42,
  "flightId": 1,
  "scheduleId": 81,
  "seatClass": "Economy",
  "baggageWeight": 23.0,
  "pnr": "SPABCD12",
  "status": "Pending",
  "paymentStatus": "Pending",
  "totalPassengers": 2,
  "confirmedPassengers": 0,
  "cancelledPassengers": 0,
  "createdAt": "2026-08-15T09:00:00Z",
  "totalAmount": 9000.00,
  "passengers": []
}
```

### 📌 What to Save

| Value | Where to Use |
|---|---|
| `id` → `1001` | `bookingId` in Steps 6, 7, 8 |
| `pnr` → `"SPABCD12"` | Passenger Name Record — for future lookups |

### ❌ Error Responses

| Status | Error Code | Cause |
|---|---|---|
| `400` | `SEATS_NOT_AVAILABLE` | Not enough economy seats left |
| `404` | `FLIGHT_NOT_FOUND` | Wrong `flightId` |
| `404` | `SCHEDULE_NOT_FOUND` | Wrong `scheduleId` |
| `400` | `VALIDATION_ERROR` | Invalid `seatClass` value |

---

## ─────────────────────────────────────────────────
## STEP 6 — Add Passengers to Booking
## ─────────────────────────────────────────────────

**Service:** FlightOpsService  
**Auth Required:** ✅ `Bearer <JWT>` (Role: `Passenger` or `Dealer`)

> Add individual passenger details for each person in the booking.  
> Since `passengerCount = 2`, send **2 passenger objects** in the array.

### Request

```http
POST http://localhost:5002/api/bookings/1001/passengers
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json
```

```json
[
  {
    "name": "Ravi Kumar",
    "age": 32,
    "gender": "Male",
    "aadharCardNo": "123456789012",
    "passportNumber": "",
    "nationality": "Indian",
    "dietaryRequirements": "Vegetarian",
    "medicalNeeds": "None",
    "medicalAlerts": "",
    "seatNumber": "14A",
    "fare": 4500.00
  },
  {
    "name": "Priya Kumar",
    "age": 28,
    "gender": "Female",
    "aadharCardNo": "987654321098",
    "passportNumber": "",
    "nationality": "Indian",
    "dietaryRequirements": "Standard",
    "medicalNeeds": "None",
    "medicalAlerts": "",
    "seatNumber": "14B",
    "fare": 4500.00
  }
]
```

### Validation Rules

| Field | Validation |
|---|---|
| `name` | 2–100 characters |
| `age` | 1–120 |
| `gender` | Any string, max 20 chars |
| `aadharCardNo` | **Exactly 12 digits** — no spaces or dashes |
| `seatNumber` | Optional — auto-assigned if omitted |
| `fare` | Must be ≥ 0 |

### ✅ Success Response — `201 Created`

```json
[
  {
    "id": 301,
    "name": "Ravi Kumar",
    "age": 32,
    "gender": "Male",
    "aadharCardNo": "123456789012",
    "passportNumber": "",
    "nationality": "Indian",
    "dietaryRequirements": "Vegetarian",
    "medicalNeeds": "None",
    "medicalAlerts": "",
    "status": "Confirmed",
    "fare": 4500.00,
    "cancelledAt": null,
    "cancellationReason": null,
    "seatNumber": "14A",
    "createdAt": "2026-08-15T09:01:00Z"
  },
  {
    "id": 302,
    "name": "Priya Kumar",
    "age": 28,
    "gender": "Female",
    "aadharCardNo": "987654321098",
    "passportNumber": "",
    "nationality": "Indian",
    "dietaryRequirements": "Standard",
    "medicalNeeds": "None",
    "medicalAlerts": "",
    "status": "Confirmed",
    "fare": 4500.00,
    "cancelledAt": null,
    "cancellationReason": null,
    "seatNumber": "14B",
    "createdAt": "2026-08-15T09:01:00Z"
  }
]
```

### 📌 What to Save

| Value | Where to Use |
|---|---|
| Passenger 1 `id` → `301` | `passengerId` in Step 8 (Check-In) |
| Passenger 2 `id` → `302` | `passengerId` in Step 8 (Check-In) |

---

## ─────────────────────────────────────────────────
## STEP 7 — Process Payment
## ─────────────────────────────────────────────────

**Service:** PaymentService  
**Auth Required:** ✅ `Bearer <JWT>` (Role: `Passenger` or `Dealer`)

> Processes payment for the booking via Razorpay gateway (test mode).  
> On success, the booking `status` changes from `Pending` → `Confirmed`.

### Request

```http
POST http://localhost:5004/api/payments
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json
```

```json
{
  "bookingId": 1001,
  "amount": 9000.00,
  "paymentMethod": "Card",
  "userId": 42,
  "userEmail": "ravi.kumar@gmail.com",
  "userName": "Ravi Kumar"
}
```

### Field Reference

| Field | Type | Required | Value | Notes |
|---|---|---|---|---|
| `bookingId` | `int` | ✅ | `1001` | From Step 5 |
| `amount` | `decimal` | ✅ | `9000.00` | Must match booking `totalAmount` |
| `paymentMethod` | `string` | ✅ | `"Card"` | `"Card"`, `"UPI"`, `"NetBanking"` |
| `userId` | `int` | ✅ | `42` | From Step 2 |
| `userEmail` | `string` | ✅ | `"ravi.kumar@gmail.com"` | |
| `userName` | `string` | ✅ | `"Ravi Kumar"` | |

### ✅ Success Response — `200 OK`

```json
{
  "id": 501,
  "bookingId": 1001,
  "amount": 9000.00,
  "status": "Paid",
  "paymentMethod": "Card",
  "createdAt": "2026-08-15T09:05:00Z"
}
```

### 📌 What to Save

| Value | Where to Use |
|---|---|
| `id` → `501` | `paymentId` — for refund if needed |

> [!IMPORTANT]
> After this step, the booking's `paymentStatus` becomes **`"Paid"`** and booking `status` becomes **`"Confirmed"`**.  
> You can verify by calling:
> ```
> GET http://localhost:5002/api/bookings/1001
> Authorization: Bearer ...
> ```

---

## ─────────────────────────────────────────────────
## STEP 8 — Online Check-In
## ─────────────────────────────────────────────────

**Service:** FlightOpsService  
**Auth Required:** ✅ `Bearer <JWT>` (Role: `Passenger`)

> [!IMPORTANT]
> **Check-in window opens only 5 hours before departure.**  
> SP-101 departs at **00:30 UTC (06:00 IST)** on Aug 23.  
> Check-in opens at: **Aug 22, 19:30 UTC (Aug 23, 01:00 IST)**  
> *(5 hours before 06:00 AM IST = 01:00 AM IST)*

> Check-in **each passenger separately** — one API call per passenger.

### Request — Passenger 1 (Ravi Kumar)

```http
POST http://localhost:5002/api/checkins/online?passengerName=Ravi+Kumar&flightNumber=SP-101&flightId=1&departureTime=2026-08-23T00:30:00Z&fare=4500
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json
```

```json
{
  "bookingId": 1001,
  "passengerId": 301,
  "userId": 42,
  "seatNumber": "14A"
}
```

### Query Parameters (required in URL)

| Parameter | Type | Value | Notes |
|---|---|---|---|
| `passengerName` | `string` | `Ravi+Kumar` | URL-encoded name |
| `flightNumber` | `string` | `SP-101` | From Step 3 |
| `flightId` | `int` | `1` | From Step 3 |
| `departureTime` | `DateTime` (UTC) | `2026-08-23T00:30:00Z` | **Must be UTC** |
| `fare` | `decimal` | `4500` | Economy fare |

### Request Body Fields

| Field | Type | Required | Value |
|---|---|---|---|
| `bookingId` | `int` | ✅ | `1001` |
| `passengerId` | `int` | ✅ | `301` (passenger 1) |
| `userId` | `int` | ✅ | `42` |
| `seatNumber` | `string?` | ❌ | `"14A"` (or omit for auto-assign) |

### ✅ Success Response — `200 OK`

```json
{
  "id": 201,
  "bookingId": 1001,
  "passengerId": 301,
  "passengerName": "Ravi Kumar",
  "flightNumber": "SP-101",
  "seatNumber": "14A",
  "gate": "A1",
  "boardingPass": "Ravi Kumar|SP-101|14A",
  "checkInTime": "2026-08-23T01:00:00Z"
}
```

### Request — Passenger 2 (Priya Kumar)

```http
POST http://localhost:5002/api/checkins/online?passengerName=Priya+Kumar&flightNumber=SP-101&flightId=1&departureTime=2026-08-23T00:30:00Z&fare=4500
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json
```

```json
{
  "bookingId": 1001,
  "passengerId": 302,
  "userId": 42,
  "seatNumber": "14B"
}
```

### 📌 What to Save

| Value | Where to Use |
|---|---|
| Passenger 1 check-in `id` → `201` | Step 9 — Get Boarding Pass |
| Passenger 2 check-in `id` → `202` | Step 9 — Get Boarding Pass |

### ❌ Error Responses

| Status | Message | Cause |
|---|---|---|
| `400` | `"Check-in for flight SP-101 opens only 5 hours before departure..."` | Too early to check in |
| `400` | `"Flight SP-101 has already departed."` | Past departure time |

---

## ─────────────────────────────────────────────────
## STEP 9 — Get Boarding Pass
## ─────────────────────────────────────────────────

**Service:** FlightOpsService  
**Auth Required:** ✅ `Bearer <JWT>` (Role: `Passenger`, `Admin`, `Staff`, `GroundStaff`)

> Retrieve digital boarding pass for each checked-in passenger.

### Request — Passenger 1 Boarding Pass

```http
GET http://localhost:5002/api/checkins/201/boarding-pass
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### ✅ Success Response — `200 OK`

```json
{
  "passengerName": "Ravi Kumar",
  "flightNumber": "SP-101",
  "gate": "A1",
  "seatNumber": "14A",
  "qrCode": "SP-101-14A-ABC123XYZ",
  "departureTime": "2026-08-23T00:30:00Z"
}
```

### Request — Passenger 2 Boarding Pass

```http
GET http://localhost:5002/api/checkins/202/boarding-pass
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

## ✅ Booking Complete — Full Summary

| Step | Action | Status | Key Output |
|---|---|---|---|
| 1 | Register | ✅ Done | Account created |
| 2 | Login | ✅ Done | `userId=42`, `token=...` |
| 3 | Search Flights | ✅ Done | `flightId=1` (SP-101) |
| 4 | Get Schedule | ✅ Done | `scheduleId=81` (Aug 23) |
| 5 | Create Booking | ✅ Done | `bookingId=1001`, `pnr=SPABCD12` |
| 6 | Add Passengers | ✅ Done | `passengerId=301`, `302` |
| 7 | Payment | ✅ Done | `paymentId=501`, Status: Paid |
| 8 | Check-In | ✅ Done | `checkInId=201`, `202` |
| 9 | Boarding Pass | ✅ Done | QR codes for gate entry |

---

## 📱 Quick Lookup Endpoints (Any Time)

```http
# Look up booking by PNR
GET http://localhost:5002/api/bookings?pnr=SPABCD12

# Look up all bookings for your user
GET http://localhost:5002/api/bookings?userId=42

# Get occupied seats on the flight
GET http://localhost:5002/api/bookings/occupied-seats?flightId=1&scheduleId=81
Authorization: Bearer ...

# Get all your check-ins for the booking
GET http://localhost:5002/api/checkins?bookingId=1001
Authorization: Bearer ...

# Get payment details
GET http://localhost:5004/api/payments/501
Authorization: Bearer ...

# Get your passenger profile
GET http://localhost:5007/api/passengers/users/42
Authorization: Bearer ...
```

---

## ❌ Cancellation Flow (If Needed)

### Cancel Entire Booking

```http
POST http://localhost:5002/api/bookings/1001/cancel
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Cancel Individual Passenger Only

```http
POST http://localhost:5002/api/bookings/passengers/302/cancel
Authorization: Bearer ...
Content-Type: application/json

{
  "cancellationReason": "Change of travel plans"
}
```

### Admin Refund

```http
POST http://localhost:5004/api/payments/501/refund
Authorization: Bearer <Admin JWT>
```

---

## 🕐 IST Time Reference for SP-101 (Aug 23)

| Event | UTC | IST |
|---|---|---|
| Check-in opens | Aug 22, 19:30 UTC | Aug 23, 01:00 AM IST |
| Check-in closes | Aug 23, 00:30 UTC | Aug 23, 06:00 AM IST (departure) |
| **Departure** | **2026-08-23T00:30:00Z** | **06:00 AM IST** |
| Arrival Mumbai | 2026-08-23T02:45:00Z | 08:15 AM IST |

---

## 🔗 Swagger UI (Interactive Testing)

| Service | Swagger URL |
|---|---|
| FlightOps | `http://localhost:5002/swagger` |
| PassengerService | `http://localhost:5007/swagger` |
| PaymentService | `http://localhost:5004/swagger` |
| BackOffice | `http://localhost:5010/swagger` |
| API Gateway | `http://localhost:5000/swagger` |

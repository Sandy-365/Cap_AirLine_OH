# 🎫 Booking Service API Guide & Workflow Manual

Welcome to the **Booking Service** technical guide. This guide details how to create bookings, attach passenger records, query booking histories, query occupied seat maps, cancel bookings/passengers, and manage booking lifecycle events.

---

## 📌 Base URLs

- **API Gateway (Recommended)**: `http://localhost:5000/api`
- **Direct Downstream Microservice**: `http://localhost:5003/api`
- **Swagger Documentation**: `http://localhost:5000/swagger/index.html`

---

## 🔒 Role-Based Access Control (RBAC) Matrix

| Endpoint | HTTP Method | Allowed Roles | Description |
| :--- | :--- | :--- | :--- |
| `/bookings` | `GET` | **Public / Authorized Users** | Get all bookings or filter by PNR, userId, flightId, or scheduleId |
| `/bookings/{id}` | `GET` | `Passenger`, `Dealer`, `Admin` | Retrieve booking details by booking ID |
| `/bookings` | `POST` | `Passenger`, `Dealer` | Create a new flight booking reservation |
| `/bookings/{bookingId}/passengers` | `POST` | `Passenger`, `Dealer` | Add passenger details to an existing booking |
| `/bookings/{bookingId}/passengers` | `GET` | `Passenger`, `Dealer`, `Admin`, `GroundStaff`, `Staff` | List all passengers registered for a booking |
| `/bookings/{id}/cancel` | `POST` | `Passenger`, `Dealer` | Cancel an entire booking reservation |
| `/bookings/{id}` | `DELETE` | `Passenger`, `Dealer`, `Admin` | Permanently delete a booking record |
| `/bookings/passengers/{passengerId}/cancel` | `POST` | `Passenger`, `Dealer` | Cancel a single passenger from a multi-passenger booking |
| `/bookings/occupied-seats` | `GET` | `Passenger`, `Dealer`, `Admin` | Get list of currently occupied seat numbers for a flight |

---

## 🚀 Step-by-Step API Usage Workflow

```mermaid
flowchart TD
    User[Passenger / Dealer] -->|1. GET /bookings/occupied-seats?flightId=10| A[View Occupied Seat Map]
    User -->|2. POST /bookings| B(Create Booking & Generate PNR)
    B -->|3. POST /bookings/{id}/passengers| C[Add Passengers & Select Seats]
    User -->|4. GET /bookings?pnr=AB1234| D[Query Booking Status]
    User -->|5. POST /bookings/{id}/cancel| E[Cancel Booking]
```

---

### Step 1: Query Occupied Seats Map

**Endpoint:** `GET http://localhost:5000/api/bookings/occupied-seats?flightId=10&scheduleId=105`  
**Authentication:** `Bearer <JWT_TOKEN>` (`Passenger`, `Dealer`, `Admin`)

#### Response (200 OK):
```json
[
  "12A",
  "12B",
  "14F",
  "03A"
]
```

---

### Step 2: Create a New Booking

**Endpoint:** `POST http://localhost:5000/api/bookings`  
**Authentication:** `Bearer <JWT_TOKEN>` (`Passenger`, `Dealer`)

#### Request Body:
```json
{
  "userId": 1001,
  "flightId": 10,
  "scheduleId": 105,
  "seatClass": "Economy",
  "totalPassengers": 2,
  "contactEmail": "jane.doe@example.com",
  "contactPhone": "+1234567890"
}
```

#### Response (201 Created):
```json
{
  "id": 301,
  "pnr": "PNR-AB92K",
  "userId": 1001,
  "flightId": 10,
  "scheduleId": 105,
  "seatClass": "Economy",
  "totalPassengers": 2,
  "totalAmount": 500.00,
  "status": "PendingPayment",
  "createdAt": "2026-08-06T11:30:00Z"
}
```

---

### Step 3: Add Passengers to the Booking

**Endpoint:** `POST http://localhost:5000/api/bookings/301/passengers`  
**Authentication:** `Bearer <JWT_TOKEN>` (`Passenger`, `Dealer`)

#### Request Body:
```json
[
  {
    "firstName": "Jane",
    "lastName": "Doe",
    "gender": "Female",
    "age": 28,
    "seatNumber": "15C",
    "specialRequests": "Vegetarian Meal"
  },
  {
    "firstName": "John",
    "lastName": "Doe",
    "gender": "Male",
    "age": 30,
    "seatNumber": "15D",
    "specialRequests": "None"
  }
]
```

#### Response (201 Created):
```json
[
  {
    "id": 701,
    "bookingId": 301,
    "firstName": "Jane",
    "lastName": "Doe",
    "seatNumber": "15C"
  },
  {
    "id": 702,
    "bookingId": 301,
    "firstName": "John",
    "lastName": "Doe",
    "seatNumber": "15D"
  }
]
```

---

### Step 4: Query Booking Status & History

#### Query Booking by PNR Code
**Endpoint:** `GET http://localhost:5000/api/bookings?pnr=PNR-AB92K`  
**Authentication:** Public / Authorized

#### Query User Booking History
**Endpoint:** `GET http://localhost:5000/api/bookings?userId=1001`  
**Authentication:** Public / Authorized

#### Response (200 OK):
```json
{
  "id": 301,
  "pnr": "PNR-AB92K",
  "userId": 1001,
  "flightId": 10,
  "scheduleId": 105,
  "totalPassengers": 2,
  "totalAmount": 500.00,
  "status": "Confirmed",
  "passengers": [
    { "id": 701, "firstName": "Jane", "lastName": "Doe", "seatNumber": "15C" },
    { "id": 702, "firstName": "John", "lastName": "Doe", "seatNumber": "15D" }
  ]
}
```

---

### Step 5: Cancel Booking / Passenger

#### Cancel Entire Booking
**Endpoint:** `POST http://localhost:5000/api/bookings/301/cancel`  
**Authentication:** `Bearer <JWT_TOKEN>` (`Passenger`, `Dealer`)

#### Response (200 OK):
```json
{
  "message": "Booking cancelled successfully"
}
```

#### Cancel Specific Passenger
**Endpoint:** `POST http://localhost:5000/api/bookings/passengers/702/cancel`  
**Authentication:** `Bearer <JWT_TOKEN>` (`Passenger`, `Dealer`)

#### Request Body:
```json
{
  "reason": "Change of travel plans"
}
```

#### Response (200 OK):
```json
{
  "message": "Passenger cancelled successfully"
}
```

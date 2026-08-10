# ✈️ Flight Service API Guide & Workflow Manual

Welcome to the **Flight Service** technical guide. This guide details how to query, create, update, and manage base flight templates and scheduled flight instances, as well as internal seat booking endpoints.

---

## 📌 Base URLs

- **API Gateway (Recommended)**: `http://localhost:5000/api`
- **Direct Downstream Microservice**: `http://localhost:5002/api`
- **Swagger Documentation**: `http://localhost:5000/swagger/index.html`

---

## 🔒 Role-Based Access Control (RBAC) Matrix

| Endpoint | HTTP Method | Allowed Roles | Description |
| :--- | :--- | :--- | :--- |
| `/flights` | `GET` | **Public** (None) | Get all base flights or search by source, destination, and departure date |
| `/flights/{id}` | `GET` | **Public** (None) | Retrieve base flight details by flight ID |
| `/flights` | `POST` | `Admin`, `SuperAdmin` | Create a new base flight template |
| `/flights/{id}` | `PUT` | `Admin`, `SuperAdmin` | Update flight details (aircraft, gate, times, crew) |
| `/flights/{id}` | `DELETE` | `Admin`, `SuperAdmin` | Permanently delete a base flight template |
| `/flights/{id}/book-seat` | `POST` | **Internal Service Call** | Deduct available seats on a base flight template (BookingService) |
| `/flights/schedules` | `GET` | **Public** (None) | Get all flight schedules or search by source, destination, date, or flightId |
| `/flights/schedules` | `POST` | `Admin`, `SuperAdmin`, `Staff` | Create a new flight schedule instance from a base flight |
| `/flights/schedules/{id}/book-seat` | `POST` | **Internal Service Call** | Deduct available seats on a schedule with distributed locks |

---

## 🚀 Step-by-Step API Usage Workflow

```mermaid
flowchart TD
    Admin[Admin / SuperAdmin] -->|1. POST /flights| A[Create Base Flight Template]
    Staff[Admin / SuperAdmin / Staff] -->|2. POST /flights/schedules| B[Create Scheduled Flight Instance]
    Public[Public / Passengers] -->|3. GET /flights/schedules?source=DEL&destination=BOM...| C[Search Available Flights]
    Public -->|4. GET /flights/{id}| D[View Flight & Seat Details]
    BookingService[BookingService] -->|5. POST /flights/schedules/{id}/book-seat| E[Reserve Seats on Schedule]
```

---

### Step 1: Create a Base Flight Template (Admin/SuperAdmin)

**Endpoint:** `POST http://localhost:5000/api/flights`  
**Authentication:** `Bearer <ADMIN_JWT_TOKEN>` (`Admin`, `SuperAdmin`)

#### Request Body:
```json
{
  "flightNumber": "SK-501",
  "airlineName": "SkyPass Express",
  "source": "JFK",
  "destination": "LAX",
  "departureTime": "2026-09-01T08:00:00Z",
  "arrivalTime": "2026-09-01T11:30:00Z",
  "aircraftType": "Boeing 737-MAX",
  "totalSeats": 180,
  "availableSeats": 180,
  "basePrice": 250.00
}
```

#### Response (201 Created):
```json
{
  "id": 10,
  "flightNumber": "SK-501",
  "airlineName": "SkyPass Express",
  "source": "JFK",
  "destination": "LAX",
  "departureTime": "2026-09-01T08:00:00Z",
  "arrivalTime": "2026-09-01T11:30:00Z",
  "aircraftType": "Boeing 737-MAX",
  "totalSeats": 180,
  "availableSeats": 180,
  "basePrice": 250.00,
  "status": "Scheduled"
}
```

---

### Step 2: Create a Flight Schedule Instance (Admin/Staff)

**Endpoint:** `POST http://localhost:5000/api/flights/schedules`  
**Authentication:** `Bearer <JWT_TOKEN>` (`Admin`, `SuperAdmin`, `Staff`)

#### Request Body:
```json
{
  "flightId": 10,
  "departureTime": "2026-09-01T08:00:00Z",
  "arrivalTime": "2026-09-01T11:30:00Z",
  "gate": "B12",
  "terminal": "T4",
  "economySeatsAvailable": 150,
  "businessSeatsAvailable": 20,
  "firstSeatsAvailable": 10,
  "economyPrice": 250.00,
  "businessPrice": 600.00,
  "firstPrice": 1200.00
}
```

#### Response (201 Created):
```json
{
  "id": 105,
  "flightId": 10,
  "flightNumber": "SK-501",
  "source": "JFK",
  "destination": "LAX",
  "departureTime": "2026-09-01T08:00:00Z",
  "arrivalTime": "2026-09-01T11:30:00Z",
  "gate": "B12",
  "terminal": "T4",
  "economySeatsAvailable": 150,
  "businessSeatsAvailable": 20,
  "firstSeatsAvailable": 10
}
```

---

### Step 3: Search & Query Flights (Public)

#### Search Base Flights by Route & Date
**Endpoint:** `GET http://localhost:5000/api/flights?source=JFK&destination=LAX&departureDate=2026-09-01`  
**Authentication:** Public (No token required)

#### Search Flight Schedules
**Endpoint:** `GET http://localhost:5000/api/flights/schedules?source=JFK&destination=LAX&departureDate=2026-09-01`  
**Authentication:** Public (No token required)

#### Response (200 OK):
```json
[
  {
    "id": 105,
    "flightId": 10,
    "flightNumber": "SK-501",
    "source": "JFK",
    "destination": "LAX",
    "departureTime": "2026-09-01T08:00:00Z",
    "arrivalTime": "2026-09-01T11:30:00Z",
    "gate": "B12",
    "economySeatsAvailable": 150,
    "businessSeatsAvailable": 20,
    "economyPrice": 250.00
  }
]
```

---

### Step 4: Update & Delete Flight Operations (Admin Only)

#### Update Flight Details
**Endpoint:** `PUT http://localhost:5000/api/flights/10`  
**Authentication:** `Bearer <ADMIN_JWT_TOKEN>` (`Admin`, `SuperAdmin`)

#### Request Body:
```json
{
  "gate": "C05",
  "status": "Boarding",
  "aircraftType": "Boeing 787 Dreamliner"
}
```

#### Response (200 OK):
```json
{
  "id": 10,
  "flightNumber": "SK-501",
  "gate": "C05",
  "status": "Boarding",
  "aircraftType": "Boeing 787 Dreamliner"
}
```

#### Delete Flight Template
**Endpoint:** `DELETE http://localhost:5000/api/flights/10`  
**Authentication:** `Bearer <ADMIN_JWT_TOKEN>` (`Admin`, `SuperAdmin`)

#### Response (204 No Content)

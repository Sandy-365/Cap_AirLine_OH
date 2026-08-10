# 🧳 CheckIn & Baggage Service API Guide & Workflow Manual

Welcome to the **CheckIn & Baggage Service** technical guide. This guide details how to perform online passenger check-ins, staff counter check-ins, generate digital boarding passes, and register/track checked baggage.

---

## 📌 Base URLs

- **API Gateway (Recommended)**: `http://localhost:5000/api`
- **Direct Downstream Microservice**: `http://localhost:5005/api`
- **Swagger Documentation**: `http://localhost:5000/swagger/index.html`

---

## 🔒 Role-Based Access Control (RBAC) Matrix

### **Check-In Endpoints:**
| Endpoint | HTTP Method | Allowed Roles | Description |
| :--- | :--- | :--- | :--- |
| `/checkin` | `GET` | **Logged-in Users** | Retrieve all check-in records or filter by `bookingId` |
| `/checkin/{id}` | `GET` | `Passenger`, `Admin`, `Staff`, `GroundStaff` | Retrieve check-in record details by ID |
| `/checkin/{id}/boarding-pass` | `GET` | `Passenger`, `Admin`, `Staff`, `GroundStaff` | Generate digital boarding pass details & QR code |
| `/checkin/online` | `POST` | `Passenger` | Passenger self-service Online Check-In |
| `/checkin/staff` | `POST` | `Admin`, `GroundStaff`, `Staff` | Staff-initiated counter check-in at airport |

### **Baggage Endpoints:**
| Endpoint | HTTP Method | Allowed Roles | Description |
| :--- | :--- | :--- | :--- |
| `/baggage` | `GET` | **Logged-in Users** | Get all baggage or track by `trackingNumber` / `bookingId` |
| `/baggage/{id}` | `GET` | `GroundStaff`, `Passenger`, `Dealer`, `Staff` | Retrieve baggage tracking details by ID |
| `/baggage` | `POST` | `GroundStaff`, `Staff` | Register new checked baggage at airport counter |
| `/baggage/{id}/status` | `PUT` | `GroundStaff`, `Staff` | Update baggage status *(CheckedIn, Loaded, Claimed, Lost)* |

---

## 🚀 Step-by-Step API Usage Workflow

```mermaid
flowchart TD
    Passenger[Passenger] -->|1. POST /checkin/online| A[Complete Online Check-In]
    A -->|2. GET /checkin/{id}/boarding-pass| B[Generate Digital Boarding Pass]
    GroundStaff[Ground Staff] -->|3. POST /baggage| C[Register Checked Baggage at Counter]
    GroundStaff -->|4. PUT /baggage/{id}/status| D[Update Baggage Status to Loaded/Claimed]
    Passenger -->|5. GET /baggage?trackingNumber=BAG-10029| E[Track Baggage Real-Time]
```

---

### Step 1: Online Check-In (Passenger Self-Service)

**Endpoint:** `POST http://localhost:5000/api/checkin/online?passengerName=Jane%20Doe&flightNumber=SK-501&flightId=10&departureTime=2026-09-01T08:00:00Z&fare=250.00`  
**Authentication:** `Bearer <PASSENGER_JWT_TOKEN>` (`Passenger`)

#### Request Body:
```json
{
  "bookingId": 301,
  "seatNumber": "15C"
}
```

#### Response (200 OK):
```json
{
  "id": 401,
  "bookingId": 301,
  "passengerName": "Jane Doe",
  "flightNumber": "SK-501",
  "seatNumber": "15C",
  "boardingTime": "2026-09-01T07:15:00Z",
  "gate": "B12",
  "status": "CheckedIn"
}
```

---

### Step 2: Generate Digital Boarding Pass

**Endpoint:** `GET http://localhost:5000/api/checkin/401/boarding-pass`  
**Authentication:** `Bearer <PASSENGER_JWT_TOKEN>` (`Passenger`, `Staff`)

#### Response (200 OK):
```json
{
  "checkInId": 401,
  "bookingId": 301,
  "passengerName": "Jane Doe",
  "flightNumber": "SK-501",
  "seatNumber": "15C",
  "boardingGroup": "Group B",
  "gate": "B12",
  "qrCodeData": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAA...",
  "status": "BoardingPassIssued"
}
```

---

### Step 3: Register & Track Checked Baggage

#### Register Baggage at Airport Counter (Ground Staff)
**Endpoint:** `POST http://localhost:5000/api/baggage`  
**Authentication:** `Bearer <STAFF_JWT_TOKEN>` (`GroundStaff`, `Staff`)

#### Request Body:
```json
{
  "bookingId": 301,
  "weightKg": 18.5,
  "baggageType": "Checked",
  "tagNumber": "TAG-884920"
}
```

#### Response (201 Created):
```json
{
  "id": 801,
  "bookingId": 301,
  "trackingNumber": "BAG-884920",
  "weightKg": 18.5,
  "status": "CheckedIn",
  "createdAt": "2026-09-01T07:00:00Z"
}
```

#### Update Baggage Handling Status (Ground Staff)
**Endpoint:** `PUT http://localhost:5000/api/baggage/801/status`  
**Authentication:** `Bearer <STAFF_JWT_TOKEN>` (`GroundStaff`, `Staff`)

#### Request Body:
```json
{
  "status": "Loaded"
}
```

#### Response (200 OK):
```json
{
  "id": 801,
  "trackingNumber": "BAG-884920",
  "status": "Loaded",
  "updatedAt": "2026-09-01T07:45:00Z"
}
```

#### Track Baggage in Real-Time (Passenger)
**Endpoint:** `GET http://localhost:5000/api/baggage?trackingNumber=BAG-884920`  
**Authentication:** `Bearer <JWT_TOKEN>` (Logged-in Users)

#### Response (200 OK):
```json
{
  "id": 801,
  "bookingId": 301,
  "trackingNumber": "BAG-884920",
  "weightKg": 18.5,
  "status": "Loaded"
}
```

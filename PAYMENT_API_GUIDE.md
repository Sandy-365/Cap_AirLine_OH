# 💳 Payment Service API Guide & Workflow Manual

Welcome to the **Payment Service** technical guide. This guide details how to process booking payments via Razorpay gateway integration, retrieve payment receipts, and issue administrative refunds.

---

## 📌 Base URLs

- **API Gateway (Recommended)**: `http://localhost:5000/api`
- **Direct Downstream Microservice**: `http://localhost:5004/api`
- **Swagger Documentation**: `http://localhost:5000/swagger/index.html`

---

## 🔒 Role-Based Access Control (RBAC) Matrix

| Endpoint | HTTP Method | Allowed Roles | Description |
| :--- | :--- | :--- | :--- |
| `/payments/{id}` | `GET` | `Passenger`, `Dealer`, `Admin`, `SuperAdmin` | Retrieve payment transaction details by ID |
| `/payments` | `POST` | `Passenger`, `Dealer` | Process or create a new Razorpay payment for a booking |
| `/payments/{id}/refund` | `POST` | `Admin`, `SuperAdmin`, `FinancialAdmin` | Initiate a refund for a processed payment transaction |

---

## 🚀 Step-by-Step API Usage Workflow

```mermaid
flowchart TD
    User[Passenger / Dealer] -->|1. POST /payments| A[Submit Payment Request with Razorpay Details]
    A -->|2. Payment Verified & Confirmed| B[Booking Status Updated to Confirmed]
    User -->|3. GET /payments/{id}| C[View Payment Receipt & Transaction Details]
    Admin[Admin / FinancialAdmin] -->|4. POST /payments/{id}/refund| D[Process Refund]
```

---

### Step 1: Process Booking Payment (Passenger / Dealer)

**Endpoint:** `POST http://localhost:5000/api/payments`  
**Authentication:** `Bearer <JWT_TOKEN>` (`Passenger`, `Dealer`)

#### Request Body:
```json
{
  "bookingId": 301,
  "amount": 500.00,
  "currency": "INR",
  "paymentMethod": "Razorpay",
  "razorpayPaymentId": "pay_P892019482",
  "razorpayOrderId": "order_O891049281",
  "razorpaySignature": "4a8e8f810148f9810..."
}
```

#### Response (200 OK):
```json
{
  "id": 901,
  "bookingId": 301,
  "transactionId": "TXN-9948201",
  "amount": 500.00,
  "currency": "INR",
  "status": "Success",
  "paymentMethod": "Razorpay",
  "paidAt": "2026-08-06T11:35:00Z"
}
```

---

### Step 2: Retrieve Payment Receipt

**Endpoint:** `GET http://localhost:5000/api/payments/901`  
**Authentication:** `Bearer <JWT_TOKEN>` (`Passenger`, `Dealer`, `Admin`, `SuperAdmin`)

#### Response (200 OK):
```json
{
  "id": 901,
  "bookingId": 301,
  "transactionId": "TXN-9948201",
  "amount": 500.00,
  "currency": "INR",
  "status": "Success",
  "paymentMethod": "Razorpay",
  "razorpayPaymentId": "pay_P892019482",
  "paidAt": "2026-08-06T11:35:00Z"
}
```

---

### Step 3: Initiate Refund (Admin / FinancialAdmin)

**Endpoint:** `POST http://localhost:5000/api/payments/901/refund`  
**Authentication:** `Bearer <ADMIN_JWT_TOKEN>` (`Admin`, `SuperAdmin`, `FinancialAdmin`)

#### Response (200 OK):
```json
{
  "id": 901,
  "bookingId": 301,
  "transactionId": "TXN-9948201",
  "amount": 500.00,
  "status": "Refunded",
  "refundedAt": "2026-08-06T11:45:00Z"
}
```

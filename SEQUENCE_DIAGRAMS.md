# Airline Management System - Sequence Diagrams Reference

This document provides detailed **Sequence Diagrams** (powered by Mermaid) depicting the runtime message flows, synchronous/asynchronous inter-service interactions, authorization handshakes, database transactions, exception paths, and external gateway integrations across the **Airline Management System** microservices platform.

---

## Table of Contents
1. [Architecture & Sequence Actors Overview](#1-architecture--sequence-actors-overview)
2. [Passenger Identity & Access Management](#2-passenger-identity--access-management)
   - [2.1 Passenger Registration & Email Verification OTP Flow](#21-passenger-registration--email-verification-otp-flow)
   - [2.2 Passenger Email OTP Verification & JWT Token Issuance](#22-passenger-email-otp-verification--jwt-token-issuance)
   - [2.3 Passenger Login & Authentication](#23-passenger-login--authentication)
   - [2.4 Passenger Forgot Password & Password Reset Flow](#24-passenger-forgot-password--password-reset-flow)
   - [2.5 Passenger Profile Update & Status Management](#25-passenger-profile-update--status-management)
3. [Backoffice Staff Authentication & Administration](#3-backoffice-staff-authentication--administration)
   - [3.1 Backoffice User Provisioning (SuperAdmin / HR / Admin)](#31-backoffice-user-provisioning-superadmin--hr--admin)
   - [3.2 Backoffice Staff Login & RBAC JWT Issuance](#32-backoffice-staff-login--rbac-jwt-issuance)
   - [3.3 Backoffice User Profile & Status Modification](#33-backoffice-user-profile--status-modification)
   - [3.4 Backoffice Forgot Password & Password Reset Workflow](#34-backoffice-forgot-password--password-reset-workflow)
4. [Flight Catalog & Schedule Operations](#4-flight-catalog--schedule-operations)
   - [4.1 Flight Template Creation (Admin / SuperAdmin)](#41-flight-template-creation-admin--superadmin)
   - [4.2 Flight Schedule Generation & Inventory Setup](#42-flight-schedule-generation--inventory-setup)
   - [4.3 Public Flight Search & Schedule Query Flow](#43-public-flight-search--schedule-query-flow)
   - [4.4 Flight Template Modification & Removal](#44-flight-template-modification--removal)
5. [End-to-End Flight Booking Lifecycle](#5-end-to-end-flight-booking-lifecycle)
   - [5.1 Multi-Passenger Booking Creation, Seat Deduction & PNR Generation](#51-multi-passenger-booking-creation-seat-deduction--pnr-generation)
   - [5.2 Booking Inquiries (By PNR, User History, or Schedule)](#52-booking-inquiries-by-pnr-user-history-or-schedule)
   - [5.3 Full Booking Cancellation & Dynamic Seat Inventory Release](#53-full-booking-cancellation--dynamic-seat-inventory-release)
   - [5.4 Partial Companion Passenger Cancellation](#54-partial-companion-passenger-cancellation)
6. [Payment Processing & Razorpay Gateway Integration](#6-payment-processing--razorpay-gateway-integration)
   - [6.1 Razorpay Order Creation Flow](#61-razorpay-order-creation-flow)
   - [6.2 Razorpay Signature Verification & Cross-Service Booking Confirmation](#62-razorpay-signature-verification--cross-service-booking-confirmation)
   - [6.3 Direct Payment Processing Workflow](#63-direct-payment-processing-workflow)
   - [6.4 Payment Refund Processing Flow](#64-payment-refund-processing-flow)
7. [Digital Online Check-In & Boarding Pass Issuance](#7-digital-online-check-in--boarding-pass-issuance)
   - [7.1 5-Hour Window Check-In, Seat Assignment & QR Generation](#71-5-hour-window-check-in-seat-assignment--qr-generation)
   - [7.2 Fetching Digital Boarding Passes by Booking ID](#72-fetching-digital-boarding-passes-by-booking-id)
8. [Asynchronous Background Processing](#8-asynchronous-background-processing)
   - [8.1 Autonomous Flight Schedule Auto-Completion Loop](#81-autonomous-flight-schedule-auto-completion-loop)
9. [Cross-Service Operational & Financial Reporting](#9-cross-service-operational--financial-reporting)
   - [9.1 Backoffice Booking & Financial Report Aggregation Flow](#91-backoffice-booking--financial-report-aggregation-flow)

---

## 1. Architecture & Sequence Actors Overview

The sequence diagrams model interactions between the following primary participants:

```mermaid
sequenceDiagram
    autonumber
    actor Client as 🌐 Client (Web / Angular / Mobile)
    participant Gateway as 🚪 ApiGateway (Port 5000)
    participant BackofficeSvc as 🏢 BackOfficeService (Port 5001)
    participant FlightOpsSvc as ✈️ FlightOpsService (Port 5002)
    participant PaymentSvc as 💳 PaymentService (Port 5004)
    participant PassengerSvc as 👤 PassengerService (Port 5007)
    participant Razorpay as 🛡️ Razorpay API
    participant Worker as ⏱️ ScheduleCompletionWorker
    participant DB as 🗄️ SQL Server Databases

    Note over Client,DB: Autonomous Microservices Architecture with Database-per-Service
```

---

## 2. Passenger Identity & Access Management

### 2.1 Passenger Registration & Email Verification OTP Flow

When a new passenger registers on the portal, an account profile is created in an unverified state (`IsEmailVerified = false`), a secure OTP is generated with a 15-minute expiration, and a confirmation email is dispatched.

```mermaid
sequenceDiagram
    autonumber
    actor Passenger as 👤 Passenger
    participant Gateway as 🚪 ApiGateway
    participant Controller as 🎮 PassengerAuthController
    participant Service as ⚙️ PassengerAuthService
    participant Hasher as 🔒 PasswordHasher
    participant Repo as 🗃️ PassengerProfileRepository
    participant DB as 🗄️ Airline_PassengerDB

    Passenger->>Gateway: POST /identity/passenger/register (PassengerRegisterDto)
    Gateway->>Controller: Forward to /api/auth/register
    Controller->>Service: RegisterAsync(dto)
    
    Service->>Repo: GetByEmailAsync(dto.Email)
    Repo->>DB: SELECT * FROM PassengerProfiles WHERE Email = @Email
    DB-->>Repo: null (Account doesn't exist)
    Repo-->>Service: null
    
    Service->>Hasher: HashPassword(dto.Password)
    Hasher-->>Service: passwordHash
    
    Service->>Service: Generate 6-Digit OTP Token & Expiry (15 mins)
    
    Service->>Repo: AddAsync(PassengerProfile)
    Repo->>DB: INSERT INTO PassengerProfiles (Email, PasswordHash, VerificationToken, TokenExpiry, IsEmailVerified=false)
    DB-->>Repo: 1 Row Affected (Generated Id)
    Repo-->>Service: Saved Entity
    
    Service->>Service: SendVerificationEmail(Email, OTP)
    Service-->>Controller: Task Completed
    Controller-->>Gateway: 200 OK { message: "Registration successful. Check your email for the OTP." }
    Gateway-->>Passenger: 200 OK Response
```

---

### 2.2 Passenger Email OTP Verification & JWT Token Issuance

The passenger submits the 6-digit OTP code received in their email to activate the account and receive an initial JWT Bearer authentication token.

```mermaid
sequenceDiagram
    autonumber
    actor Passenger as 👤 Passenger
    participant Gateway as 🚪 ApiGateway
    participant Controller as 🎮 PassengerAuthController
    participant Service as ⚙️ PassengerAuthService
    participant JwtGen as 🔑 JwtTokenGenerator
    participant Repo as 🗃️ PassengerProfileRepository
    participant DB as 🗄️ Airline_PassengerDB

    Passenger->>Gateway: POST /identity/passenger/verify (PassengerVerifyDto: { email, token })
    Gateway->>Controller: Forward to /api/auth/verify
    Controller->>Service: VerifyAsync(dto)
    
    Service->>Repo: GetByEmailAsync(dto.Email)
    Repo->>DB: SELECT * FROM PassengerProfiles WHERE Email = @Email
    DB-->>Repo: PassengerProfile Record
    Repo-->>Service: PassengerProfile Entity
    
    alt Profile Not Found
        Service-->>Controller: throw InvalidOperationException("User not found")
        Controller-->>Gateway: 400 Bad Request { message: "User not found" }
    else Already Verified
        Service-->>Controller: throw InvalidOperationException("Email is already verified")
        Controller-->>Gateway: 400 Bad Request { message: "Email is already verified" }
    else Invalid / Expired OTP
        Service-->>Controller: throw InvalidOperationException("Invalid or expired verification token")
        Controller-->>Gateway: 400 Bad Request { message: "Invalid or expired verification token" }
    else OTP Matches & Valid
        Service->>Service: profile.IsEmailVerified = true, Clear OTP Fields
        Service->>Repo: UpdateAsync(profile)
        Repo->>DB: UPDATE PassengerProfiles SET IsEmailVerified = 1, VerificationToken = null
        DB-->>Repo: Success
        
        Service->>JwtGen: GenerateToken(UserId, Email, "Passenger", FullName)
        JwtGen-->>Service: JWT Bearer Token String
        
        Service-->>Controller: AuthResponseDto { Token, UserId, Email, Role, FullName }
        Controller-->>Gateway: 200 OK AuthResponseDto
        Gateway-->>Passenger: 200 OK { token: "...", role: "Passenger", ... }
    end
```

---

### 2.3 Passenger Login & Authentication

Existing passengers authenticate with their email and password. The system checks email verification status, verifies the cryptographic password hash, and produces a signed JWT claim token.

```mermaid
sequenceDiagram
    autonumber
    actor Passenger as 👤 Passenger
    participant Gateway as 🚪 ApiGateway
    participant Controller as 🎮 PassengerAuthController
    participant Service as ⚙️ PassengerAuthService
    participant Hasher as 🔒 PasswordHasher
    participant JwtGen as 🔑 JwtTokenGenerator
    participant Repo as 🗃️ PassengerProfileRepository
    participant DB as 🗄️ Airline_PassengerDB

    Passenger->>Gateway: POST /identity/passenger/login (PassengerLoginDto)
    Gateway->>Controller: Forward to /api/auth/login
    Controller->>Service: LoginAsync(dto)
    
    Service->>Repo: GetByEmailAsync(dto.Email)
    Repo->>DB: SELECT * FROM PassengerProfiles WHERE Email = @Email
    DB-->>Repo: PassengerProfile Record
    Repo-->>Service: PassengerProfile Entity
    
    alt Account Not Found
        Service-->>Controller: throw UnauthorizedAccessException("Invalid email or password")
        Controller-->>Gateway: 401 Unauthorized
    else Account Deactivated
        Service-->>Controller: throw UnauthorizedAccessException("Account is deactivated")
        Controller-->>Gateway: 401 Unauthorized
    else Email Not Verified
        Service-->>Controller: throw UnauthorizedAccessException("Please verify your email before logging in.")
        Controller-->>Gateway: 401 Unauthorized
    else Password Mismatch
        Service->>Hasher: VerifyPassword(dto.Password, profile.PasswordHash)
        Hasher-->>Service: false
        Service-->>Controller: throw UnauthorizedAccessException("Invalid email or password")
        Controller-->>Gateway: 401 Unauthorized
    else Valid Credentials
        Service->>Hasher: VerifyPassword(dto.Password, profile.PasswordHash)
        Hasher-->>Service: true
        
        Service->>JwtGen: GenerateToken(profile.UserId, profile.Email, "Passenger", profile.FullName)
        JwtGen-->>Service: Signed JWT String
        
        Service-->>Controller: AuthResponseDto { Token, UserId, Email, Role, FullName }
        Controller-->>Gateway: 200 OK AuthResponseDto
        Gateway-->>Passenger: 200 OK (Token saved to localStorage)
    end
```

---

### 2.4 Passenger Forgot Password & Password Reset Flow

A streamlined self-service password recovery flow that issues a 15-minute reset OTP and verifies the OTP before updating the hashed password.

```mermaid
sequenceDiagram
    autonumber
    actor Passenger as 👤 Passenger
    participant Gateway as 🚪 ApiGateway
    participant Controller as 🎮 PassengerAuthController
    participant Service as ⚙️ PassengerAuthService
    participant Hasher as 🔒 PasswordHasher
    participant Repo as 🗃️ PassengerProfileRepository
    participant DB as 🗄️ Airline_PassengerDB

    %% Step 1: Forgot Password Request
    Passenger->>Gateway: POST /identity/passenger/forgot-password (PassengerForgotPasswordDto: { email })
    Gateway->>Controller: Forward to /api/auth/forgot-password
    Controller->>Service: ForgotPasswordAsync(dto.Email)
    Service->>Repo: GetByEmailAsync(email)
    Repo->>DB: SELECT * FROM PassengerProfiles WHERE Email = @Email
    DB-->>Repo: PassengerProfile
    Service->>Service: Generate 6-Digit OTP & ResetExpiry (15 Mins)
    Service->>Repo: UpdateAsync(profile)
    Repo->>DB: UPDATE PassengerProfiles SET PasswordResetToken = @OTP, ResetExpiry = @Expiry
    DB-->>Repo: Success
    Service-->>Controller: otpToken
    Controller-->>Gateway: 200 OK { token: "123456", resetToken: "123456", expiresInMinutes: 15 }
    Gateway-->>Passenger: 200 OK (Frontend displays OTP in notification/alert)

    %% Step 2: Reset Password Submission
    Passenger->>Gateway: POST /identity/passenger/reset-password (PassengerResetPasswordDto: { email, token, newPassword })
    Gateway->>Controller: Forward to /api/auth/reset-password
    Controller->>Service: ResetPasswordAsync(dto)
    Service->>Repo: GetByEmailAsync(dto.Email)
    Repo->>DB: SELECT * FROM PassengerProfiles WHERE Email = @Email
    DB-->>Repo: PassengerProfile
    
    alt Token Mismatch or Expired
        Service-->>Controller: throw InvalidOperationException("Invalid or expired reset token")
        Controller-->>Gateway: 400 Bad Request
    else Token Valid
        Service->>Hasher: HashPassword(dto.NewPassword)
        Hasher-->>Service: newPasswordHash
        Service->>Service: profile.PasswordHash = newPasswordHash, Clear ResetToken
        Service->>Repo: UpdateAsync(profile)
        Repo->>DB: UPDATE PassengerProfiles SET PasswordHash = @newHash, PasswordResetToken = null
        DB-->>Repo: Success
        Service-->>Controller: Task Completed
        Controller-->>Gateway: 200 OK { message: "Password reset successfully." }
        Gateway-->>Passenger: 200 OK Response
    end
```

---

### 2.5 Passenger Profile Update & Status Management

```mermaid
sequenceDiagram
    autonumber
    actor User as 👤 Passenger / 👑 Admin
    participant Gateway as 🚪 ApiGateway
    participant Controller as 🎮 PassengersController
    participant Service as ⚙️ PassengerAuthService
    participant Repo as 🗃️ PassengerProfileRepository
    participant DB as 🗄️ Airline_PassengerDB

    %% Update Profile
    User->>Gateway: PUT /api/passengers/users/{userId}/profile [Bearer Token]
    Gateway->>Controller: PUT /api/passengers/users/{userId}/profile
    Note over Controller: Verify JWT UserID matches route or has Admin role
    Controller->>Service: UpdateProfileAsync(userId, updateDto)
    Service->>Repo: GetByIdAsync(userId)
    Repo->>DB: SELECT * FROM PassengerProfiles WHERE Id = @userId
    DB-->>Repo: PassengerProfile
    Service->>Service: Apply Phone, Address, Medical, Dietary, Companion data
    Service->>Repo: UpdateAsync(profile)
    Repo->>DB: UPDATE PassengerProfiles SET ...
    DB-->>Repo: Success
    Service-->>Controller: Updated PassengerProfileDto
    Controller-->>Gateway: 200 OK
    Gateway-->>User: 200 OK Updated Profile
```

---

## 3. Backoffice Staff Authentication & Administration

### 3.1 Backoffice User Provisioning (SuperAdmin / HR / Admin)

SuperAdmins, Admins, and HR managers provision internal staff accounts across all departments (HR, FinancialAdmin, Staff/Crew, GroundStaff, Dealer).

```mermaid
sequenceDiagram
    autonumber
    actor AdminUser as 👑 SuperAdmin / HR Admin
    participant Gateway as 🚪 ApiGateway
    participant Controller as 🎮 BackofficeController
    participant Service as ⚙️ BackofficeAuthService
    participant Hasher as 🔒 PasswordHasher
    participant Repo as 🗃️ BackofficeProfileRepository
    participant DB as 🗄️ Airline_BackOfficeDB

    AdminUser->>Gateway: POST /api/backoffice/users [Bearer Token (Roles: SuperAdmin, Admin, HR)]
    Gateway->>Controller: Forward to /api/backoffice/users
    Controller->>Service: RegisterAsync(BackofficeRegisterDto)
    
    Service->>Repo: GetByEmailAsync(dto.Email)
    Repo->>DB: SELECT * FROM BackofficeProfiles WHERE Email = @Email
    DB-->>Repo: null (Email not in use)
    Repo-->>Service: null
    
    Service->>Hasher: HashPassword(dto.Password)
    Hasher-->>Service: passwordHash
    
    Service->>Repo: AddAsync(BackofficeProfile)
    Repo->>DB: INSERT INTO BackofficeProfiles (Email, FullName, Role, Department, AirportStationCode, PasswordHash, IsActive=true)
    DB-->>Repo: 1 Row Affected
    Repo-->>Service: Saved Entity
    
    Service-->>Controller: Task Completed
    Controller-->>Gateway: 200 OK { message: "User registered successfully." }
    Gateway-->>AdminUser: 200 OK Response
```

---

### 3.2 Backoffice Staff Login & RBAC JWT Issuance

All backoffice personnel log in via a unified portal endpoint (`/api/backoffice/auth/login`), receiving a JWT embedded with their specific administrative role claims.

```mermaid
sequenceDiagram
    autonumber
    actor Staff as 🧑‍💼 Staff / Admin / Dealer
    participant Gateway as 🚪 ApiGateway
    participant Controller as 🎮 BackofficeAuthController
    participant Service as ⚙️ BackofficeAuthService
    participant Hasher as 🔒 PasswordHasher
    participant JwtGen as 🔑 JwtTokenGenerator
    participant Repo as 🗃️ BackofficeProfileRepository
    participant DB as 🗄️ Airline_BackOfficeDB

    Staff->>Gateway: POST /identity/admin/login or /identity/staff/login
    Gateway->>Controller: Forward to /api/backoffice/auth/login
    Controller->>Service: LoginAsync(BackofficeLoginDto)
    
    Service->>Repo: GetByEmailAsync(dto.Email)
    Repo->>DB: SELECT * FROM BackofficeProfiles WHERE Email = @Email
    DB-->>Repo: BackofficeProfile Record
    Repo-->>Service: BackofficeProfile Entity
    
    alt Account Not Found or Inactive
        Service-->>Controller: throw UnauthorizedAccessException("Invalid email or password")
        Controller-->>Gateway: 401 Unauthorized
    else Password Verification
        Service->>Hasher: VerifyPassword(dto.Password, profile.PasswordHash)
        alt Password Invalid
            Hasher-->>Service: false
            Service-->>Controller: throw UnauthorizedAccessException("Invalid email or password")
            Controller-->>Gateway: 401 Unauthorized
        else Password Valid
            Hasher-->>Service: true
            Service->>JwtGen: GenerateToken(profile.UserId, profile.Email, profile.Role, profile.FullName)
            JwtGen-->>Service: Signed JWT Bearer Token
            Service-->>Controller: AuthResponseDto { Token, UserId, Email, Role, FullName }
            Controller-->>Gateway: 200 OK AuthResponseDto
            Gateway-->>Staff: 200 OK (Access granted with RBAC permissions)
        end
    end
```

---

### 3.3 Backoffice User Profile & Status Modification

```mermaid
sequenceDiagram
    autonumber
    actor SuperAdmin as 👑 SuperAdmin / HR
    participant Gateway as 🚪 ApiGateway
    participant Controller as 🎮 BackofficeController
    participant Service as ⚙️ BackofficeAuthService
    participant Repo as 🗃️ BackofficeProfileRepository
    participant DB as 🗄️ Airline_BackOfficeDB

    %% Activate / Deactivate Staff User
    SuperAdmin->>Gateway: PUT /api/backoffice/users/{userId}/status [Bearer Token]
    Gateway->>Controller: PUT /api/backoffice/users/{userId}/status
    Controller->>Service: UpdateUserStatusAsync(userId, dto.IsActive)
    Service->>Repo: GetByIdAsync(userId)
    Repo->>DB: SELECT * FROM BackofficeProfiles WHERE Id = @userId
    DB-->>Repo: BackofficeProfile
    Service->>Service: profile.IsActive = dto.IsActive
    Service->>Repo: UpdateAsync(profile)
    Repo->>DB: UPDATE BackofficeProfiles SET IsActive = @isActive, UpdatedAt = UTC
    DB-->>Repo: Success
    Service-->>Controller: Done
    Controller-->>Gateway: 200 OK { message: "Status updated" }
    Gateway-->>SuperAdmin: 200 OK Status Modified
```

---

### 3.4 Backoffice Forgot Password & Password Reset Workflow

```mermaid
sequenceDiagram
    autonumber
    actor Staff as 🧑‍💼 Backoffice Staff
    participant Gateway as 🚪 ApiGateway
    participant Controller as 🎮 BackofficeAuthController
    participant Service as ⚙️ BackofficeAuthService
    participant Hasher as 🔒 PasswordHasher
    participant Repo as 🗃️ BackofficeProfileRepository
    participant DB as 🗄️ Airline_BackOfficeDB

    Staff->>Gateway: POST /identity/admin/forgot-password (BackofficeForgotPasswordDto)
    Gateway->>Controller: Forward to /api/backoffice/auth/forgot-password
    Controller->>Service: ForgotPasswordAsync(dto.Email)
    Service->>Repo: GetByEmailAsync(dto.Email)
    Repo->>DB: SELECT * FROM BackofficeProfiles WHERE Email = @Email
    DB-->>Repo: BackofficeProfile
    Service->>Service: Generate 6-Digit OTP Token & 15-Minute Expiry
    Service->>Repo: UpdateAsync(profile)
    Repo->>DB: UPDATE BackofficeProfiles SET PasswordResetToken = @OTP, ResetExpiry = @Expiry
    DB-->>Repo: Success
    Service-->>Controller: otpToken
    Controller-->>Gateway: 200 OK { message: "OTP generated", token: "123456", expiresInMinutes: 15 }
    Gateway-->>Staff: 200 OK (Displayed in UI Alert)

    Staff->>Gateway: POST /identity/admin/reset-password (BackofficeResetPasswordDto)
    Gateway->>Controller: Forward to /api/backoffice/auth/reset-password
    Controller->>Service: ResetPasswordAsync(dto)
    Service->>Repo: GetByEmailAsync(dto.Email)
    Repo->>DB: SELECT * FROM BackofficeProfiles WHERE Email = @Email
    DB-->>Repo: BackofficeProfile
    alt Token Valid
        Service->>Hasher: HashPassword(dto.NewPassword)
        Hasher-->>Service: newHash
        Service->>Service: profile.PasswordHash = newHash, Clear Token
        Service->>Repo: UpdateAsync(profile)
        Repo->>DB: UPDATE BackofficeProfiles SET PasswordHash = @newHash, PasswordResetToken = null
        DB-->>Repo: Success
        Service-->>Controller: Done
        Controller-->>Gateway: 200 OK { message: "Password reset successfully." }
        Gateway-->>Staff: 200 OK Response
    end
```

---

## 4. Flight Catalog & Schedule Operations

### 4.1 Flight Template Creation (Admin / SuperAdmin)

Flight templates establish the baseline route, pricing tiers, and cabin class capacities for a given flight number.

```mermaid
sequenceDiagram
    autonumber
    actor Admin as 👑 Admin / SuperAdmin
    participant Gateway as 🚪 ApiGateway
    participant Controller as 🎮 FlightsController
    participant FlightSvc as ⚙️ FlightService
    participant Repo as 🗃️ FlightRepository
    participant DB as 🗄️ Airline_FlightOpsDB

    Admin->>Gateway: POST /flights [Bearer Token (Roles: Admin, SuperAdmin)]
    Gateway->>Controller: Forward to POST /api/flights
    Controller->>FlightSvc: CreateFlightAsync(CreateFlightDto)
    
    FlightSvc->>FlightSvc: Validate Aircraft, Seats (Economy+Business+First = Total), Prices > 0
    FlightSvc->>FlightSvc: Build Flight Entity (Status = Scheduled)
    
    FlightSvc->>Repo: AddAsync(flight)
    Repo->>DB: INSERT INTO Flights (FlightNumber, Airline, Source, Destination, DepartureTime, ArrivalTime, EconomySeats, BusinessSeats, FirstSeats, EconomyPrice, BusinessPrice, FirstClassPrice, Status)
    DB-->>Repo: 1 Row Inserted (Id generated)
    Repo-->>FlightSvc: Persisted Flight Entity
    
    FlightSvc-->>Controller: FlightDto (Mapped from Entity)
    Controller-->>Gateway: 201 CreatedAtAction [Location: /api/flights/{id}]
    Gateway-->>Admin: 201 Created Response
```

---

### 4.2 Flight Schedule Generation & Inventory Setup

Schedules are concrete departure instances derived from flight templates, managing day-to-day seat inventory.

```mermaid
sequenceDiagram
    autonumber
    actor OpsStaff as ✈️ Admin / SuperAdmin / Staff
    participant Gateway as 🚪 ApiGateway
    participant Controller as 🎮 FlightsController
    participant ScheduleSvc as ⚙️ FlightScheduleService
    participant FlightRepo as 🗃️ FlightRepository
    participant ScheduleRepo as 🗃️ FlightScheduleRepository
    participant DB as 🗄️ Airline_FlightOpsDB

    OpsStaff->>Gateway: POST /flights/schedules (CreateScheduleDto: { flightId, departureTime, arrivalTime, ... })
    Gateway->>Controller: Forward to POST /api/flights/schedules
    Controller->>ScheduleSvc: CreateScheduleAsync(dto)
    
    ScheduleSvc->>FlightRepo: GetByIdAsync(dto.FlightId)
    FlightRepo->>DB: SELECT * FROM Flights WHERE Id = @flightId
    DB-->>FlightRepo: Flight Entity
    FlightRepo-->>ScheduleSvc: Flight Template Data
    
    ScheduleSvc->>ScheduleSvc: Instantiate FlightSchedule from Flight Defaults (or overrides)
    Note over ScheduleSvc: EconomySeats = flight.EconomySeats<br/>BusinessSeats = flight.BusinessSeats<br/>FirstSeats = flight.FirstSeats<br/>Status = "Scheduled"
    
    ScheduleSvc->>ScheduleRepo: AddAsync(schedule)
    ScheduleRepo->>DB: INSERT INTO FlightSchedules (FlightId, DepartureTime, ArrivalTime, EconomySeats, BusinessSeats, FirstSeats, EconomyPrice, BusinessPrice, FirstClassPrice, Status)
    DB-->>ScheduleRepo: 1 Row Inserted
    ScheduleRepo-->>ScheduleSvc: Saved Schedule Entity
    
    ScheduleSvc-->>Controller: FlightScheduleDto
    Controller-->>Gateway: 201 CreatedAtAction
    Gateway-->>OpsStaff: 201 Created Schedule Instance
```

---

### 4.3 Public Flight Search & Schedule Query Flow

Passengers and Dealers query flight schedules by origin, destination, and departure date without requiring authentication.

```mermaid
sequenceDiagram
    autonumber
    actor Customer as 👤 Passenger / 🎟️ Dealer
    participant Gateway as 🚪 ApiGateway
    participant Controller as 🎮 FlightsController
    participant ScheduleSvc as ⚙️ FlightScheduleService
    participant ScheduleRepo as 🗃️ FlightScheduleRepository
    participant DB as 🗄️ Airline_FlightOpsDB

    Customer->>Gateway: GET /flights/schedules?source=DEL&destination=BOM&departureDate=2026-08-25
    Gateway->>Controller: Forward to /api/flights/schedules
    Controller->>ScheduleSvc: SearchSchedulesAsync("DEL", "BOM", "2026-08-25", null)
    
    ScheduleSvc->>ScheduleRepo: SearchSchedulesAsync(source, destination, parsedDate, flightId)
    ScheduleRepo->>DB: SELECT fs.*, f.* FROM FlightSchedules fs JOIN Flights f ON fs.FlightId = f.Id WHERE f.Source = @source AND f.Destination = @dest AND CAST(fs.DepartureTime AS DATE) = @date AND fs.Status != 'Cancelled'
    DB-->>ScheduleRepo: Matching Schedules with Flight Navigation
    ScheduleRepo-->>ScheduleSvc: List<FlightSchedule>
    
    ScheduleSvc->>ScheduleSvc: Map Entities to List<FlightScheduleDto>
    ScheduleSvc-->>Controller: List<FlightScheduleDto>
    Controller-->>Gateway: 200 OK [Schedules with available seats and prices]
    Gateway-->>Customer: 200 OK Search Results
```

---

### 4.4 Flight Template Modification & Removal

```mermaid
sequenceDiagram
    autonumber
    actor Admin as 👑 Admin / SuperAdmin
    participant Gateway as 🚪 ApiGateway
    participant Controller as 🎮 FlightsController
    participant FlightSvc as ⚙️ FlightService
    participant Repo as 🗃️ FlightRepository
    participant DB as 🗄️ Airline_FlightOpsDB

    %% Update Flight
    Admin->>Gateway: PUT /flights/{id} [Bearer Token]
    Gateway->>Controller: PUT /api/flights/{id} (UpdateFlightDto)
    Controller->>FlightSvc: UpdateFlightAsync(id, dto)
    FlightSvc->>Repo: GetByIdAsync(id)
    Repo->>DB: SELECT * FROM Flights WHERE Id = @id
    DB-->>Repo: Flight Entity
    FlightSvc->>FlightSvc: Apply Non-Null Fields (Gate, Aircraft, Times, Crew)
    FlightSvc->>Repo: UpdateAsync(flight)
    Repo->>DB: UPDATE Flights SET Gate = @gate, Aircraft = @aircraft ...
    DB-->>Repo: Success
    FlightSvc-->>Controller: Updated FlightDto
    Controller-->>Gateway: 200 OK FlightDto
    Gateway-->>Admin: 200 OK Updated

    %% Delete Flight
    Admin->>Gateway: DELETE /flights/{id} [Bearer Token]
    Gateway->>Controller: DELETE /api/flights/{id}
    Controller->>FlightSvc: DeleteFlightAsync(id)
    FlightSvc->>Repo: DeleteAsync(id)
    Repo->>DB: DELETE FROM Flights WHERE Id = @id
    DB-->>Repo: Success
    FlightSvc-->>Controller: Task Completed
    Controller-->>Gateway: 204 No Content
    Gateway-->>Admin: 204 No Content Response
```

---

## 5. End-to-End Flight Booking Lifecycle

### 5.1 Multi-Passenger Booking Creation, Seat Deduction & PNR Generation

When booking, the system extracts the user identity from JWT claims, verifies seat inventory in-process on the `FlightSchedule` (or `Flight`), deducts seats, generates a unique 6-character PNR, inserts passenger manifest items, and marks the initial state as `Pending` payment.

```mermaid
sequenceDiagram
    autonumber
    actor Passenger as 👤 Passenger / 🎟️ Dealer
    participant Gateway as 🚪 ApiGateway
    participant Controller as 🎮 BookingsController
    participant BookingSvc as ⚙️ BookingService
    participant ScheduleSvc as ⚙️ FlightScheduleService
    participant BookingRepo as 🗃️ BookingRepository
    participant ScheduleRepo as 🗃️ FlightScheduleRepository
    participant DB as 🗄️ Airline_FlightOpsDB

    Passenger->>Gateway: POST /bookings (CreateBookingDto) [Bearer JWT]
    Gateway->>Controller: Forward to POST /api/bookings
    
    Note over Controller: Extract UserId, Email, Name from JWT Claims if omitted
    Controller->>BookingSvc: CreateBookingAsync(dto)
    
    %% In-Process Schedule Verification
    BookingSvc->>ScheduleSvc: GetScheduleAsync(dto.ScheduleId.Value)
    ScheduleSvc->>ScheduleRepo: GetByIdAsync(dto.ScheduleId)
    ScheduleRepo->>DB: SELECT * FROM FlightSchedules WHERE Id = @id
    DB-->>ScheduleRepo: FlightSchedule Record
    ScheduleRepo-->>ScheduleSvc: FlightSchedule Entity
    ScheduleSvc-->>BookingSvc: FlightScheduleDto
    
    alt Schedule Cancelled / Completed
        BookingSvc-->>Controller: throw InvalidScheduleException
        Controller-->>Gateway: 400 Bad Request
    else Flight Already Departed (DepartureTime < Now_IST)
        BookingSvc-->>Controller: throw FlightAlreadyDepartedException
        Controller-->>Gateway: 400 Bad Request
    else Available Seats < Requested Seats
        BookingSvc-->>Controller: throw SeatsNotAvailableException
        Controller-->>Gateway: 400 Bad Request { errorCode: "SEATS_NOT_AVAILABLE", availableSeats: X }
    else Seat Class Available & Valid
        BookingSvc->>BookingSvc: Determine UnitPrice (Economy / Business / First)
        BookingSvc->>BookingSvc: Calculate TotalAmount = UnitPrice * PassengerCount
        BookingSvc->>BookingSvc: Generate Unique 6-Char PNR (e.g. "XY789K")
        BookingSvc->>BookingSvc: Construct Booking & List<BookingPassenger> Entities (Status = Pending)
        
        %% Save Booking Record
        BookingSvc->>BookingRepo: AddAsync(booking)
        BookingRepo->>DB: INSERT INTO Bookings (UserId, FlightId, ScheduleId, PNR, SeatClass, TotalPassengers, TotalAmount, Status="Pending", PaymentStatus="Pending")<br/>INSERT INTO BookingPassengers (BookingId, Name, Age, Gender, Aadhar, Passport, Dietary, Fare, Status="Confirmed")
        DB-->>BookingRepo: Booking Created (Generated BookingId)
        BookingRepo-->>BookingSvc: Persisted Booking
        
        %% Deduct Seats In-Process
        BookingSvc->>ScheduleSvc: BookScheduleSeatAsync(ScheduleId, SeatClass, PassengerCount)
        ScheduleSvc->>ScheduleRepo: DeductSeatInventory(ScheduleId, SeatClass, count)
        ScheduleRepo->>DB: UPDATE FlightSchedules SET EconomySeats = EconomySeats - @count WHERE Id = @id
        DB-->>ScheduleRepo: Inventory Updated
        ScheduleRepo-->>ScheduleSvc: Success
        ScheduleSvc-->>BookingSvc: Success
        
        BookingSvc-->>Controller: BookingDto (PNR, TotalAmount, Passengers, Status="Pending")
        Controller-->>Gateway: 201 CreatedAtAction [/api/bookings/{id}]
        Gateway-->>Passenger: 201 Created (Ready to initiate Razorpay payment)
    end
```

---

### 5.2 Booking Inquiries (By PNR, User History, or Schedule)

```mermaid
sequenceDiagram
    autonumber
    actor Client as 🌐 Passenger / Admin / Ground Staff
    participant Gateway as 🚪 ApiGateway
    participant Controller as 🎮 BookingsController
    participant BookingSvc as ⚙️ BookingService
    participant Repo as 🗃️ BookingRepository
    participant DB as 🗄️ Airline_FlightOpsDB

    %% Query by PNR
    alt Search by 6-character PNR
        Client->>Gateway: GET /bookings?pnr=XY789K
        Gateway->>Controller: GET /api/bookings?pnr=XY789K
        Controller->>BookingSvc: GetBookingByPnrAsync("XY789K")
        BookingSvc->>Repo: GetByPNRAsync("XY789K")
        Repo->>DB: SELECT b.*, bp.* FROM Bookings b LEFT JOIN BookingPassengers bp ON b.Id = bp.BookingId WHERE b.PNR = @pnr
        DB-->>Repo: Booking with Passengers
        BookingSvc-->>Controller: BookingDto
        Controller-->>Gateway: 200 OK BookingDto
        Gateway-->>Client: 200 OK
        
    %% Query by UserId (Booking History)
    else Search by UserId
        Client->>Gateway: GET /bookings?userId=42 [Bearer Token]
        Gateway->>Controller: GET /api/bookings?userId=42
        Controller->>BookingSvc: GetBookingHistoryAsync(42)
        BookingSvc->>Repo: GetByUserIdAsync(42)
        Repo->>DB: SELECT * FROM Bookings WHERE UserId = 42 ORDER BY CreatedAt DESC
        DB-->>Repo: List of Bookings
        BookingSvc-->>Controller: List<BookingHistoryDto>
        Controller-->>Gateway: 200 OK List
        Gateway-->>Client: 200 OK Booking History
    end
```

---

### 5.3 Full Booking Cancellation & Dynamic Seat Inventory Release

Cancelling a booking updates the booking status to `Cancelled` and automatically restores the reserved seats to the `FlightSchedule` seat inventory.

```mermaid
sequenceDiagram
    autonumber
    actor Passenger as 👤 Passenger / 🎟️ Dealer
    participant Gateway as 🚪 ApiGateway
    participant Controller as 🎮 BookingsController
    participant BookingSvc as ⚙️ BookingService
    participant ScheduleSvc as ⚙️ FlightScheduleService
    participant BookingRepo as 🗃️ BookingRepository
    participant ScheduleRepo as 🗃️ FlightScheduleRepository
    participant DB as 🗄️ Airline_FlightOpsDB

    Passenger->>Gateway: POST /bookings/{id}/cancel [Bearer Token (Roles: Passenger, Dealer)]
    Gateway->>Controller: Forward to POST /api/bookings/{id}/cancel
    Controller->>BookingSvc: CancelBookingAsync(id)
    
    BookingSvc->>BookingRepo: GetByIdAsync(id)
    BookingRepo->>DB: SELECT * FROM Bookings WHERE Id = @id
    DB-->>BookingRepo: Booking Record
    BookingRepo-->>BookingSvc: Booking Entity
    
    alt Booking Already Cancelled
        BookingSvc-->>Controller: throw BookingCancellationNotAllowedException("Booking is already cancelled")
        Controller-->>Gateway: 400 Bad Request
    else Active Booking
        BookingSvc->>BookingSvc: booking.Status = BookingStatus.Cancelled
        BookingSvc->>BookingRepo: UpdateAsync(booking)
        BookingRepo->>DB: UPDATE Bookings SET Status = 'Cancelled', UpdatedAt = UTC WHERE Id = @id
        DB-->>BookingRepo: Success
        
        %% Release Seats In-Process
        opt Has ScheduleId & TotalPassengers > 0
            BookingSvc->>ScheduleSvc: ReleaseScheduleSeatAsync(ScheduleId, SeatClass, TotalPassengers)
            ScheduleSvc->>ScheduleRepo: IncrementSeatInventory(ScheduleId, SeatClass, count)
            ScheduleRepo->>DB: UPDATE FlightSchedules SET EconomySeats = EconomySeats + @count WHERE Id = @id
            DB-->>ScheduleRepo: Seats Restored
            ScheduleRepo-->>ScheduleSvc: Success
            ScheduleSvc-->>BookingSvc: Success
        end
        
        BookingSvc-->>Controller: Task Completed
        Controller-->>Gateway: 200 OK { message: "Booking cancelled successfully" }
        Gateway-->>Passenger: 200 OK Cancellation Confirmed
    end
```

---

### 5.4 Partial Companion Passenger Cancellation

Allows removing an individual companion passenger from a multi-seat booking without cancelling the remaining passengers.

```mermaid
sequenceDiagram
    autonumber
    actor Passenger as 👤 Passenger / 🎟️ Dealer
    participant Gateway as 🚪 ApiGateway
    participant Controller as 🎮 BookingsController
    participant PassengerSvc as ⚙️ PassengerService (FlightOps)
    participant PassengerRepo as 🗃️ PassengerRepository
    participant BookingRepo as 🗃️ BookingRepository
    participant DB as 🗄️ Airline_FlightOpsDB

    Passenger->>Gateway: POST /bookings/passengers/{passengerId}/cancel (CancelPassengerDto: { reason })
    Gateway->>Controller: Forward to /api/bookings/passengers/{passengerId}/cancel
    Controller->>PassengerSvc: CancelPassengerAsync(passengerId, dto)
    
    PassengerSvc->>PassengerRepo: GetPassengerByIdAsync(passengerId)
    PassengerRepo->>DB: SELECT * FROM BookingPassengers WHERE Id = @passengerId
    DB-->>PassengerRepo: BookingPassenger Entity
    PassengerRepo-->>PassengerSvc: BookingPassenger
    
    PassengerSvc->>PassengerSvc: passenger.Status = Cancelled, CancelledAt = UTC, CancellationReason = reason
    PassengerSvc->>PassengerRepo: UpdatePassengerAsync(passenger)
    PassengerRepo->>DB: UPDATE BookingPassengers SET Status='Cancelled', CancelledAt=UTC, CancellationReason=@reason WHERE Id=@passengerId
    DB-->>PassengerRepo: Success
    
    %% Update Parent Booking Passenger Counts
    PassengerSvc->>BookingRepo: GetByIdAsync(passenger.BookingId)
    BookingRepo->>DB: SELECT * FROM Bookings WHERE Id = @bookingId
    DB-->>BookingRepo: Booking Entity
    BookingRepo-->>PassengerSvc: Booking
    
    PassengerSvc->>PassengerSvc: booking.CancelledPassengers++, booking.ConfirmedPassengers--
    PassengerSvc->>BookingRepo: UpdateAsync(booking)
    BookingRepo->>DB: UPDATE Bookings SET CancelledPassengers=@cp, ConfirmedPassengers=@cf, UpdatedAt=UTC WHERE Id=@id
    DB-->>BookingRepo: Success
    
    PassengerSvc-->>Controller: Done
    Controller-->>Gateway: 200 OK { message: "Passenger cancelled successfully" }
    Gateway-->>Passenger: 200 OK Response
```

---

## 6. Payment Processing & Razorpay Gateway Integration

### 6.1 Razorpay Order Creation Flow

The frontend initiates payment by asking `PaymentService` to create an official Razorpay Order. `PaymentService` calls `FlightOpsService` to validate the booking's authoritative total amount before communicating with Razorpay.

```mermaid
sequenceDiagram
    autonumber
    actor Client as 🌐 Passenger (Angular Frontend)
    participant Gateway as 🚪 ApiGateway
    participant PaymentCtrl as 🎮 PaymentsController
    participant PaymentSvc as ⚙️ PaymentService
    participant FlightOpsCtrl as 🎮 BookingsController (FlightOps)
    participant RazorpayApi as 🛡️ Razorpay API Server

    Client->>Gateway: POST /payments/create-order (CreateOrderDto: { bookingId, amount })
    Gateway->>PaymentCtrl: Forward to POST /api/payments/create-order
    PaymentCtrl->>PaymentSvc: CreateOrderAsync(dto)
    
    %% Cross-Service Booking Validation
    PaymentSvc->>FlightOpsCtrl: GET http://localhost:5002/api/bookings/{bookingId} [Bearer Token]
    FlightOpsCtrl-->>PaymentSvc: 200 OK { totalAmount: 4500.00, status: "Pending" }
    
    PaymentSvc->>PaymentSvc: Convert Amount to Paise (4500 * 100 = 450000)
    PaymentSvc->>PaymentSvc: Build Options { amount: 450000, currency: "INR", receipt: "booking_rcptid_123" }
    
    %% Call Razorpay SDK
    PaymentSvc->>RazorpayApi: Order.Create(options) [KeyId & KeySecret]
    RazorpayApi-->>PaymentSvc: Razorpay Order Object { id: "order_Kj98f2hsk98", amount: 450000, currency: "INR" }
    
    PaymentSvc-->>PaymentCtrl: { orderId: "order_Kj98f2hsk98", key: "rzp_test_xxx", amount: 450000, currency: "INR" }
    PaymentCtrl-->>Gateway: 200 OK Order Response
    Gateway-->>Client: 200 OK (Launches Razorpay Checkout modal in browser)
```

---

### 6.2 Razorpay Signature Verification & Cross-Service Booking Confirmation

After the user completes payment on the Razorpay modal, the frontend sends the checkout signatures for cryptographic verification. Once verified, `PaymentService` notifies `FlightOpsService` via REST to confirm the booking.

```mermaid
sequenceDiagram
    autonumber
    actor Client as 🌐 Passenger (Angular / Razorpay SDK)
    participant Gateway as 🚪 ApiGateway
    participant PaymentCtrl as 🎮 PaymentsController
    participant PaymentSvc as ⚙️ PaymentService
    participant PaymentRepo as 🗃️ PaymentRepository
    participant DB_Pay as 🗄️ Airline_PaymentDB
    participant FlightOpsCtrl as 🎮 BookingsController (FlightOps)
    participant BookingRepo as 🗃️ BookingRepository (FlightOps)
    participant DB_Flight as 🗄️ Airline_FlightOpsDB

    Note over Client: User completes payment on Razorpay modal.<br/>Receives payment_id, order_id, signature.
    
    Client->>Gateway: POST /payments/verify-signature (VerifySignatureDto: { bookingId, amount, razorpayOrderId, razorpayPaymentId, razorpaySignature })
    Gateway->>PaymentCtrl: Forward to POST /api/payments/verify-signature
    PaymentCtrl->>PaymentSvc: VerifySignatureAsync(dto)
    
    %% HMAC-SHA256 Signature Verification
    PaymentSvc->>PaymentSvc: Compute HMAC-SHA256(payload = orderId + "|" + paymentId, secret = KeySecret)
    PaymentSvc->>PaymentSvc: Compare computed signature with dto.RazorpaySignature
    
    alt Signature Mismatch
        PaymentSvc-->>PaymentCtrl: throw InvalidOperationException("Signature mismatch")
        PaymentCtrl-->>Gateway: 400 Bad Request { message: "Invalid RazorPay Signature. Payment Failed." }
        Gateway-->>Client: 400 Bad Request
    else Signature Valid
        %% Persist Payment Record
        PaymentSvc->>PaymentRepo: AddAsync(Payment: { BookingId, Amount, PaymentMethod="RazorPay", TransactionId=paymentId, Status="Success" })
        PaymentRepo->>DB_Pay: INSERT INTO Payments (BookingId, Amount, PaymentMethod, TransactionId, Status, CreatedAt)
        DB_Pay-->>PaymentRepo: Payment Record Saved
        PaymentRepo-->>PaymentSvc: Saved Payment Entity
        
        %% Inter-Service Call to Confirm Booking
        PaymentSvc->>FlightOpsCtrl: POST http://localhost:5002/api/bookings/{bookingId}/confirm-payment?transactionId={paymentId}&paymentMethod=RazorPay [Bearer Token]
        
        FlightOpsCtrl->>BookingRepo: GetByIdAsync(bookingId)
        BookingRepo->>DB_Flight: SELECT * FROM Bookings WHERE Id = @bookingId
        DB_Flight-->>BookingRepo: Booking Record
        BookingRepo-->>FlightOpsCtrl: Booking Entity
        
        FlightOpsCtrl->>BookingRepo: Update Booking (Status = "Confirmed", PaymentStatus = "Success")
        BookingRepo->>DB_Flight: UPDATE Bookings SET Status = 'Confirmed', PaymentStatus = 'Success', UpdatedAt = UTC WHERE Id = @bookingId
        DB_Flight-->>BookingRepo: Success
        FlightOpsCtrl-->>PaymentSvc: 200 OK { message: "Booking payment confirmed successfully", status: "Confirmed" }
        
        PaymentSvc-->>PaymentCtrl: PaymentDto { Id, BookingId, Amount, Status="Success", PaymentMethod="RazorPay" }
        PaymentCtrl-->>Gateway: 200 OK PaymentDto
        Gateway-->>Client: 200 OK Payment Confirmed (Redirect to Booking Success Page)
    end
```

---

### 6.3 Direct Payment Processing Workflow

For direct card/UPI simulation endpoints (`POST /api/payments`):

```mermaid
sequenceDiagram
    autonumber
    actor Client as 🌐 Passenger / 🎟️ Dealer
    participant Gateway as 🚪 ApiGateway
    participant PaymentCtrl as 🎮 PaymentsController
    participant PaymentSvc as ⚙️ PaymentService
    participant PaymentRepo as 🗃️ PaymentRepository
    participant DB_Pay as 🗄️ Airline_PaymentDB
    participant FlightOpsCtrl as 🎮 BookingsController (FlightOps)
    participant DB_Flight as 🗄️ Airline_FlightOpsDB

    Client->>Gateway: POST /payments (ProcessPaymentDto: { bookingId, amount, paymentMethod }) [Bearer Token]
    Gateway->>PaymentCtrl: Forward to POST /api/payments
    PaymentCtrl->>PaymentSvc: ProcessPaymentAsync(dto)
    
    PaymentSvc->>FlightOpsCtrl: GET /api/bookings/{bookingId} (Validate Total)
    FlightOpsCtrl-->>PaymentSvc: 200 OK { totalAmount: 3200.00 }
    
    PaymentSvc->>PaymentSvc: Generate Transaction UUID
    PaymentSvc->>PaymentRepo: AddAsync(Payment)
    PaymentRepo->>DB_Pay: INSERT INTO Payments (BookingId, Amount, PaymentMethod, TransactionId, Status="Success")
    DB_Pay-->>PaymentRepo: Success
    
    PaymentSvc->>FlightOpsCtrl: POST /api/bookings/{bookingId}/confirm-payment
    FlightOpsCtrl->>DB_Flight: UPDATE Bookings SET Status = 'Confirmed', PaymentStatus = 'Success'
    DB_Flight-->>FlightOpsCtrl: Success
    FlightOpsCtrl-->>PaymentSvc: 200 OK
    
    PaymentSvc-->>PaymentCtrl: PaymentDto
    PaymentCtrl-->>Gateway: 200 OK PaymentDto
    Gateway-->>Client: 200 OK Direct Payment Successful
```

---

### 6.4 Payment Refund Processing Flow

Authorized Financial Admins and SuperAdmins issue refunds for previously successful payments.

```mermaid
sequenceDiagram
    autonumber
    actor FinAdmin as 💰 Financial Admin / SuperAdmin
    participant Gateway as 🚪 ApiGateway
    participant Controller as 🎮 PaymentsController
    participant Service as ⚙️ PaymentService
    participant Repo as 🗃️ PaymentRepository
    participant DB as 🗄️ Airline_PaymentDB

    FinAdmin->>Gateway: POST /payments/{id}/refund [Bearer Token (Roles: Admin, SuperAdmin, FinancialAdmin)]
    Gateway->>Controller: Forward to POST /api/payments/{id}/refund
    Controller->>Service: RefundAsync(paymentId)
    
    Service->>Repo: GetByIdAsync(paymentId)
    Repo->>DB: SELECT * FROM Payments WHERE Id = @paymentId
    DB-->>Repo: Payment Record
    Repo-->>Service: Payment Entity
    
    alt Payment Record Not Found
        Service-->>Controller: throw KeyNotFoundException("Payment not found")
        Controller-->>Gateway: 404 Not Found
    else Active Payment
        Service->>Service: payment.Status = PaymentStatus.Refunded
        Service->>Repo: UpdateAsync(payment)
        Repo->>DB: UPDATE Payments SET Status = 'Refunded', UpdatedAt = UTC WHERE Id = @paymentId
        DB-->>Repo: Success
        Service-->>Controller: Updated PaymentDto (Status="Refunded")
        Controller-->>Gateway: 200 OK PaymentDto
        Gateway-->>FinAdmin: 200 OK Refund Processed
    end
```

---

## 7. Digital Online Check-In & Boarding Pass Issuance

### 7.1 5-Hour Window Check-In, Seat Assignment & QR Generation

Check-in enforces an IST departure time rule (opens strictly within **5 hours** before departure), generates or assigns the seat number, creates a dynamic Base64 PNG QR code via `QRCoder`, and issues the digital boarding pass.

```mermaid
sequenceDiagram
    autonumber
    actor Passenger as 👤 Passenger / 🏢 Ground Staff
    participant Gateway as 🚪 ApiGateway
    participant Controller as 🎮 CheckInsController
    participant CheckInSvc as ⚙️ CheckInService
    participant QREngine as 📱 QRCoder Engine
    participant Repo as 🗃️ CheckInRepository
    participant DB as 🗄️ Airline_FlightOpsDB

    Passenger->>Gateway: POST /api/checkins?passengerName=John+Doe&flightNumber=AI-202&flightId=5&departureTime=2026-08-20T15:00:00&fare=4500 (OnlineCheckInDto) [Bearer Token]
    Gateway->>Controller: Forward to POST /api/checkins
    Controller->>CheckInSvc: OnlineCheckInAsync(dto, passengerName, flightNumber, flightId, departureTime, fare, token)
    
    %% Rule: 5-Hour Departure Check
    CheckInSvc->>CheckInSvc: Calculate nowIst = DateTime.UtcNow + 5.5 hours
    CheckInSvc->>CheckInSvc: timeUntilDeparture = departureTime - nowIst
    
    alt Time Until Departure > 5 Hours
        CheckInSvc-->>Controller: throw InvalidOperationException("Check-in opens only 5 hours before departure.")
        Controller-->>Gateway: 400 Bad Request { message: "Check-in opens only 5 hours before departure..." }
        Gateway-->>Passenger: 400 Bad Request
    else Flight Already Departed (timeUntilDeparture < 0)
        CheckInSvc-->>Controller: throw InvalidOperationException("Flight has already departed.")
        Controller-->>Gateway: 400 Bad Request
        Gateway-->>Passenger: 400 Bad Request
    else Check-In Window Valid (0 <= hours <= 5)
        CheckInSvc->>Repo: GetByPassengerIdAsync(dto.PassengerId)
        Repo->>DB: SELECT * FROM CheckIns WHERE PassengerId = @passengerId
        DB-->>Repo: null (Not checked in yet)
        Repo-->>CheckInSvc: null
        
        CheckInSvc->>CheckInSvc: Assign Seat (dto.SeatNumber or Generate e.g. "14B")
        
        %% Generate Dynamic QR Code
        CheckInSvc->>QREngine: GenerateQRCode("AI-202-14B")
        QREngine->>QREngine: Create PngByteQRCode (ECCLevel Q)
        QREngine-->>CheckInSvc: Base64 Encoded PNG QR String
        
        CheckInSvc->>CheckInSvc: Construct CheckIn Entity { BoardingPass: "John Doe|AI-202|14B", QRCode: base64, Gate: "TBD", IsCheckedIn: true }
        
        CheckInSvc->>Repo: AddAsync(checkIn)
        Repo->>DB: INSERT INTO CheckIns (BookingId, PassengerId, UserId, FlightId, SeatNumber, Gate, BoardingPass, QRCode, CheckInTime, IsCheckedIn)
        DB-->>Repo: 1 Row Inserted (Id generated)
        Repo-->>CheckInSvc: Persisted CheckIn Entity
        
        CheckInSvc-->>Controller: CheckInDto
        Controller-->>Gateway: 200 OK CheckInDto
        Gateway-->>Passenger: 200 OK (Displays Boarding Pass with Scannable QR Code)
    end
```

---

### 7.2 Fetching Digital Boarding Passes by Booking ID

```mermaid
sequenceDiagram
    autonumber
    actor Passenger as 👤 Passenger / 🏢 Ground Staff
    participant Gateway as 🚪 ApiGateway
    participant Controller as 🎮 CheckInsController
    participant CheckInSvc as ⚙️ CheckInService
    participant Repo as 🗃️ CheckInRepository
    participant DB as 🗄️ Airline_FlightOpsDB

    Passenger->>Gateway: GET /api/checkins?bookingId=123 [Bearer Token]
    Gateway->>Controller: GET /api/checkins?bookingId=123
    Controller->>CheckInSvc: GetBoardingPassesByBookingAsync(123)
    CheckInSvc->>Repo: GetByBookingIdAsync(123)
    Repo->>DB: SELECT * FROM CheckIns WHERE BookingId = 123
    DB-->>Repo: List<CheckIn> Records
    Repo-->>CheckInSvc: List<CheckIn> Entities
    
    CheckInSvc->>CheckInSvc: Parse BoardingPass strings into BoardingPassDto list (PassengerName, FlightNumber, SeatNumber, Gate, QRCode)
    CheckInSvc-->>Controller: List<BoardingPassDto>
    Controller-->>Gateway: 200 OK [Boarding Passes with QR Codes]
    Gateway-->>Passenger: 200 OK Response
```

---

## 8. Asynchronous Background Processing

### 8.1 Autonomous Flight Schedule Auto-Completion Loop

The `ScheduleCompletionWorker` runs as a hosted background service loop every 60 seconds. It identifies flight schedules whose arrival time has elapsed in IST and marks them as `Completed`.

```mermaid
sequenceDiagram
    autonumber
    participant Host as ⚙️ ASP.NET Core Runtime
    participant Worker as ⏱️ ScheduleCompletionWorker (BackgroundService)
    participant ScopeFactory as 🏭 IServiceScopeFactory
    participant ScheduleSvc as ⚙️ FlightScheduleService
    participant Repo as 🗃️ FlightScheduleRepository
    participant DB as 🗄️ Airline_FlightOpsDB

    Host->>Worker: StartAsync(CancellationToken)
    
    loop Every 60 Seconds (While Not Cancelled)
        Worker->>ScopeFactory: CreateScope()
        ScopeFactory-->>Worker: IServiceScope
        
        Worker->>ScheduleSvc: MarkExpiredSchedulesCompletedAsync()
        ScheduleSvc->>Repo: GetExpiredScheduledSchedulesAsync(nowIst)
        Repo->>DB: SELECT * FROM FlightSchedules WHERE Status = 'Scheduled' AND ArrivalTime <= @nowIst
        DB-->>Repo: Expired FlightSchedule Entities
        Repo-->>ScheduleSvc: List<FlightSchedule>
        
        loop For Each Expired Schedule
            ScheduleSvc->>ScheduleSvc: schedule.Status = "Completed"
            ScheduleSvc->>Repo: UpdateAsync(schedule)
            Repo->>DB: UPDATE FlightSchedules SET Status = 'Completed', UpdatedAt = UTC WHERE Id = @schedule.Id
            DB-->>Repo: Success
        end
        
        ScheduleSvc-->>Worker: Completed
        Worker->>Worker: Task.Delay(60000, stoppingToken)
    end
```

---

## 9. Cross-Service Operational & Financial Reporting

### 9.1 Backoffice Booking & Financial Report Aggregation Flow

The Backoffice Service aggregates financial performance, booking velocity, and occupancy metrics across dates by invoking the `FlightOpsService` API securely over internal HTTP.

```mermaid
sequenceDiagram
    autonumber
    actor Admin as 💰 Financial Admin / 👑 SuperAdmin
    participant Gateway as 🚪 ApiGateway
    participant BackofficeCtrl as 🎮 BackofficeController
    participant BackofficeSvc as ⚙️ BackofficeService
    participant Http as 🌐 HttpClient
    participant FlightOpsCtrl as 🎮 BookingsController (FlightOps)
    participant BookingRepo as 🗃️ BookingRepository (FlightOps)
    participant DB_Flight as 🗄️ Airline_FlightOpsDB

    Admin->>Gateway: GET /api/backoffice/booking-report?startDate=2026-08-01&endDate=2026-08-31 [Bearer Token (Roles: SuperAdmin, Admin, HR, FinancialAdmin)]
    Gateway->>BackofficeCtrl: Forward to /api/backoffice/booking-report
    BackofficeCtrl->>BackofficeSvc: GetBookingReportAsync(startDate, endDate)
    
    BackofficeSvc->>Http: GET http://localhost:5002/api/bookings
    Http->>FlightOpsCtrl: GET /api/bookings
    FlightOpsCtrl->>BookingRepo: GetAllAsync()
    BookingRepo->>DB_Flight: SELECT * FROM Bookings
    DB_Flight-->>BookingRepo: All Bookings List
    BookingRepo-->>FlightOpsCtrl: List<Booking>
    FlightOpsCtrl-->>Http: 200 OK [JSON Array of Bookings]
    Http-->>BackofficeSvc: 200 OK Response
    
    BackofficeSvc->>BackofficeSvc: Filter Bookings (CreatedAt >= startDate AND CreatedAt <= endDate)
    BackofficeSvc->>BackofficeSvc: Map to List<BookingReportDto> (Id, PNR, UserEmail, Amount, SeatClass, Status, PaymentStatus, CreatedAt)
    
    BackofficeSvc-->>BackofficeCtrl: IEnumerable<BookingReportDto>
    BackofficeCtrl-->>Gateway: 200 OK Filtered Report Data
    Gateway-->>Admin: 200 OK Financial & Operational Report
```

---

## Summary of Sequence Interaction Coverage

| Feature / Workflow | Primary Initiator | Services & External Components Involved | Outcome / Result |
| :--- | :--- | :--- | :--- |
| **Passenger Registration & OTP** | Public Passenger | `PassengerService`, SQL Server | Profile created (`IsEmailVerified=false`), 6-digit OTP sent via email. |
| **Email OTP Verification** | Passenger | `PassengerService`, `JwtTokenGenerator` | Account activated, signed JWT Bearer token issued. |
| **Backoffice Staff Provisioning** | SuperAdmin / HR | `BackOfficeService`, `PasswordHasher` | Role-based internal staff profile provisioned. |
| **Flight Template & Schedule Creation** | Admin / SuperAdmin | `FlightOpsService`, `Airline_FlightOpsDB` | Catalog template & schedule instances with inventory generated. |
| **Booking Creation & Seat Deduction** | Passenger / Dealer | `FlightOpsService` (In-Process) | Seats deducted, 6-char PNR created, booking set to `Pending`. |
| **Razorpay Order Creation** | Passenger (Frontend) | `PaymentService`, `FlightOpsService`, Razorpay API | Authoritative total validated, order created in paise. |
| **Razorpay Payment Verification** | Passenger (Frontend) | `PaymentService`, `FlightOpsService`, `Razorpay` | HMAC-SHA256 signature verified, booking transitioned to `Confirmed` & `Paid`. |
| **Digital Online Check-In** | Passenger / Ground Staff | `FlightOpsService`, `QRCoder` | 5-hour window verified, seat assigned, Base64 PNG QR boarding pass issued. |
| **Schedule Auto-Completion Worker** | Background Hosted Service | `FlightOpsService`, SQL Server | Departed flight schedules automatically transitioned to `Completed`. |
| **Cross-Service Reporting** | FinancialAdmin / SuperAdmin | `BackOfficeService` $\rightarrow$ `FlightOpsService` | Booking and revenue data aggregated and filtered across date ranges. |

# Airline Management System - Class Diagrams Reference

This document provides complete Class Diagrams (using Mermaid) detailing the Object-Oriented structure, layered architecture, interface contracts, entity relationships, repositories, services, controllers, and Data Transfer Objects (DTOs) for all microservices in the solution.

---

## Table of Contents
1. [Layered Architecture Pattern](#1-layered-architecture-pattern)
2. [Shared Common & Core Infrastructure Classes](#2-shared-common--core-infrastructure-classes)
3. [BackOfficeService Class Diagram](#3-backofficeservice-class-diagram)
4. [PassengerService Class Diagram](#4-passengerservice-class-diagram)
5. [FlightOpsService Class Diagram](#5-flightopsservice-class-diagram)
6. [PaymentService Class Diagram](#6-paymentservice-class-diagram)
7. [Comprehensive Domain Models & Inheritance Map](#7-comprehensive-domain-models--inheritance-map)

---

## 1. Layered Architecture Pattern

All microservices follow a clean 4-tier architectural design:
- **Presentation Layer (Controllers)**: Receives HTTP requests, executes authorization checks, model state validations, and dispatches to services.
- **Business Logic Layer (Services)**: Implements business logic, transaction boundaries, validations, token generation, and DTO mapping.
- **Data Access Layer (Repositories)**: Encapsulates EF Core DbContext interactions and SQL queries.
- **Data Model Layer (Entities & DbContext)**: Represents database tables and schemas inheriting from `BaseEntity`.

```mermaid
classDiagram
    direction TB

    class Controller {
        <<ControllerBase>>
        +HandleRequest()
    }

    class IService {
        <<interface>>
        +ExecuteBusinessLogic()
    }

    class ServiceImpl {
        +ExecuteBusinessLogic()
    }

    class IRepository {
        <<interface>>
        +GetByIdAsync(id)
        +AddAsync(entity)
        +UpdateAsync(entity)
    }

    class RepositoryImpl {
        +GetByIdAsync(id)
        +AddAsync(entity)
        +UpdateAsync(entity)
    }

    class DbContext {
        <<EF Core>>
        +DbSet~Entity~ Entities
        +SaveChangesAsync()
    }

    class BaseEntity {
        +int Id
        +DateTime CreatedAt
        +DateTime UpdatedAt
    }

    Controller ..> IService : depends on
    IService <|.. ServiceImpl : implements
    ServiceImpl ..> IRepository : depends on
    IRepository <|.. RepositoryImpl : implements
    RepositoryImpl ..> DbContext : queries
    DbContext ..> BaseEntity : persists
```

---

## 2. Shared Common & Core Infrastructure Classes

The `Shared` library provides reusable base entities, security helpers, JWT token generation, and domain enums used across all services.

```mermaid
classDiagram
    direction TB

    class BaseEntity_TId_~TId~ {
        +TId Id
        +DateTime CreatedAt
        +DateTime? UpdatedAt
    }

    class BaseEntity {
        <<Base Entity int>>
    }

    BaseEntity_TId_ <|-- BaseEntity : inherits with int PK

    class PasswordHasher {
        <<static>>
        +Hash(password: string) string
        +Verify(password: string, passwordHash: string) bool
    }

    class ITokenService {
        <<interface>>
        +GenerateToken(userId: int, email: string, role: string) string
    }

    class JwtTokenService {
        -string _key
        -string _issuer
        -string _audience
        -int _expirationMinutes
        +JwtTokenService(key, issuer, audience, expirationMinutes)
        +GenerateToken(userId: int, email: string, role: string) string
    }

    ITokenService <|.. JwtTokenService : implements

    class JwtSettings {
        +string Key
        +string Issuer
        +string Audience
        +int ExpirationMinutes
    }

    class UserRole {
        <<enumeration>>
        SuperAdmin
        Admin
        HR
        FinancialAdmin
        Staff
        Dealer
        Passenger
    }

    class BookingStatus {
        <<enumeration>>
        Pending
        Confirmed
        CheckedIn
        Completed
        Cancelled
        PartiallyCancelled
        PaymentFailed
    }

    class PaymentStatus {
        <<enumeration>>
        Pending
        Success
        Failed
        Refunded
    }

    class FlightStatus {
        <<enumeration>>
        Scheduled
        Boarding
        Departed
        InFlight
        Landed
        Delayed
        Cancelled
        Completed
    }

    class SeatClass {
        <<enumeration>>
        Economy
        Business
        First
    }
```

---

## 3. BackOfficeService Class Diagram

Handles authentication, staff account provisioning, role management, airport assignment, and cross-service booking aggregation.

```mermaid
classDiagram
    direction TB

    %% Controllers
    class BackofficeAuthController {
        -IBackofficeAuthService _authService
        +BackofficeAuthController(authService: IBackofficeAuthService)
        +Login(dto: BackofficeLoginDto) Task~IActionResult~
        +ForgotPassword(dto: BackofficeForgotPasswordDto) Task~IActionResult~
        +ResetPassword(dto: BackofficeResetPasswordDto) Task~IActionResult~
    }

    class BackofficeController {
        -IBackofficeService _backofficeService
        -IBackofficeAuthService _authService
        +BackofficeController(backofficeService, authService)
        +GetBookingReport(startDate: DateTime, endDate: DateTime) Task~IActionResult~
        +ProvisionUser(dto: BackofficeRegisterDto) Task~IActionResult~
        +GetUsers(roles: string?) Task~IActionResult~
        +UpdateProfile(userId: int, dto: BackofficeUpdateProfileDto) Task~IActionResult~
        +UpdateUserStatus(userId: int, dto: BackofficeUpdateStatusDto) Task~IActionResult~
    }

    %% Service Interfaces & Implementations
    class IBackofficeAuthService {
        <<interface>>
        +RegisterAsync(dto: BackofficeRegisterDto) Task
        +LoginAsync(dto: BackofficeLoginDto) Task~BackofficeAuthResponseDto~
        +ForgotPasswordAsync(email: string) Task~string~
        +ResetPasswordAsync(dto: BackofficeResetPasswordDto) Task
        +UpdateProfileAsync(id: int, dto: BackofficeUpdateProfileDto) Task~BackofficeProfile~
        +GetAllUsersAsync(roles: string[]?) Task~List~BackofficeProfile~~
        +UpdateUserStatusAsync(id: int, isActive: bool) Task
    }

    class BackofficeAuthService {
        -IBackofficeProfileRepository _repo
        -ITokenService _tokenService
        +BackofficeAuthService(repo, tokenService)
        +RegisterAsync(dto) Task
        +LoginAsync(dto) Task~BackofficeAuthResponseDto~
        +ForgotPasswordAsync(email) Task~string~
        +ResetPasswordAsync(dto) Task
        +UpdateProfileAsync(id, dto) Task~BackofficeProfile~
        +GetAllUsersAsync(roles) Task~List~BackofficeProfile~~
        +UpdateUserStatusAsync(id, isActive) Task
    }

    class IBackofficeService {
        <<interface>>
        +GetBookingReportAsync(startDate: DateTime, endDate: DateTime) Task~IEnumerable~BookingReportDto~~
    }

    class BackofficeServiceImpl {
        -HttpClient _httpClient
        -IConfiguration _configuration
        +BackofficeServiceImpl(httpClient, configuration)
        +GetBookingReportAsync(startDate, endDate) Task~IEnumerable~BookingReportDto~~
    }

    IBackofficeAuthService <|.. BackofficeAuthService : implements
    IBackofficeService <|.. BackofficeServiceImpl : implements
    BackofficeAuthController ..> IBackofficeAuthService : uses
    BackofficeController ..> IBackofficeService : uses
    BackofficeController ..> IBackofficeAuthService : uses

    %% Repository Layer
    class IBackofficeProfileRepository {
        <<interface>>
        +GetByIdAsync(id: int) Task~BackofficeProfile?~
        +GetByEmailAsync(email: string) Task~BackofficeProfile?~
        +GetAllAsync(roles: string[]?) Task~List~BackofficeProfile~~
        +AddAsync(profile: BackofficeProfile) Task
        +UpdateAsync(profile: BackofficeProfile) Task
        +DeleteAsync(id: int) Task
    }

    class BackofficeProfileRepository {
        -BackOfficeDbContext _db
        +BackofficeProfileRepository(db: BackOfficeDbContext)
        +GetByIdAsync(id) Task~BackofficeProfile?~
        +GetByEmailAsync(email) Task~BackofficeProfile?~
        +GetAllAsync(roles) Task~List~BackofficeProfile~~
        +AddAsync(profile) Task
        +UpdateAsync(profile) Task
        +DeleteAsync(id) Task
    }

    IBackofficeProfileRepository <|.. BackofficeProfileRepository : implements
    BackofficeAuthService ..> IBackofficeProfileRepository : uses

    %% Model & Data Layer
    class BackOfficeDbContext {
        +DbSet~BackofficeProfile~ BackofficeProfiles
        #OnModelCreating(modelBuilder: ModelBuilder) void
    }

    class BackofficeProfile {
        +string Email
        +string PasswordHash
        +string Name
        +string Role
        +bool IsActive
        +string? ResetToken
        +DateTime? ResetTokenExpiry
        +string Department
        +string RoleTitle
        +string AssignedAirportCode
        +string? PhoneNumber
    }

    BackofficeProfileRepository ..> BackOfficeDbContext : uses
    BackOfficeDbContext ..> BackofficeProfile : maps

    %% DTOs
    class BackofficeRegisterDto {
        +string Name
        +string Email
        +string Password
        +string? Department
        +string? RoleTitle
        +string? AssignedAirportCode
        +string? Role
    }

    class BackofficeLoginDto {
        +string Email
        +string Password
    }

    class BackofficeAuthResponseDto {
        +int UserId
        +string Email
        +string Name
        +string Role
        +string Token
    }

    class BackofficeUpdateProfileDto {
        +string Name
        +string Email
        +string? Department
        +string? RoleTitle
        +string? AssignedAirportCode
        +string? PhoneNumber
    }

    class BackofficeUpdateStatusDto {
        +bool IsActive
    }

    class BookingReportDto {
        +int BookingId
        +int UserId
        +int FlightId
        +string Status
        +DateTime CreatedAt
    }
```

---

## 4. PassengerService Class Diagram

Handles passenger authentication, email verification via OTP, profile updates, travel preferences, and companion passenger records.

```mermaid
classDiagram
    direction TB

    %% Controllers
    class PassengerAuthController {
        -IPassengerAuthService _authService
        +PassengerAuthController(authService)
        +Login(dto: PassengerLoginDto) Task~IActionResult~
        +Register(dto: PassengerRegisterDto) Task~IActionResult~
        +Verify(dto: PassengerVerifyDto) Task~IActionResult~
        +ResendVerification(dto: PassengerForgotPasswordDto) Task~IActionResult~
        +ForgotPassword(dto: PassengerForgotPasswordDto) Task~IActionResult~
        +ResetPassword(dto: PassengerResetPasswordDto) Task~IActionResult~
    }

    class PassengersController {
        -IPassengerAuthService _authService
        +PassengersController(authService)
        +GetUsers() Task~IActionResult~
        +GetUser(userId: int) Task~IActionResult~
        +UpdateProfile(userId: int, dto: PassengerUpdateProfileDto) Task~IActionResult~
        +UpdateUserStatus(userId: int, dto: PassengerUpdateStatusDto) Task~IActionResult~
    }

    %% Service Layer
    class IPassengerAuthService {
        <<interface>>
        +RegisterAsync(dto: PassengerRegisterDto) Task
        +VerifyAsync(dto: PassengerVerifyDto) Task~PassengerAuthResponseDto~
        +ResendVerificationAsync(email: string) Task
        +LoginAsync(dto: PassengerLoginDto) Task~PassengerAuthResponseDto~
        +ForgotPasswordAsync(email: string) Task~string?~
        +ResetPasswordAsync(dto: PassengerResetPasswordDto) Task
        +GetUserAsync(id: int) Task~PassengerProfileResponseDto?~
        +UpdateProfileAsync(id: int, dto: PassengerUpdateProfileDto) Task~PassengerProfileResponseDto~
        +GetAllPassengersAsync() Task~List~PassengerProfileResponseDto~~
        +UpdateUserStatusAsync(id: int, isActive: bool) Task
    }

    class PassengerAuthService {
        -IPassengerProfileRepository _repo
        -ITokenService _tokenService
        -IConfiguration _config
        +PassengerAuthService(repo, tokenService, config)
        +RegisterAsync(dto) Task
        +VerifyAsync(dto) Task~PassengerAuthResponseDto~
        +ResendVerificationAsync(email) Task
        +LoginAsync(dto) Task~PassengerAuthResponseDto~
        +ForgotPasswordAsync(email) Task~string?~
        +ResetPasswordAsync(dto) Task
        +GetUserAsync(id) Task~PassengerProfileResponseDto?~
        +UpdateProfileAsync(id, dto) Task~PassengerProfileResponseDto~
        +GetAllPassengersAsync() Task~List~PassengerProfileResponseDto~~
        +UpdateUserStatusAsync(id, isActive) Task
        -SendOtpEmailAsync(toEmail, name, otp) Task
        -MapToResponseDto(p: PassengerProfile) PassengerProfileResponseDto
    }

    IPassengerAuthService <|.. PassengerAuthService : implements
    PassengerAuthController ..> IPassengerAuthService : uses
    PassengersController ..> IPassengerAuthService : uses

    %% Repository Layer
    class IPassengerProfileRepository {
        <<interface>>
        +GetByIdAsync(id: int) Task~PassengerProfile?~
        +GetByEmailAsync(email: string) Task~PassengerProfile?~
        +GetAllAsync() Task~List~PassengerProfile~~
        +AddAsync(profile: PassengerProfile) Task
        +UpdateAsync(profile: PassengerProfile) Task
    }

    class PassengerProfileRepository {
        -PassengerDbContext _context
        +PassengerProfileRepository(context)
        +GetByIdAsync(id) Task~PassengerProfile?~
        +GetByEmailAsync(email) Task~PassengerProfile?~
        +GetAllAsync() Task~List~PassengerProfile~~
        +AddAsync(profile) Task
        +UpdateAsync(profile) Task
    }

    IPassengerProfileRepository <|.. PassengerProfileRepository : implements
    PassengerAuthService ..> IPassengerProfileRepository : uses

    %% Models
    class PassengerDbContext {
        +DbSet~PassengerProfile~ PassengerProfiles
        +DbSet~SavedPassenger~ SavedPassengers
        #OnModelCreating(modelBuilder: ModelBuilder) void
    }

    class PassengerProfile {
        +string Email
        +string PasswordHash
        +string Name
        +string Role
        +bool IsEmailVerified
        +string? VerificationToken
        +DateTime? VerificationTokenExpiry
        +bool IsActive
        +string? ResetToken
        +DateTime? ResetTokenExpiry
        +string FirstName
        +string LastName
        +string PassportNumber
        +string Nationality
        +DateTime? DateOfBirth
        +string Phone
        +string Aadhar
        +string Gender
        +string DietaryRequirements
        +string MedicalNeeds
        +string MedicalAlerts
        +byte[]? ProfileImage
        +List~SavedPassenger~ SavedPassengers
    }

    class SavedPassenger {
        +int PassengerProfileId
        +string Name
        +int Age
        +string Gender
        +string Aadhar
        +string PassportNumber
        +string Nationality
        +string DietaryRequirements
        +string MedicalNeeds
        +string MedicalAlerts
        +PassengerProfile? Profile
    }

    PassengerProfileRepository ..> PassengerDbContext : uses
    PassengerDbContext ..> PassengerProfile : maps
    PassengerDbContext ..> SavedPassenger : maps
    PassengerProfile "1" *-- "many" SavedPassenger : contains

    %% DTOs
    class PassengerRegisterDto {
        +string Name
        +string Email
        +string Password
        +string? Phone
        +string? DateOfBirth
        +string? Aadhar
    }

    class PassengerLoginDto {
        +string Email
        +string Password
    }

    class PassengerVerifyDto {
        +string Email
        +string Token
    }

    class PassengerProfileResponseDto {
        +int Id
        +string Name
        +string FirstName
        +string LastName
        +string Email
        +string? Phone
        +string? DateOfBirth
        +string? Aadhar
        +string? Gender
        +string? PassportNumber
        +string? Nationality
        +bool IsActive
        +string Role
        +byte[]? ProfileImage
        +DateTime CreatedAt
        +TravelPreferencesDto TravelPreferences
        +List~SavedPassengerDto~ SavedPassengers
    }

    class TravelPreferencesDto {
        +string MealType
        +string MedicalNeeds
        +string MedicalAlerts
    }

    class SavedPassengerDto {
        +string Name
        +int Age
        +string Gender
        +string Aadhar
        +string PassportNumber
        +string Nationality
        +string DietaryRequirements
        +string MedicalNeeds
        +string MedicalAlerts
    }
```

---

## 5. FlightOpsService Class Diagram

Core operational service managing flights, scheduled instances, bookings, manifests, check-ins, and background completion.

```mermaid
classDiagram
    direction TB

    %% Controllers
    class FlightsController {
        -IFlightService _flightService
        -IFlightScheduleService _scheduleService
        +FlightsController(flightService, scheduleService)
        +GetAllFlights(source, destination, departureDate) Task~IActionResult~
        +GetFlight(id: int) Task~IActionResult~
        +CreateFlight(dto: CreateFlightDto) Task~IActionResult~
        +UpdateFlight(id: int, dto: UpdateFlightDto) Task~IActionResult~
        +DeleteFlight(id: int) Task~IActionResult~
        +GetAllSchedules(source, destination, departureDate, flightId) Task~IActionResult~
        +CreateSchedule(dto: CreateScheduleDto) Task~IActionResult~
    }

    class BookingsController {
        -IBookingService _bookingService
        -IPassengerService _passengerService
        -ILogger~BookingsController~ _logger
        +BookingsController(bookingService, passengerService, logger)
        +GetBookings(pnr, userId, flightId, scheduleId) Task~IActionResult~
        +GetBooking(id: int) Task~IActionResult~
        +CreateBooking(dto: CreateBookingDto) Task~IActionResult~
        +CancelBooking(id: int) Task~IActionResult~
        +CancelPassenger(passengerId: int, dto: CancelPassengerDto) Task~IActionResult~
        +GetOccupiedSeats(flightId: int, scheduleId: int?) Task~IActionResult~
    }

    class CheckInsController {
        -ICheckInService _checkInService
        +CheckInsController(checkInService)
        +GetAll(bookingId: int?) Task~IActionResult~
        +GetCheckIn(id: int) Task~IActionResult~
        +CheckIn(dto: OnlineCheckInDto, ...) Task~IActionResult~
    }

    %% Service Layer
    class IFlightService {
        <<interface>>
        +CreateFlightAsync(dto: CreateFlightDto) Task~FlightDto~
        +GetFlightAsync(id: int) Task~FlightDto~
        +UpdateFlightAsync(id: int, dto: UpdateFlightDto) Task~FlightDto~
        +DeleteFlightAsync(id: int) Task
        +SearchFlightsAsync(source, destination, departureDate) Task~IEnumerable~FlightDto~~
        +GetAllFlightsAsync() Task~IEnumerable~FlightDto~~
    }

    class IFlightScheduleService {
        <<interface>>
        +CreateScheduleAsync(dto: CreateScheduleDto) Task~FlightScheduleDto~
        +GetScheduleAsync(id: int) Task~FlightScheduleDto~
        +DeleteScheduleAsync(id: int) Task
        +CancelScheduleAsync(id: int) Task
        +SearchSchedulesAsync(source, destination, departureDate, flightId) Task~IEnumerable~FlightScheduleDto~~
        +GetAllSchedulesAsync() Task~IEnumerable~FlightScheduleDto~~
        +BookScheduleSeatAsync(scheduleId, seatClass, count) Task
        +ReleaseScheduleSeatAsync(scheduleId, seatClass, count) Task
        +MarkExpiredSchedulesCompletedAsync() Task
    }

    class IBookingService {
        <<interface>>
        +CreateBookingAsync(dto: CreateBookingDto) Task~BookingDto~
        +GetBookingAsync(id: int) Task~BookingDto~
        +CancelBookingAsync(id: int) Task
        +GetBookingHistoryAsync(userId: int) Task~IEnumerable~BookingHistoryDto~~
        +GetBookingsByScheduleAsync(scheduleId: int) Task~IEnumerable~object~~
        +GetOccupiedSeatsAsync(flightId: int, scheduleId: int?) Task~IEnumerable~string~~
        +GetBookingByPnrAsync(pnr: string) Task~BookingDto~
        +GetAllBookingsAsync() Task~IEnumerable~object~~
        +GetBookingsByFlightIdAsync(flightId: int) Task~IEnumerable~object~~
        +ConfirmPaymentAsync(bookingId: int, transactionId, paymentMethod) Task
    }

    class ICheckInService {
        <<interface>>
        +OnlineCheckInAsync(dto: OnlineCheckInDto, ...) Task~CheckInDto~
        +GetCheckInAsync(id: int) Task~CheckInDto~
        +GetBoardingPassesByBookingAsync(bookingId: int) Task~IEnumerable~BoardingPassDto~~
        +GetAllCheckInsAsync() Task~IEnumerable~CheckInDto~~
    }

    class IPassengerService {
        <<interface>>
        +GetPassengerAsync(passengerId: int) Task~PassengerResponseDto?~
        +CancelPassengerAsync(passengerId: int, dto: CancelPassengerDto) Task
        +ValidateAadharNumberAsync(aadharCardNo: string, excludeId: int?) Task~bool~
    }

    %% Service Implementations
    class FlightServiceImpl {
        -IFlightRepository _repository
    }
    class FlightScheduleService {
        -IFlightRepository _repository
    }
    class BookingServiceImpl {
        -IBookingRepository _repository
        -IFlightService _flightService
        -IFlightScheduleService _scheduleService
    }
    class CheckInServiceImpl {
        -ICheckInRepository _repository
    }
    class PassengerService {
        -IPassengerRepository _passengerRepository
        -IBookingRepository _bookingRepository
    }

    IFlightService <|.. FlightServiceImpl : implements
    IFlightScheduleService <|.. FlightScheduleService : implements
    IBookingService <|.. BookingServiceImpl : implements
    ICheckInService <|.. CheckInServiceImpl : implements
    IPassengerService <|.. PassengerService : implements

    FlightsController ..> IFlightService : uses
    FlightsController ..> IFlightScheduleService : uses
    BookingsController ..> IBookingService : uses
    BookingsController ..> IPassengerService : uses
    CheckInsController ..> ICheckInService : uses

    %% Background Worker
    class ScheduleCompletionWorker {
        <<BackgroundService>>
        -IServiceScopeFactory _scopeFactory
        #ExecuteAsync(stoppingToken: CancellationToken) Task
    }
    ScheduleCompletionWorker ..> IFlightScheduleService : triggers

    %% Repositories
    class IFlightRepository {
        <<interface>>
        +GetByIdAsync(id: int) Task~Flight?~
        +GetByFlightNumberAsync(flightNumber: string) Task~Flight?~
        +AddAsync(flight: Flight) Task~Flight~
        +UpdateAsync(flight: Flight) Task
        +DeleteAsync(id: int) Task
        +GetScheduleByIdAsync(id: int) Task~FlightSchedule?~
        +AddScheduleAsync(schedule: FlightSchedule) Task~FlightSchedule~
        +UpdateScheduleAsync(schedule: FlightSchedule) Task
        +DeleteScheduleAsync(id: int) Task
        +SearchSchedulesAsync(source, destination, date, flightId) Task~IEnumerable~FlightSchedule~~
        +GetAllSchedulesAsync() Task~IEnumerable~FlightSchedule~~
    }

    class IBookingRepository {
        <<interface>>
        +GetByIdAsync(id: int) Task~Booking?~
        +GetByPNRAsync(pnr: string) Task~Booking?~
        +GetByUserIdAsync(userId: int) Task~IEnumerable~Booking~~
        +GetByScheduleIdAsync(scheduleId: int) Task~IEnumerable~Booking~~
        +AddAsync(booking: Booking) Task~Booking~
        +UpdateAsync(booking: Booking) Task
        +GetOccupiedSeatsAsync(flightId: int, scheduleId: int?) Task~IEnumerable~string~~
    }

    class ICheckInRepository {
        <<interface>>
        +GetByIdAsync(id: int) Task~CheckIn?~
        +GetByBookingIdAsync(bookingId: int) Task~IEnumerable~CheckIn~~
        +GetByPassengerIdAsync(passengerId: int) Task~CheckIn?~
        +AddAsync(checkIn: CheckIn) Task~CheckIn~
        +GetAllAsync() Task~IEnumerable~CheckIn~~
    }

    class IPassengerRepository {
        <<interface>>
        +GetPassengerByIdAsync(passengerId: int) Task~BookingPassenger?~
        +UpdatePassengerAsync(passenger: BookingPassenger) Task
        +IsAadharUniqueAsync(aadhar: string, excludeId: int?) Task~bool~
    }

    %% Domain Entities
    class Flight {
        +string FlightNumber
        +string Source
        +string Destination
        +DateTime DepartureTime
        +DateTime ArrivalTime
        +string Gate
        +string Aircraft
        +FlightStatus Status
        +int TotalSeats
        +int AvailableSeats
        +int EconomySeats
        +int BusinessSeats
        +int FirstSeats
        +string CrewAssignment
        +decimal EconomyPrice
        +decimal BusinessPrice
        +decimal FirstClassPrice
    }

    class FlightSchedule {
        +int FlightId
        +Flight? Flight
        +DateTime DepartureTime
        +DateTime ArrivalTime
        +string Gate
        +FlightStatus Status
        +int TotalSeats
        +int AvailableSeats
        +int EconomySeats
        +int BusinessSeats
        +int FirstSeats
        +decimal EconomyPrice
        +decimal BusinessPrice
        +decimal FirstClassPrice
    }

    class Booking {
        +int UserId
        +string UserEmail
        +string UserName
        +int FlightId
        +Flight? Flight
        +int? ScheduleId
        +FlightSchedule? Schedule
        +SeatClass SeatClass
        +decimal BaggageWeight
        +string PNR
        +BookingStatus Status
        +PaymentStatus PaymentStatus
        +int TotalPassengers
        +int ConfirmedPassengers
        +int CancelledPassengers
        +decimal TotalAmount
        +ICollection~BookingPassenger~ Passengers
    }

    class BookingPassenger {
        +int BookingId
        +string Name
        +int Age
        +string Gender
        +string AadharCardNo
        +string PassportNumber
        +string Nationality
        +string DietaryRequirements
        +string MedicalNeeds
        +string MedicalAlerts
        +BookingPassengerStatus Status
        +decimal Fare
        +DateTime? CancelledAt
        +string? CancellationReason
        +string? SeatNumber
        +Booking? Booking
    }

    class CheckIn {
        +int BookingId
        +Booking? Booking
        +int PassengerId
        +BookingPassenger? Passenger
        +int UserId
        +int FlightId
        +Flight? Flight
        +string SeatNumber
        +string Gate
        +string BoardingPass
        +string QRCode
        +DateTime CheckInTime
        +bool IsCheckedIn
    }

    Flight "1" *-- "many" FlightSchedule : has
    Flight "1" *-- "many" Booking : booked
    FlightSchedule "1" *-- "many" Booking : scheduled
    Booking "1" *-- "many" BookingPassenger : manifest
    Booking "1" *-- "many" CheckIn : checked in
    BookingPassenger "1" *-- "0..1" CheckIn : issued
```

---

## 6. PaymentService Class Diagram

Encapsulates payment processing, Razorpay order generation, signature verification, refunds, and cross-service booking updates.

```mermaid
classDiagram
    direction TB

    %% Controller
    class PaymentsController {
        -IPaymentService _paymentService
        +PaymentsController(paymentService: IPaymentService)
        +GetPayment(id: int) Task~IActionResult~
        +ProcessPayment(dto: ProcessPaymentDto) Task~IActionResult~
        +Refund(id: int) Task~IActionResult~
    }

    %% Service Layer
    class IPaymentService {
        <<interface>>
        +ProcessPaymentAsync(dto: ProcessPaymentDto) Task~PaymentDto~
        +CreateOrderAsync(dto: CreateOrderDto) Task~object~
        +VerifySignatureAsync(dto: VerifySignatureDto) Task~PaymentDto~
        +GetPaymentAsync(id: int) Task~PaymentDto~
        +RefundAsync(paymentId: int) Task~PaymentDto~
        +ReportFailureAsync(dto: ReportFailureDto) Task
    }

    class PaymentServiceImpl {
        -IPaymentRepository _repository
        -HttpClient _httpClient
        -IConfiguration _configuration
        -IHttpContextAccessor _httpContextAccessor
        -ILogger~PaymentServiceImpl~ _logger
        +PaymentServiceImpl(repository, httpClient, configuration, httpContextAccessor, logger)
        +ProcessPaymentAsync(dto) Task~PaymentDto~
        +CreateOrderAsync(dto) Task~object~
        +VerifySignatureAsync(dto) Task~PaymentDto~
        +GetPaymentAsync(id) Task~PaymentDto~
        +RefundAsync(paymentId) Task~PaymentDto~
        +ReportFailureAsync(dto) Task
        -ValidateBookingAsync(bookingId: int) Task~decimal~
        -NotifyBookingPaymentSuccessAsync(bookingId, transactionId, paymentMethod) Task
    }

    IPaymentService <|.. PaymentServiceImpl : implements
    PaymentsController ..> IPaymentService : uses

    %% Repository Layer
    class IPaymentRepository {
        <<interface>>
        +GetByIdAsync(id: int) Task~Payment?~
        +GetByBookingIdAsync(bookingId: int) Task~Payment?~
        +AddAsync(payment: Payment) Task~Payment~
        +UpdateAsync(payment: Payment) Task
        +GetAllAsync() Task~IEnumerable~Payment~~
    }

    class PaymentRepository {
        -PaymentDbContext _context
        +PaymentRepository(context: PaymentDbContext)
        +GetByIdAsync(id) Task~Payment?~
        +GetByBookingIdAsync(bookingId) Task~Payment?~
        +AddAsync(payment) Task~Payment~
        +UpdateAsync(payment) Task
        +GetAllAsync() Task~IEnumerable~Payment~~
    }

    IPaymentRepository <|.. PaymentRepository : implements
    PaymentServiceImpl ..> IPaymentRepository : uses

    %% Model & Data Layer
    class PaymentDbContext {
        +DbSet~Payment~ Payments
    }

    class Payment {
        +int BookingId
        +decimal Amount
        +PaymentStatus Status
        +string PaymentMethod
        +string TransactionId
    }

    PaymentRepository ..> PaymentDbContext : uses
    PaymentDbContext ..> Payment : maps

    %% DTOs
    class ProcessPaymentDto {
        +int BookingId
        +string PaymentMethod
        +decimal? Amount
        +int? UserId
        +string? UserEmail
        +string? UserName
    }

    class PaymentDto {
        +int Id
        +int BookingId
        +decimal Amount
        +string Status
        +string PaymentMethod
        +DateTime CreatedAt
    }

    class CreateOrderDto {
        +int BookingId
        +decimal Amount
    }

    class VerifySignatureDto {
        +int BookingId
        +decimal Amount
        +string RazorpayOrderId
        +string RazorpayPaymentId
        +string RazorpaySignature
        +int UserId
        +string UserEmail
        +string UserName
    }

    class ReportFailureDto {
        +int BookingId
        +int UserId
        +string UserEmail
        +string UserName
        +string Reason
    }
```

---

## 7. Comprehensive Domain Models & Inheritance Map

Illustrates how all persistence entity models across the 4 microservices inherit from `BaseEntity` and maintain domain properties.

```mermaid
classDiagram
    direction TB

    class BaseEntity {
        +int Id
        +DateTime CreatedAt
        +DateTime? UpdatedAt
    }

    class BackofficeProfile {
        +string Email
        +string PasswordHash
        +string Name
        +string Role
        +bool IsActive
        +string? ResetToken
        +DateTime? ResetTokenExpiry
        +string Department
        +string RoleTitle
        +string AssignedAirportCode
        +string? PhoneNumber
    }

    class PassengerProfile {
        +string Email
        +string PasswordHash
        +string Name
        +string FirstName
        +string LastName
        +string Role
        +bool IsEmailVerified
        +string? VerificationToken
        +DateTime? VerificationTokenExpiry
        +bool IsActive
        +string? ResetToken
        +DateTime? ResetTokenExpiry
        +string PassportNumber
        +string Nationality
        +DateTime? DateOfBirth
        +string Phone
        +string Aadhar
        +string Gender
        +string DietaryRequirements
        +string MedicalNeeds
        +string MedicalAlerts
        +byte[]? ProfileImage
    }

    class SavedPassenger {
        +int PassengerProfileId
        +string Name
        +int Age
        +string Gender
        +string Aadhar
        +string PassportNumber
        +string Nationality
        +string DietaryRequirements
        +string MedicalNeeds
        +string MedicalAlerts
    }

    class Flight {
        +string FlightNumber
        +string Source
        +string Destination
        +DateTime DepartureTime
        +DateTime ArrivalTime
        +string Gate
        +string Aircraft
        +FlightStatus Status
        +int TotalSeats
        +int AvailableSeats
        +int EconomySeats
        +int BusinessSeats
        +int FirstSeats
        +string CrewAssignment
        +decimal EconomyPrice
        +decimal BusinessPrice
        +decimal FirstClassPrice
    }

    class FlightSchedule {
        +int FlightId
        +DateTime DepartureTime
        +DateTime ArrivalTime
        +string Gate
        +FlightStatus Status
        +int TotalSeats
        +int AvailableSeats
        +int EconomySeats
        +int BusinessSeats
        +int FirstSeats
        +decimal EconomyPrice
        +decimal BusinessPrice
        +decimal FirstClassPrice
    }

    class Booking {
        +int UserId
        +string UserEmail
        +string UserName
        +int FlightId
        +int? ScheduleId
        +SeatClass SeatClass
        +decimal BaggageWeight
        +string PNR
        +BookingStatus Status
        +PaymentStatus PaymentStatus
        +int TotalPassengers
        +int ConfirmedPassengers
        +int CancelledPassengers
        +decimal TotalAmount
    }

    class BookingPassenger {
        +int BookingId
        +string Name
        +int Age
        +string Gender
        +string AadharCardNo
        +string PassportNumber
        +string Nationality
        +string DietaryRequirements
        +string MedicalNeeds
        +string MedicalAlerts
        +BookingPassengerStatus Status
        +decimal Fare
        +DateTime? CancelledAt
        +string? CancellationReason
        +string? SeatNumber
    }

    class CheckIn {
        +int BookingId
        +int PassengerId
        +int UserId
        +int FlightId
        +string SeatNumber
        +string Gate
        +string BoardingPass
        +string QRCode
        +DateTime CheckInTime
        +bool IsCheckedIn
    }

    class Payment {
        +int BookingId
        +decimal Amount
        +PaymentStatus Status
        +string PaymentMethod
        +string TransactionId
    }

    BaseEntity <|-- BackofficeProfile
    BaseEntity <|-- PassengerProfile
    BaseEntity <|-- SavedPassenger
    BaseEntity <|-- Flight
    BaseEntity <|-- FlightSchedule
    BaseEntity <|-- Booking
    BaseEntity <|-- BookingPassenger
    BaseEntity <|-- CheckIn
    BaseEntity <|-- Payment
```

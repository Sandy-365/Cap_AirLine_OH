# Admin Service Migration Guide

## Current Issues Identified
1. **Mixed Responsibilities**: `AdminAuthController` handles authentication but is placed alongside general `AdminController`
2. **Naming Inconsistencies**: DTO files use plural form (`AdminDtos.cs`)
3. **Service Implementation Clarity**: `AdminServiceImpl.cs` vs `AdminService.cs` confusion

## Migration Steps

### Step 1: Create Auth Subfolder Structure
```
AirlineManagementSystem/main/Services/AdminService
├── Controllers
│   ├── Auth
│   │   └── AdminAuthController.cs
│   └── AdminController.cs
├── Services
│   ├── Auth
│   │   ├── IAdminAuthService.cs
│   │   └── AdminAuthService.cs
│   ├── IAdminService.cs
│   └── AdminServiceImpl.cs
├── DTOs
│   ├── Auth
│   │   ├── AdminAuthDto.cs
│   │   └── AdminLoginDto.cs
│   └── AdminDto.cs
└── Data
    ├── AdminDbContext.cs
    └── Migrations/
```

### Step 2: Move Files
```bash
# Move auth controllers
mv Controllers/AdminAuthController.cs Controllers/Auth/

# Move auth services
mv Services/IAdminAuthService.cs Services/Auth/
mv Services/AdminAuthService.cs Services/Auth/

# Move auth DTOs
mv DTOs/AdminAuthDtos.cs DTOs/Auth/AdminAuthDto.cs
```

### Step 3: Update File Contents

**Controllers/Auth/AdminAuthController.cs**
```csharp
using AdminService.DTOs.Auth;
using AdminService.Services.Auth;
using Microsoft.AspNetCore.Mvc;

namespace AdminService.Controllers.Auth;

[ApiController]
[Route("api/auth")]
public class AdminAuthController : ControllerBase
{
    private readonly IAdminAuthService _authService;
    public AdminAuthController(IAdminAuthService authService) => _authService = authService;
    
    // ... existing methods with updated using statements
}
```

**Services/Auth/IAdminAuthService.cs**
```csharp
using AdminService.DTOs.Auth;
using System.Threading.Tasks;

namespace AdminService.Services.Auth;

public interface IAdminAuthService
{
    Task RegisterAsync(AdminRegisterDto dto);
    Task<AdminAuthResponseDto> VerifyAsync(AdminVerifyDto dto);
    // ... other auth methods
}
```

**DTOs/Auth/AdminAuthDto.cs**
```csharp
namespace AdminService.DTOs.Auth;

public class AdminRegisterDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Role { get; set; }
    public string? Department { get; set; }
    public bool ProvisionedByAdmin { get; set; }
}

public class AdminAuthResponseDto
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}
```

### Step 4: Update Program.cs and Startup
Add service registrations:
```csharp
// Auth services
builder.Services.AddScoped<IAdminAuthService, AdminAuthService>();

// General admin services
builder.Services.AddScoped<IAdminService, AdminServiceImpl>();
```

### Step 5: Update Namespaces
Update all moved files to use the new namespace structure:
- `AdminService.Controllers.Auth`
- `AdminService.Services.Auth`
- `AdminService.DTOs.Auth`

### Step 6: Verify Compilation
```bash
dotnet build
```

### Step 7: Update API Documentation
Update Swagger/OpenAPI documentation to reflect the new controller structure.

## Benefits of This Structure
1. **Clear Separation**: Authentication logic isolated from business logic
2. **Scalability**: Easy to add OAuth, JWT, or other auth mechanisms
3. **Maintainability**: Smaller, focused controllers and services
4. **Testability**: Auth components can be tested independently

## Potential Issues to Watch
- Ensure all moved files update their `using` statements
- Verify dependency injection still resolves correctly
- Test all endpoints after restructuring

## Rollback Plan
If issues arise:
1. Restore original file locations from Git
2. Recompile and test
3. Consider incremental migration instead of all-at-once

This migration guide provides a systematic approach to improving the Admin Service structure while maintaining functionality.
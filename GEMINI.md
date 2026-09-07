# Webview Edge Defense & SSO Proxy System (SSO Proxy)

This document serves as the foundational instruction context (`GEMINI.md`) for the SSO Proxy project. It defines the workspace topology, system architecture, core technologies, and development conventions that must be strictly adhered to during maintenance, debugging, and feature expansion.

---

## 1. Architectural & Purpose Overview

The **SSO Proxy** is a high-performance, non-intrusive (non-invasive) edge defense and single sign-on proxy system. It acts as a gatekeeper at the network boundary when native applications (iOS/Android/Desktop Clients) open protected internal web pages through an embedded Webview.

### Core Philosophy:
* **Strong Edge, Lean Backend**: The SSO Proxy handles all security, rate-limiting, authentication checks, and user session mapping. Target web services remain completely decoupled from authentication logic and simply trust downstream `X-User-Info` headers injected by the proxy.
* **Stateless Active-Active Cluster**: Multiple proxy nodes run in parallel under an F5 Big-IP LTM (or local load balancer container), allowing seamless horizontal scaling.
* **Sentinel-backed Resilience**: Real-time session validation with Redis Sentinel failover to prevent single-point-of-failure issues.
* **Standardized Diagnostics**: Clean, structured console outputs in JSON format to directly feed Filebeat/ELK monitoring pipelines.

```
       [ App / Native Client ]
                 │ (1. Initial API Authentication / login)
                 ▼
          [ SSO Proxy API ] ──(2. Generate & Cache Token)──► [ Redis Sentinel Pool ]
                 │ (3. Return 8hr JWE Token)
                 ▼
       [ Webview (Embedded) ]
                 │ (4. Request Page: Host: test1.gd.com, Authorization: Bearer <JWE>)
                 ▼
       [ F5 BIG-IP LTM (Nginx) ]
                 │ (5. Load Balances / Health Checks)
                 ▼
         [ SSO Proxy Node ]
                 │ (6. GlobalDefenseMiddleware intercepts request)
                 │     - Validate Token with Redis
                 │     - Strip 'Authorization' header
                 │     - Inject 'X-User-Info' payload header
                 ▼
      [ Target Web (1 or 2) ] (7. Directly read user profiles, render HTML)
```

---

## 2. Workspace & Technology Topology

The workspace is organized into discrete service components and configurations simulating a financial-grade hybrid environment locally:

```
sso/
├── GEMINI.md                    # This master instruction file
├── docker-compose.yml           # Complete PoC stack orchestrator
├── README.md                    # Basic project intro
├── dns-config/                  # CoreDNS configuration for test.gd.com resolution
├── f5-config/                   # Nginx-based simulation of F5 BIG-IP LTM
├── redis-config/                # Sentinel configuration
├── target-web1/                 # Stub html backend web app 1
├── target-web2/                 # Stub html backend web app 2
│
├── gd/                          # [Global Defense Core Assembly]
│   ├── Core/                    # Exception framework (GDException, GDExCode)
│   ├── Extensions/              # DI Scanning Utilities
│   └── Util/                    # Utilities (Profiler, StringUtil)
│
└── ssoproxy/                    # [SSO Proxy App Assembly]
    ├── Controllers/             # Token and Page controllers
    ├── Database/                # Redis interface, Sentinel connection manager
    ├── Middleware/              # GlobalDefenseMiddleware (the heart of the edge wall)
    ├── Services/                # Path validation, Token verification, Login services
    └── Views/                   # SSO login screens and layouts
```

### Key Technology Stack:
* **Backend Framework**: .NET 8.0 (LTS)
* **Reverse Proxy Engine**: YARP (Yet Another Reverse Proxy) 2.1.0 (configured dynamically via `appsettings.json`)
* **Session Cache**: StackExchange.Redis 2.7.33 (Sentinel aware)
* **Operating System**: Alpine Linux base docker images
* **Logging**: NLog 5.x + NLog.Web.AspNetCore

---

## 3. Core Development Conventions & Idiomatic Design

To ensure codebase integrity, any modifications must strictly conform to these established architectural styles and rules:

### A. Automatic Component Registration (Dependency Injection)
Instead of manually wiring every service in `Program.cs`, the project employs convention-based automatic registration via the `gd` assembly:
* **Rule**: Services placed in designated namespaces (e.g., `ssoproxy.Services`, `ssoproxy.Proxy`, `ssoproxy.Database`) must implement an interface named exactly `I{ClassName}` (e.g., `TokenService` implements `ITokenService`).
* **Implementation**: The extension `builder.Services.AddNamespaceComponents(namespaces, ServiceLifetime.Singleton)` in `Program.cs` dynamically registers all matching types.
* **Guideline**: When adding new services, follow this interface naming rule and place them under scanned namespaces to ensure they are automatically injected.

### B. Global Defense & Middleware Mechanics
The `GlobalDefenseMiddleware` executes the main defensive sequence:
1. **Bypass Check**: Evaluates the request path against green-light paths and regex-based whitelist profiles (`SsoWhitelist` in `appsettings.json`) via `PathValidatorService`.
2. **Token Extraction**: Extracts the `Authorization: Bearer <Token>` token. Redirects to `/Home/SsoLogin` with a normalized `returnUrl` if missing or invalid.
3. **Token Validation**: Interrogates the Redis Sentinel cache.
4. **Header Mutation**: **Must strip** the original `Authorization` header to prevent credentials leakage to backends and **inject** the verified JSON payload into `X-User-Info` header.

### C. Exception Handling and Error Standards
Never throw raw, generic exceptions. All business errors must utilize the custom `GDException` paired with structured `GDExCode` identifiers:
* **Pattern**: `throw new GDException(SsoproxyExCode.InvalidCredentials);`
* **Success Code**: `00-000`
* **Fallback Uncaught Code**: `99-999`
* All API controllers should wrap payloads in a standardized `ResponseEntity` mapping runtime execution metrics and custom exception codes.

### D. Cryptography and Token Design (JWE)
Tokens are generated as encrypted JSON Web Encryption (JWE) tokens under Compact Serialization format (`header.encryptedKey.iv.ciphertext.tag`):
* Key derivation: SHA256 hashed JWE Secret (loaded from config `Jwe:Secret`).
* Cipher: Authenticated encryption via **AES-GCM (128/256)** using `AesGcm` class.
* Safety Mandate: Always scrub secrets; never print, log, or hardcode private keys or JWE secrets. Use `IConfiguration` variables.

### E. Structured Logging Conventions
Diagnostic tracking is highly categorized. Use the following logging prefixes for consistent log-filtering:
* `[SSO_BYPASS]` - Path matches whitelist or login routes, bypassing validation.
* `[SSO_MISSING_TOKEN]` - Request has no credential, initiating redirect.
* `[SSO_AUTH_SUCCESS]` - Token validated successfully, forwarding to target backend.
* `[SSO_AUTH_FAIL]` - Token expired or altered, redirecting.
* `[SSO_GLOBAL_CRASH]` - Unhandled runtime exceptions caught in middleware.
* `[SSO_API_TOKEN_SUCCESS]` / `[SSO_API_TOKEN_ERROR]` - API login and token issuing state.

---

## 4. Local Building, Running & Simulation

### To Build and Run Locally (Individual Project):
Ensure you have .NET 8.0 SDK installed on your workstation.

```bash
# From sso root
dotnet build sso.sln

# Run ssoproxy on development server
dotnet run --project ssoproxy/ssoproxy.csproj
```

### To Run the Full PoC Environment (Docker Stack):
The system is built to simulate F5, CoreDNS, double proxies, backend web app targets, and a master-slave sentinel cache pool seamlessly.

```bash
# Build & spin up the entire cluster in background
docker-compose up --build -d

# Verify cluster nodes
docker-compose ps

# Monitor consolidated container logs
docker-compose logs -f
```

### Network Topology Ports (Simulated PoC):
* **F5 Load Balancer Gateway**: Exposes port `9999` to physical workstation. Direct your browser or native webview to `http://test1.gd.com:9999/` or `http://test2.gd.com:9999/` (ensure local hosts file maps these to `127.0.0.1`).
* **SSO Proxy Nodes**: Listen internally inside the safe network bridge `172.31.254.0/24` (IPs: `.11`, `.12`) on port `80`.
* **Redis Master/Slave Pool**: Redis Master at `.15`, Slave at `.16` with password `YourStrongPassword`.
* **Redis Sentinels**: Listen on `.21` and `.22` at port `26379`.

---

## 5. Quality Assurance & Verification

Currently, the workspace validates system correctness through comprehensive end-to-end integration testing in the local Docker environment:
* **Manual Verification**: Run `docker-compose up`, append hosts mapping to `/etc/hosts` (`127.0.0.1 test1.gd.com test2.gd.com`), navigate to `http://test1.gd.com:9999/`. Ensure unauthorized sessions are safely captured and routed to the login page, and authorized sessions pass with injected header elements correctly.
* **Unit Testing Guideline (Future Expansion)**: If adding automated unit/integration test suites, place them in a separate project named `ssoproxy.Tests` or `gd.Tests` mapping namespaces. Always test both bypass routing policies (Regex check) and cryptography-bound token decryptions using xUnit and Moq.

# ECommerceSolution

A production-style e-commerce backend API built with **ASP.NET Core 8**, **Entity Framework Core**, **MySQL/TiDB Cloud**, **JWT Authentication**, **Docker**, **GitHub Actions CI/CD**, and **Render** deployment.

## Live API

**Production API:**

https://ecommerce-api-f1vp.onrender.com

**Swagger UI:**

https://ecommerce-api-f1vp.onrender.com/swagger

---

## Features

* User registration and login
* JWT-based authentication
* Refresh token support
* Role-based authorization
* Admin and customer roles
* Product management
* Category management
* Shopping cart
* Wishlist
* Order management
* Payment management
* Cash on Delivery
* Khalti payment integration
* eSewa payment integration
* Product stock management
* Input validation
* Exception handling
* Secure configuration through environment variables
* Entity Framework Core migrations
* Integration testing
* Docker containerization
* GitHub Actions CI/CD
* Render deployment
* TiDB Cloud database

---

## Technology Stack

| Technology                       | Purpose                |
| -------------------------------- | ---------------------- |
| C#                               | Programming language   |
| .NET 8                           | Backend framework      |
| ASP.NET Core Web API             | REST API               |
| Entity Framework Core 8          | ORM                    |
| MySQL / TiDB Cloud               | Database               |
| Pomelo.EntityFrameworkCore.MySql | MySQL EF Core provider |
| JWT                              | Authentication         |
| Swagger / OpenAPI                | API documentation      |
| xUnit                            | Testing                |
| Docker                           | Containerization       |
| GitHub Actions                   | CI/CD                  |
| Render                           | Cloud deployment       |
| Khalti                           | Payment integration    |
| eSewa                            | Payment integration    |

---

## Architecture

The project follows a layered architecture separating API, application logic, domain models, and infrastructure concerns.

```text
ECommerceSolution
│
├── ECommerce.API
│   └── Controllers, middleware, configuration
│
├── ECommerce.Application
│   ├── DTOs
│   ├── Interfaces
│   ├── Services
│   └── Business logic
│
├── ECommerce.Domain
│   ├── Entities
│   ├── Enums
│   └── Domain models
│
├── ECommerce.Infrastructure
│   ├── DbContext
│   ├── Repositories
│   ├── External services
│   └── Payment integrations
│
└── ECommerce.Tests
    ├── Unit tests
    └── Integration tests
```

### Request Flow

```text
Client
   │
   ▼
ASP.NET Core API
   │
   ▼
Controllers
   │
   ▼
Application Services
   │
   ▼
Repository Interfaces
   │
   ▼
Infrastructure Repositories
   │
   ▼
Entity Framework Core
   │
   ▼
TiDB Cloud
```

---

## Authentication

The API uses JWT Bearer authentication.

Authentication flow:

```text
User
 │
 ▼
Register / Login
 │
 ▼
Authentication Service
 │
 ▼
JWT Access Token
 │
 ▼
Authorization Header
 │
 ▼
Protected API Endpoint
```

Example:

```http
Authorization: Bearer <access-token>
```

JWT configuration is stored through environment variables and is not committed to the repository.

---

## Authorization

The API uses role-based authorization.

### Customer

Customers can perform operations such as:

* Browse products
* Manage their cart
* Manage their wishlist
* Create orders
* View their orders
* Manage their own payments

### Admin

Administrators can perform management operations such as:

* Create products
* Update products
* Delete products
* Create categories
* Update categories
* Delete categories
* Manage orders
* Access administrative endpoints

The API also verifies that users cannot access resources belonging to other users.

---

## Product Management

Products contain information such as:

* Name
* Description
* Price
* Stock
* Category
* Active status
* Creation/update timestamps

Stock validation prevents customers from adding unavailable products to their carts.

---

## Shopping Cart

The cart supports:

* Adding products
* Updating quantities
* Removing products
* Viewing the current user's cart
* Stock validation
* Order creation from cart contents

The API prevents adding products when the requested quantity exceeds available stock.

---

## Wishlist

Authenticated users can:

* Add products to their wishlist
* Remove products
* View their wishlist
* Prevent duplicate wishlist entries

Wishlist data is associated with the authenticated user.

---

## Orders

The order system supports:

* Creating orders from the shopping cart
* Order items
* Product prices at order time
* Order totals
* Order status
* User-specific order history
* Administrative order management

Supported order statuses include:

```text
Pending
Confirmed
Processing
Shipped
Delivered
Cancelled
```

---

## Payments

The backend supports multiple payment methods.

### Cash on Delivery

Cash on Delivery payments are created directly through the API.

### Khalti

The project includes Khalti payment integration and payment verification functionality.

### eSewa

The project includes eSewa integration using the eSewa test/sandbox environment.

Payment operations are separated from the core order logic using payment services and repositories.

---

## Database

The application uses MySQL-compatible database technology.

Development:

```text
MySQL 8
```

Production:

```text
TiDB Cloud
```

Production database:

```text
Ecommercedb
```

Entity Framework Core migrations are used to manage database schema changes.

---

## Testing

The project includes automated tests using **xUnit**.

The test suite covers areas including:

* Authentication
* Registration
* Login
* Authorization
* Product management
* Category management
* Product-category relationships
* Shopping cart
* Orders
* Payments
* Validation
* Security

The integration test suite contains **94 tests**.

Expected result:

```text
Total: 94
Passed: 94
Failed: 0
Skipped: 0
```

Run the tests with:

```bash
dotnet test ECommerceSolution.slnx --configuration Release
```

---

## Docker

The API is containerized using Docker.

The project includes:

```text
Dockerfile
docker-compose.yml
```

Docker Compose can be used for local development with the API and MySQL database.

Production deployment uses the Docker configuration through Render.

Sensitive configuration such as JWT keys and database credentials is provided through environment variables rather than committed secrets.

---

## CI/CD

GitHub Actions is used for continuous integration.

The workflow performs:

```text
Git Push
   │
   ▼
GitHub Actions
   │
   ├── Restore
   ├── Build
   └── Test
```

The project uses automated testing to prevent broken code from progressing through the development workflow.

---

## Deployment

The production API is deployed on **Render**.

Deployment architecture:

```text
GitHub
   │
   ▼
GitHub Actions
   │
   ▼
Render
   │
   ▼
Docker Container
   │
   ▼
ASP.NET Core API
   │
   ▼
TiDB Cloud
```

Production environment secrets are configured through Render environment variables.

---

## API Documentation

Interactive API documentation is available through Swagger.

**Swagger UI:**

https://ecommerce-api-f1vp.onrender.com/swagger

Swagger can be used to test:

* Authentication
* Products
* Categories
* Cart
* Wishlist
* Orders
* Payments
* Administrative endpoints

Protected endpoints require a JWT access token.

---

## Security

Security practices implemented in the project include:

* JWT authentication
* Role-based authorization
* User-specific resource authorization
* Environment-based secret configuration
* Password hashing
* Input validation
* Protected administrative endpoints
* Database access through Entity Framework Core
* Automated authorization tests

Production secrets are not stored in the Git repository.

---

## Environment Configuration

The application uses environment variables for sensitive configuration.

Examples include:

```text
ConnectionStrings__DefaultConnection
Jwt__Key
Jwt__Issuer
Jwt__Audience
```

A local `.env` file may be used for development, while production secrets are configured through Render.

The `.env` file must never be committed to Git.

---

## Running Locally

Clone the repository:

```bash
git clone https://github.com/parth1-web/ECommerceSolution.git
```

Move into the project:

```bash
cd ECommerceSolution
```

Restore dependencies:

```bash
dotnet restore ECommerceSolution.slnx
```

Build:

```bash
dotnet build ECommerceSolution.slnx
```

Run tests:

```bash
dotnet test ECommerceSolution.slnx
```

Run the API:

```bash
dotnet run --project ECommerce.API
```

Swagger will be available at the local Swagger URL displayed by ASP.NET Core.

---

## Docker Development

Build the API image:

```bash
docker build -f ECommerce.API/Dockerfile -t ecommerce-api .
```

Run using Docker Compose:

```bash
docker compose up -d
```

Check running containers:

```bash
docker compose ps
```

View API logs:

```bash
docker logs ecommerce-api
```

Stop the containers:

```bash
docker compose down
```

---

## Project Goals

This project was developed to demonstrate practical backend development skills using modern .NET technologies.

The project demonstrates:

* Clean separation of responsibilities
* RESTful API development
* Database design
* Entity Framework Core
* Repository and service patterns
* Authentication and authorization
* Payment integrations
* Automated testing
* Docker
* CI/CD
* Cloud deployment
* Production configuration management

---

## Future Improvements

Potential future improvements include:

* Frontend application
* Redis caching
* Background jobs
* Email notifications
* Advanced product search
* Pagination and filtering
* Rate limiting
* Centralized logging
* Monitoring and observability
* Kubernetes deployment
* Automated production database backup
* API versioning

---

## Author

**Parth**

GitHub:

https://github.com/parth1-web

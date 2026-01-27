# Quater Water Quality Lab Management System
This is a monorepo project for the Quater water quality lab management system.

## Project Structure
- `backend/` - ASP.NET Core 10.0 Web API & Core Logic
- `desktop/` - Avalonia UI 11.x Desktop Application (Windows/Linux/macOS)
- `mobile/` - React Native 0.73+ Mobile Application (Android)
- `specs/` - Feature specifications and planning documents (Speckit)

## Monorepo Conventions
- **Backend**: C# 13, .NET 10, PostgreSQL, Entity Framework Core
- **Desktop**: C# 13, .NET 10, Avalonia UI, SukiUI
- **Mobile**: TypeScript, React Native, Yarn
- **Task Tracking**: Use `bd` (Beads) for all task tracking.

---

## Build, Lint & Test Commands

### Backend (.NET 10)
```bash
# Build
dotnet build backend/Quater.Backend.sln
dotnet build backend/Quater.Backend.sln --configuration Release

# Run tests
dotnet test backend/tests/Quater.Backend.Api.Tests/
dotnet test backend/tests/Quater.Backend.Core.Tests/
dotnet test backend/tests/Quater.Backend.Sync.Tests/

# Run single test
dotnet test --filter "FullyQualifiedName=Quater.Backend.Api.Tests.SampleControllerTests.CreateSample_ValidData_ReturnsCreated"
dotnet test --filter "FullyQualifiedName~SampleController"

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Lint
dotnet format backend/Quater.Backend.sln --verify-no-changes

# Run API locally
cd backend/src/Quater.Backend.Api
dotnet run
```

### Desktop (Avalonia)
```bash
# Build
dotnet build desktop/Quater.Desktop.sln

# Run tests
dotnet test desktop/tests/Quater.Desktop.Tests/
dotnet test desktop/tests/Quater.Desktop.Data.Tests/

# Run desktop app
cd desktop/src/Quater.Desktop
dotnet run
```

### Mobile (React Native)
```bash
cd mobile/

# Install dependencies
yarn install

# Lint & Format
yarn lint
yarn lint:fix

# Type check
yarn type-check

# Run tests
yarn test
yarn test SampleScreen.test.tsx
yarn test --testNamePattern="should create sample"

# Build & run Android
yarn android
```

---

## C# Code Style Guidelines (Backend/Desktop)

### Core Principles
- **Version**: .NET 10, C# 13
- **Nullability**: `#nullable enable` is required. Treat warnings as errors.
- **Implicit Usings**: Enabled.

### Type System & Patterns
- **Primary Constructors**: Use for all classes/records to reduce boilerplate.
- **File-Scoped Namespaces**: Always use to save indentation.
- **Collection Expressions**: Use `[]` for initialization.
- **Discriminated Unions**: Use abstract record hierarchies.
- **Strongly Typed IDs**: Use `readonly record struct`.

### Async & Performance
- **CancellationTokens**: Propagate to all async methods.
- **Async All the Way**: Never use `.Result` or `.Wait()`.
- **TimeProvider**: Inject `TimeProvider` instead of using `DateTime.Now`.
- **Structured Logging**: Use `nameof` and log properties.

### Organization
- **PascalCase**: Classes, methods, properties.
- **camelCase**: Local variables, parameters, private fields.
- **Imports Order**: System -> Third-party -> Project.

---

## TypeScript/React Native Guidelines (Mobile)

### Configuration
- **Linter**: ESLint with `@react-native` and `@typescript-eslint/recommended`.
- **Formatter**: Prettier (Single quotes, no semi-colons, 100 char width).

### Principles
- **Strict Typing**: No `any`. Use `unknown` if necessary. Explicit return types.
- **Components**: Functional components with hooks.
- **State**: Local state with `useState`, global with Context/Redux (if applicable).
- **Navigation**: React Navigation.

### Naming
- **PascalCase**: Components, types, interfaces.
- **camelCase**: Variables, functions, hooks.
- **UPPER_SNAKE_CASE**: Constants.

---

## Workflow & Protocol

### Beads (Task Tracking)
Use `bd` to manage tasks.
- `bd ready`: Find work.
- `bd update <id> --status=in_progress`: Start work.
- `bd close <id>`: Finish work.
- `bd sync`: Sync with remote.

### Session Close Protocol (MANDATORY)
1. **Close Issues**: `bd close <ids>`
2. **Sync Beads**: `bd sync --from-main`
3. **Commit Code**: `git add . && git commit -m "..."`
4. **Push**: `git push` (Ensure success!)

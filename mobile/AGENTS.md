# Mobile Agent Instructions - Quater Water Quality Lab Management System

## Build, Lint & Test Commands

### Mobile (React Native + Yarn)

```bash
cd mobile/

# Install dependencies
yarn install

# Lint
yarn lint
yarn lint:fix

# Type check
yarn type-check

# Run tests
yarn test
yarn test SampleScreen.test.tsx  # Single test file
yarn test --testNamePattern="should create sample"  # Single test

# Run with coverage
yarn test --coverage

# Build & run Android
yarn android

# Build release APK
cd android && ./gradlew assembleRelease
```

---

## TypeScript Code Style Guidelines (Mobile)

### Naming Conventions

- **PascalCase**: Components, types, interfaces (`SampleScreen`, `Sample`, `ISampleService`)
- **camelCase**: Variables, functions, properties (`sampleId`, `createSample()`)
- **UPPER_SNAKE_CASE**: Constants (`API_BASE_URL`, `MAX_RETRY_COUNT`)

### Formatting

- **Indentation**: 2 spaces
- **Semicolons**: Required
- **Quotes**: Single quotes for strings
- **Line length**: Max 100 characters

### Types

- Always use TypeScript, never `any` (use `unknown` if truly unknown)
- Define interfaces for all data structures
- Use type inference where obvious: `const count = 0;`
- Explicit return types for functions

### Imports Order

```typescript
// React/React Native first
import React, { useState, useEffect } from 'react';
import { View, Text, Button } from 'react-native';

// Third-party packages
import { useNavigation } from '@react-navigation/native';

// Project imports (absolute paths via tsconfig)
import { Sample } from '@/types/sample';
import { useSampleService } from '@/services/sampleService';
import { SampleForm } from '@/components/SampleForm';
```

---

## Code Organization and Structure

### Directory Structure
- `src/app/`: Application entry point and navigation setup.
- `src/components/`: Reusable UI components (buttons, cards, inputs).
- `src/features/`: Feature-based modules (e.g., `samples/`, `auth/`).
- `src/hooks/`: Custom React hooks (e.g., `useNetworkStatus`).
- `src/services/`: API clients and business logic services.
- `src/store/`: State management (Redux/Zustand) and contexts.
- `src/types/`: Global TypeScript definitions.
- `src/utils/`: Helper functions and utilities.
- `src/assets/`: Images, fonts, and local assets.

### File Naming
- Components: `PascalCase.tsx` (e.g., `SampleCard.tsx`).
- Hooks: `camelCase.ts` (e.g., `useAuth.ts`).
- Services/Utils: `camelCase.ts` (e.g., `apiClient.ts`, `dateFormatter.ts`).
- Tests: `Filename.test.tsx` (co-located with source file).

### Module Organization
- Use index files (`index.ts`) for cleaner imports from directories.
- Group related features together (co-location principle).

### Component Architecture
- **Container vs. Presentational**: Separate logic (Container) from UI rendering (Presentational).
- **Atomic Design**: Structure components as Atoms, Molecules, and Organisms where appropriate.

### Code Splitting
- Lazy load large components or screens using `React.lazy` and `Suspense` where appropriate.

---

## Common Patterns and Anti-patterns

### Design Patterns
- **Custom Hooks**: Encapsulate reusable logic (e.g., form handling, API calls).
- **Context API**: For global state like themes or user sessions (avoid overusing for rapidly changing data).
- **HOCs (Higher-Order Components)**: Use sparingly; prefer hooks for logic reuse.

### Recommended Approaches
- **Functional Components**: Use functional components with hooks exclusively.
- **Controlled Components**: Manage form inputs via state.
- **Destructuring**: Destructure props and state for readability.

### Anti-patterns
- **Prop Drilling**: Avoid passing props through many layers; use Context or Composition.
- **Large Components**: Break down components exceeding 200-300 lines.
- **Inline Styles**: Avoid inline styles; use `StyleSheet.create`.
- **Index as Key**: Do not use array index as keys in lists; use unique IDs.

### State Management
- Use local state (`useState`) for UI-specific data.
- Use global state (Context/Zustand) for shared data (auth, user profile).
- **Immutability**: Always treat state as immutable.

### Error Handling
- Use Error Boundaries to catch UI crashes.
- Handle API errors gracefully with `try/catch` and user feedback (toasts/alerts).

---

## Performance Considerations

### Optimization
- **Memoization**: Use `React.memo`, `useMemo`, and `useCallback` to prevent unnecessary re-renders.
- **FlatList**: Use `FlatList` for long lists instead of `ScrollView`. Implement `keyExtractor`, `getItemLayout`.

### Memory Management
- Clean up side effects in `useEffect` (timers, subscriptions).
- Avoid creating new object/array references in render methods.

### Rendering
- Minimize the number of re-renders.
- Use `shouldComponentUpdate` logic via `React.memo`.

### Bundle Size
- Tree-shake imports.
- Monitor bundle size; avoid heavy libraries (e.g., prefer `date-fns` over `moment`).

### Lazy Loading
- Load images efficiently; use caching strategies.

---

## Security Best Practices

### Vulnerabilities
- Keep dependencies updated (`yarn audit`).
- Avoid `eval()` or dangerous code execution.

### Validation
- Validate all user inputs (forms) and API responses (Zod/Yup).

### Authentication & Data Protection
- Store sensitive tokens in `EncryptedStorage` or `Keychain` (NOT `AsyncStorage`).
- Use HTTPS for all network requests.

### Secure API
- Implement SSL pinning if high security is required.
- Don't expose API keys in the client code (use environment variables properly).

---

## Testing Approaches

### Unit Testing
- Test utility functions and hooks.
- Use Jest for logic testing.

### Integration Testing
- Test component interactions and state changes.
- Use `@testing-library/react-native`.

### End-to-End (E2E) Testing
- Simulate real user scenarios.
- Use Detox or Maestro for E2E flows.

### Mocking
- Mock API calls using `msw` or Jest mocks.
- Mock native modules that aren't available in the test environment.

---

## Common Pitfalls and Gotchas

### Mistakes
- Forgetting to request permissions for hardware features (Camera, Location).
- Modifying state directly.

### Edge Cases
- Handle offline state gracefully (NetInfo).
- Account for different screen sizes and safe areas.

### Version Issues & Compatibility
- React Native upgrades can be tricky; follow the upgrade helper.
- Verify library compatibility with the current React Native version.

### Debugging
- Use Flipper or React Native Debugger.
- Use `console.log` responsibly; remove in production.

---

## Tooling and Environment

### Dev Tools
- React Native Debugger / Flipper.
- React DevTools.

### Build Configuration
- Use `env` files for configuration (`react-native-config`).

### Linting & Formatting
- ESLint and Prettier are mandatory.
- Use `husky` for pre-commit hooks.

### Deployment / CI/CD
- Use Fastlane for automating builds and deployments.
- CI/CD pipelines (GitHub Actions) for running tests and linting on PRs.

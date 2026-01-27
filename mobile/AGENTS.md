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

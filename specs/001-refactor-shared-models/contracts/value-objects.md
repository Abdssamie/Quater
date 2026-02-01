# Value Object Contracts

**Feature**: Refactor Shared Models for Consistency and Maintainability  
**Date**: 2025-01-17

## Overview

This document defines the contracts for value objects used in the shared models, including validation rules, serialization formats, and usage examples.

---

## Location Value Object

### Contract

**Type**: `Quater.Shared.ValueObjects.Location`  
**Purpose**: Encapsulate GPS coordinates with validation

### Properties

| Property | Type | Required | Validation | Description |
|----------|------|----------|------------|-------------|
| `Latitude` | `double` | Yes | -90 to 90 | GPS latitude coordinate |
| `Longitude` | `double` | Yes | -180 to 180 | GPS longitude coordinate |
| `Description` | `string?` | No | Max 200 chars | Human-readable location name |
| `Hierarchy` | `string?` | No | Max 500 chars | Hierarchical path |

### Validation Rules

1. **Latitude**: Must be between -90 and 90 (inclusive)
2. **Longitude**: Must be between -180 and 180 (inclusive)
3. **Description**: Optional, max 200 characters
4. **Hierarchy**: Optional, max 500 characters

### JSON Serialization

```json
{
  "latitude": 33.5731,
  "longitude": -7.5898,
  "description": "Municipal Well #3",
  "hierarchy": "Casablanca/Anfa/Site-A"
}
```

---

## Measurement Value Object

### Contract

**Type**: `Quater.Shared.ValueObjects.Measurement`  
**Purpose**: Encapsulate test measurement with parameter/unit validation

### Properties

| Property | Type | Required | Validation | Description |
|----------|------|----------|------------|-------------|
| `ParameterId` | `Guid` | Yes | Must reference existing Parameter | Foreign key to Parameter |
| `Value` | `double` | Yes | Within Parameter range | Measured value |
| `Unit` | `string` | Yes | Must match Parameter.Unit | Unit of measurement |

### Validation Rules

1. **ParameterId**: Must reference existing Parameter
2. **Value**: Must be within Parameter.MinValue/MaxValue
3. **Unit**: Must match Parameter.Unit (case-insensitive)

### JSON Serialization

```json
{
  "parameterId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "value": 7.2,
  "unit": "pH"
}
```

---

## EntityType Enum

### Values

| Value | Integer | Description |
|-------|---------|-------------|
| `Lab` | 1 | Lab entity |
| `User` | 2 | User entity |
| `Sample` | 3 | Sample entity |
| `TestResult` | 4 | TestResult entity |
| `Parameter` | 5 | Parameter entity |

**Contract Status**: ✅ COMPLETE

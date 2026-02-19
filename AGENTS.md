# Agent Instructions - Quater Water Quality Lab Management System

## Project Overview

**Quater** - Water Quality Lab Management System
- **Backend**: ASP.NET Core 10.0 + PostgreSQL
- **Desktop**: Avalonia UI 11.x (offline-first with SQLite)
- **Mobile**: React Native 0.73+ (Android field collection only)
- **Architecture**: Offline-first with optimistic concurrency (RowVersion)

## Code Style

- **Primary constructors** (C# 14) for DI
- **Use**: `string.Empty`, `[]` (collection expressions), `CancellationToken ct = default`

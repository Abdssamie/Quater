# Quater Mobile - Sample Collection App

## Overview

Mobile application for field water sample collection with offline-first architecture.

## Features Implemented

### 1. Sample Collection Screen
- GPS location capture with fallback strategy
- Sample type selection (5 types)
- Form validation
- Offline SQLite storage
- Collection date/time tracking
- Collector name and notes

### 2. Sample List Screen
- View all collected samples
- Pull-to-refresh
- Sync status indicators
- Navigate to sample details

### 3. Services
- **DatabaseService**: SQLite CRUD operations with WAL mode
- **LocationService**: GPS capture with 3-tier fallback (GPS → Network → Last Known)

### 4. Components
- SampleTypePicker: Dropdown for sample types
- LocationDisplay: GPS coordinates display
- LoadingSpinner: Loading indicator
- ErrorMessage: Error display component

## Setup Instructions

### Prerequisites
- Node.js 20+
- Yarn
- Android Studio (for Android development)
- React Native CLI

### Installation

```bash
cd mobile
yarn install
```

### Android Permissions

Location permissions are already configured in `android/app/src/main/AndroidManifest.xml`:
- ACCESS_FINE_LOCATION
- ACCESS_COARSE_LOCATION

### Running the App

```bash
# Start Metro bundler
yarn start

# Run on Android
yarn android
```

## Testing the Sample Collection Flow

### 1. Launch the App
- App opens to the Sample List screen
- Initially empty with "No Samples Yet" message

### 2. Create a New Sample
- Tap the blue "+" FAB button (bottom right)
- Navigate to "New Sample Collection" screen

### 3. Fill Out the Form
- **Sample Type**: Select from dropdown (default: Drinking Water)
- **Location**: Tap "Capture Location" button
  - Grant location permission when prompted
  - Wait for GPS coordinates to appear
  - If GPS fails, manual entry fields will appear
- **Location Description** (optional): e.g., "Municipal Well #3"
- **Location Hierarchy** (optional): e.g., "Morocco/Casablanca/District 5"
- **Collector Name** (required): Enter your name
- **Notes** (optional): Additional information

### 4. Save the Sample
- Tap "Save Sample" button
- Success alert appears
- Automatically navigates back to Sample List

### 5. View Saved Samples
- Sample appears in the list with:
  - Sample type
  - Location description or coordinates
  - Collection date/time
  - Collector name
  - Sync status badge (orange "Not Synced")

### 6. Refresh the List
- Pull down to refresh the sample list

### 7. View Sample Details
- Tap on any sample card
- Navigate to Sample Details screen (shows ID)

## Data Model

```typescript
interface Sample {
  id: string;                    // UUID
  type: SampleType;              // DrinkingWater, Wastewater, etc.
  locationLatitude: number;      // -90 to 90
  locationLongitude: number;     // -180 to 180
  locationDescription?: string;  // Max 200 chars
  locationHierarchy?: string;    // Max 500 chars
  collectionDate: string;        // ISO 8601
  collectorName: string;         // Max 100 chars, required
  notes?: string;                // Max 1000 chars
  status: SampleStatus;          // Pending, Completed, Archived
  isSynced: boolean;             // Sync status
  createdDate: string;           // ISO 8601
  lastModified: string;          // ISO 8601
}
```

## Performance

- **Target**: Sample creation in < 2 minutes ✅
- **GPS Capture**: 10s timeout for GPS, 5s for network
- **Database**: SQLite with WAL mode for better concurrency
- **Battery**: Single location update (not continuous tracking)

## Offline Support

- All data stored locally in SQLite
- No internet connection required for sample collection
- Samples marked as "Not Synced" until backend sync implemented
- Database persists across app restarts

## Architecture

```
mobile/
├── src/
│   ├── components/          # Reusable UI components
│   │   ├── ErrorMessage.tsx
│   │   ├── LoadingSpinner.tsx
│   │   ├── LocationDisplay.tsx
│   │   └── SampleTypePicker.tsx
│   ├── navigation/          # Navigation configuration
│   │   └── AppNavigator.tsx
│   ├── screens/             # Screen components
│   │   ├── HomeScreen.tsx
│   │   ├── SampleCollectionScreen.tsx   │   ├── SampleListScreen.tsx
│   │   └── SampleScreen.tsx
│   ├── services/            # Business logic services
│   │   ├── DatabaseService.ts
│   │   ├── LocationService.ts
│   │   └── logger.ts
│   └── types/               # TypeScript type definitions
│       ├── Location.ts
│       ├── navigation.ts
│       └── Sample.ts
├── android/                 # Android native code
└── App.tsx                  # Root component
```

## Known Limitations

- iOS support not yet implemented (Android only)
- Backend sync not implemented (Phase 2)
- Sample editing not implemented
- Sample deletion not implemented
- No map view for location selection

## Next Steps

1. Implement backend API integration
2. Add bidirectional sync functionality
3. Implement sample editing
4. Add iOS support
5. Add map view for location selection
6. Implement sample deletion with soft delete
7. Add offline queue for sync operations

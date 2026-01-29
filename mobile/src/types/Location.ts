export interface LocationCoordinates {
  latitude: number;
  longitude: number;
  accuracy?: number;
  altitude?: number;
  timestamp?: number;
}

export interface LocationError {
  code: number;
  message: string;
}

export type LocationSource = 'gps' | 'network' | 'lastKnown' | 'manual';

export interface LocationResult {
  coordinates: LocationCoordinates;
  source: LocationSource;
  error?: LocationError;
}

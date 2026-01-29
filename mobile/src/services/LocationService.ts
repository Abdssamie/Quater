import Geolocation from 'react-native-geolocation-service';
import { Platform, PermissionsAndroid } from 'react-native';
import { LocationCoordinates, LocationResult, LocationError } from '../types/Location';
import { logger } from './logger';

class LocationService {
  private lastKnownLocation: LocationCoordinates | null = null;
  private lastLocationTimestamp: number = 0;

  async requestLocationPermission(): Promise<boolean> {
    if (Platform.OS === 'android') {
      try {
        const granted = await PermissionsAndroid.request(
          PermissionsAndroid.PERMISSIONS.ACCESS_FINE_LOCATION,
          {
            title: 'Location Permission',
            message: 'Quater needs access to your location to record sample collection sites.',
            buttonNeutral: 'Ask Me Later',
            buttonNegative: 'Cancel',
            buttonPositive: 'OK',
          }
        );

        if (granted === PermissionsAndroid.RESULTS.GRANTED) {
          logger.info('Location permission granted');
          return true;
        } else {
          logger.warn('Location permission denied');
          return false;
        }
      } catch (error) {
        logger.error('Failed to request location permission', error);
        return false;
      }
    }

    // iOS permissions are handled via Info.plist
    return true;
  }

  async getCurrentLocation(): Promise<LocationResult> {
    const hasPermission = await this.requestLocationPermission();
    if (!hasPermission) {
      return {
        coordinates: { latitude: 0, longitude: 0 },
        source: 'manual',
        error: {
          code: -1,
          message: 'Location permission denied',
        },
      };
    }

    // Try GPS first (high accuracy, 10s timeout)
    try {
      const gpsLocation = await this.getLocationWithTimeout(10000, true);
      this.updateLastKnownLocation(gpsLocation);
      return {
        coordinates: gpsLocation,
        source: 'gps',
      };
    } catch (gpsError) {
      logger.warn('GPS location failed, trying network', gpsError);

      // Fallback to network provider (5s timeout)
      try {
        const networkLocation = await this.getLocationWithTimeout(5000, false);
        this.updateLastKnownLocation(networkLocation);
        return {
          coordinates: networkLocation,
          source: 'network',
        };
      } catch (networkError) {
        logger.warn('Network location failed, checking last known', networkError);

        // Use last known location if recent (<30 minutes)
        if (this.isLastKnownLocationRecent()) {
          logger.info('Using last known location');
          return {
            coordinates: this.lastKnownLocation!,
            source: 'lastKnown',
          };
        }

        // All methods failed
        logger.error('All location methods failed');
        return {
          coordinates: { latitude: 0, longitude: 0 },
          source: 'manual',
          error: {
            code: -1,
            message: 'Unable to determine location. Please enter coordinates manually.',
          },
        };
      }
    }
  }

  private getLocationWithTimeout(
    timeout: number,
    enableHighAccuracy: boolean
  ): Promise<LocationCoordinates> {
    return new Promise((resolve, reject) => {
      Geolocation.getCurrentPosition(
        (position) => {
          resolve({
            latitude: position.coords.latitude,
            longitude: position.coords.longitude,
            accuracy: position.coords.accuracy,
            altitude: position.coords.altitude || undefined,
            timestamp: position.timestamp,
          });
        },
        (error) => {
          reject({
            code: error.code,
            message: error.message,
          });
        },
        {
          enableHighAccuracy,
          timeout,
          maximumAge: 0,
        }
      );
    });
  }

  private updateLastKnownLocation(location: LocationCoordinates): void {
    this.lastKnownLocation = location;
    this.lastLocationTimestamp = Date.now();
  }

  private isLastKnownLocationRecent(): boolean {
    if (!this.lastKnownLocation) {
      return false;
    }

    const thirtyMinutesInMs = 30 * 60 * 1000;
    const age = Date.now() - this.lastLocationTimestamp;
    return age < thirtyMinutesInMs;
  }

  validateCoordinates(latitude: number, longitude: number): boolean {
    return (
      latitude >= -90 &&
      latitude <= 90 &&
      longitude >= -180 &&
      longitude <= 180
    );
  }
}

export const locationService = new LocationService();

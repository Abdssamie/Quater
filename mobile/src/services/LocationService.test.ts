import { locationService } from '../LocationService';
import Geolocation from 'react-native-geolocation-service';
import { PermissionsAndroid, Platform } from 'react-native';

jest.mock('react-native-geolocation-service');
jest.mock('@/services/logger');

describe('LocationService', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('validateCoordinates', () => {
    it('validates correct coordinates', () => {
      expect(locationService.validateCoordinates(33.5731, -7.5898)).toBe(true);
      expect(locationService.validateCoordinates(0, 0)).toBe(true);
      expect(locationService.validateCoordinates(90, 180)).toBe(true);
      expect(locationService.validateCoordinates(-90, -180)).toBe(true);
    });

    it('rejects invalid coordinates', () => {
      expect(locationService.validateCoordinates(91, 0)).toBe(false);
      expect(locationService.validateCoordinates(-91, 0)).toBe(false);
      expect(locationService.validateCoordinates(0, 181)).toBe(false);
      expect(locationService.validateCoordinates(0, -181)).toBe(false);
    });
  });

  describe('requestLocationPermission', () => {
    it('returns true on iOS', async () => {
      Platform.OS = 'ios';
      const result = await locationService.requestLocationPermission();
      expect(result).toBe(true);
    });

    it('requests permission on Android', async () => {
      Platform.OS = 'android';
      (PermissionsAndroid.request as jest.Mock).mockResolvedValue(PermissionsAndroid.RESULTS.GRANTED);

      const result = await locationService.requestLocationPermission();

      expect(result).toBe(true);
      expect(PermissionsAndroid.request).toHaveBeenCalledWith(
        PermissionsAndroid.PERMISSIONS.ACCESS_FINE_LOCATION,
        expect.any(Object)
      );
    });

    it('returns false when permission denied on Android', async () => {
      Platform.OS = 'android';
      (PermissionsAndroid.request as jest.Mock).mockResolvedValue(PermissionsAndroid.RESULTS.DENIED);

      const result = await locationService.requestLocationPermission();

      expect(result).toBe(false);
    });
  });

  describe('getCurrentLocation', () => {
    it('returns GPS location on success', async () => {
      Platform.OS = 'ios';
      const mockPosition = {
        coords: {
          latitude: 33.5731,
          longitude: -7.5898,
          accuracy: 10,
          altitude: 100,
        },
        timestamp: Date.now(),
      };

      (Geolocation.getCurrentPosition as jest.Mock).mockImplementation((success) => {
        success(mockPosition);
      });

      const result = await locationService.getCurrentLocation();

      expect(result.source).toBe('gps');
      expect(result.coordinates.latitude).toBe(33.5731);
      expect(result.coordinates.longitude).toBe(-7.5898);
      expect(result.error).toBeUndefined();
    });

    it('returns error when permission denied', async () => {
      Platform.OS = 'android';
      (PermissionsAndroid.request as jest.Mock).mockResolvedValue(PermissionsAndroid.RESULTS.DENIED);

      const result = await locationService.getCurrentLocation();

      expect(result.source).toBe('manual');
      expect(result.error).toBeDefined();
      expect(result.error?.message).toBe('Location permission denied');
    });

    it('falls back to network location when GPS fails', async () => {
      Platform.OS = 'ios';
      let callCount = 0;

      (Geolocation.getCurrentPosition as jest.Mock).mockImplementation((success, error, options) => {
        callCount++;
        if (callCount === 1 && options.enableHighAccuracy) {
          // GPS fails
          error({ code: 1, message: 'GPS timeout' });
        } else {
          // Network succeeds
          success({
            coords: { latitude: 33.5731, longitude: -7.5898, accuracy: 50 },
            timestamp: Date.now(),
          });
        }
      });

      const result = await locationService.getCurrentLocation();

      expect(result.source).toBe('network');
      expect(result.coordinates.latitude).toBe(33.5731);
    });
  });
});

import { databaseService } from '../DatabaseService';
import SQLite from 'react-native-sqlite-storage';

jest.mock('react-native-sqlite-storage');
jest.mock('@/services/logger');

describe('DatabaseService', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('initDatabase', () => {
    it('opens database successfully', async () => {
      const mockDb = {
        executeSql: jest.fn().mockResolvedValue([]),
      };
      (SQLite.openDatabase as jest.Mock).mockResolvedValue(mockDb);

      await databaseService.initDatabase();

      expect(SQLite.openDatabase).toHaveBeenCalledWith({
        name: 'quater.db',
        location: 'default',
      });
    });

    it('creates tables on initialization', async () => {
      const mockExecuteSql = jest.fn().mockResolvedValue([]);
      const mockDb = {
        executeSql: mockExecuteSql,
      };
      (SQLite.openDatabase as jest.Mock).mockResolvedValue(mockDb);

      await databaseService.initDatabase();

      expect(mockExecuteSql).toHaveBeenCalledWith(expect.stringContaining('CREATE TABLE IF NOT EXISTS samples'));
    });
  });

  describe('createSample', () => {
    it('creates sample with valid data', async () => {
      const mockDb = {
        executeSql: jest.fn().mockResolvedValue([]),
      };
      (databaseService as any).db = mockDb;

      const sampleDto = {
        type: 'DrinkingWater' as const,
        locationLatitude: 33.5731,
        locationLongitude: -7.5898,
        collectionDate: '2024-01-01T00:00:00Z',
        collectorName: 'John Doe',
      };

      const sample = await databaseService.createSample(sampleDto);

      expect(sample.id).toBeDefined();
      expect(sample.type).toBe('DrinkingWater');
      expect(sample.collectorName).toBe('John Doe');
      expect(mockDb.executeSql).toHaveBeenCalled();
    });

    it('throws error when database not initialized', async () => {
      (databaseService as any).db = null;

      const sampleDto = {
        type: 'DrinkingWater' as const,
        locationLatitude: 33.5731,
        locationLongitude: -7.5898,
        collectionDate: '2024-01-01T00:00:00Z',
        collectorName: 'John Doe',
      };

      await expect(databaseService.createSample(sampleDto)).rejects.toThrow('Database not initialized');
    });
  });

  describe('getSamples', () => {
    it('returns empty array when no samples', async () => {
      const mockDb = {
        executeSql: jest.fn().mockResolvedValue([{ rows: { length: 0, item: () => null } }]),
      };
      (databaseService as any).db = mockDb;

      const samples = await databaseService.getSamples();

      expect(samples).toEqual([]);
    });

    it('returns samples when they exist', async () => {
      const mockSample = {
        id: '1',
        type: 'DrinkingWater',
        locationLatitude: 33.5731,
        locationLongitude: -7.5898,
        collectionDate: '2024-01-01T00:00:00Z',
        collectorName: 'John Doe',
        status: 'Pending',
        isSynced: 0,
        createdDate: '2024-01-01T00:00:00Z',
        lastModified: '2024-01-01T00:00:00Z',
      };

      const mockDb = {
        executeSql: jest.fn().mockResolvedValue([{
          rows: {
            length: 1,
            item: (index: number) => mockSample,
          },
        }]),
      };
      (databaseService as any).db = mockDb;

      const samples = await databaseService.getSamples();

      expect(samples).toHaveLength(1);
      expect(samples[0].id).toBe('1');
      expect(samples[0].isSynced).toBe(false);
    });
  });
});

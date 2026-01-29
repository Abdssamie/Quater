import SQLite from 'react-native-sqlite-storage';
import { Sample, CreateSampleDto } from '../types/Sample';
import { logger } from './logger';

SQLite.enablePromise(true);

class DatabaseService {
  private db: SQLite.SQLiteDatabase | null = null;

  async initDatabase(): Promise<void> {
    try {
      this.db = await SQLite.openDatabase({
        name: 'quater.db',
        location: 'default',
      });

      logger.info('Database opened successfully');

      // Enable WAL mode for better concurrency
      await this.db.executeSql('PRAGMA journal_mode=WAL;');

      // Create samples table
      await this.db.executeSql(`
        CREATE TABLE IF NOT EXISTS samples (
          id TEXT PRIMARY KEY,
          type TEXT NOT NULL,
          locationLatitude REAL NOT NULL,
          locationLongitude REAL NOT NULL,
          locationDescription TEXT,
          locationHierarchy TEXT,
          collectionDate TEXT NOT NULL,
          collectorName TEXT NOT NULL,
          notes TEXT,
          status TEXT NOT NULL DEFAULT 'Pending',
          isSynced INTEGER NOT NULL DEFAULT 0,
          createdDate TEXT NOT NULL,
          lastModified TEXT NOT NULL
        );
      `);

      // Create indexes
      await this.db.executeSql(
        'CREATE INDEX IF NOT EXISTS idx_samples_lastModified ON samples(lastModified);'
      );
      await this.db.executeSql(
        'CREATE INDEX IF NOT EXISTS idx_samples_isSynced ON samples(isSynced);'
      );
      await this.db.executeSql(
        'CREATE INDEX IF NOT EXISTS idx_samples_status ON samples(status);'
      );

      logger.info('Database initialized successfully');
    } catch (error) {
      logger.error('Failed to initialize database', error);
      throw error;
    }
  }

  async createSample(dto: CreateSampleDto): Promise<Sample> {
    if (!this.db) {
      throw new Error('Database not initialized');
    }

    const now = new Date().toISOString();
    const id = this.generateUUID();

    const sample: Sample = {
      id,
      type: dto.type,
      locationLatitude: dto.locationLatitude,
      locationLongitude: dto.locationLongitude,
      locationDescription: dto.locationDescription,
      locationHierarchy: dto.locationHierarchy,
      collectionDate: dto.collectionDate,
      collectorName: dto.collectorName,
      notes: dto.notes,
      status: 'Pending',
      isSynced: false,
      createdDate: now,
      lastModified: now,
    };

    try {
      await this.db.executeSql(
        `INSERT INTO samples (
          id, type, locationLatitude, locationLongitude, locationDescription,
          locationHierarchy, collectionDate, collectorName, notes, status,
          isSynced, createdDate, lastModified
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`,
        [
          sample.id,
          sample.type,
          sample.locationLatitude,
          sample.locationLongitude,
          sample.locationDescription || null,
          sample.locationHierarchy || null,
          sample.collectionDate,
          sample.collectorName,
          sample.notes || null,
          sample.status,
          sample.isSynced ? 1 : 0,
          sample.createdDate,
          sample.lastModified,
        ]
      );

      logger.info('Sample created', { sampleId: sample.id });
      return sample;
    } catch (error) {
      logger.error('Failed to create sample', error);
      throw error;
    }
  }

  async getSamples(): Promise<Sample[]> {
    if (!this.db) {
      throw new Error('Database not initialized');
    }

    try {
      const [results] = await this.db.executeSql(
        'SELECT * FROM samples ORDER BY collectionDate DESC'
      );

      const samples: Sample[] = [];
      for (let i = 0; i < results.rows.length; i++) {
        const row = results.rows.item(i);
        samples.push(this.mapRowToSample(row));
      }

      return samples;
    } catch (error) {
      logger.error('Failed to get samples', error);
      throw error;
    }
  }

  async getSampleById(id: string): Promise<Sample | null> {
    if (!this.db) {
      throw new Error('Database not initialized');
    }

    try {
      const [results] = await this.db.executeSql(
        'SELECT * FROM samples WHERE id = ?',
        [id]
      );

      if (results.rows.length === 0) {
        return null;
      }

      return this.mapRowToSample(results.rows.item(0));
    } catch (error) {
      logger.error('Failed to get sample by id', error);
      throw error;
    }
  }

  async updateSample(id: string, updates: Partial<Sample>): Promise<void> {
    if (!this.db) {
      throw new Error('Database not initialized');
    }

    const now = new Date().toISOString();
    const fields: string[] = [];
    const values: any[] = [];

    Object.entries(updates).forEach(([key, value]) => {
      if (key !== 'id' && key !== 'createdDate') {
        fields.push(`${key} = ?`);
        values.push(value);
      }
    });

    fields.push('lastModified = ?');
    values.push(now);
    values.push(id);

    try {
      await this.db.executeSql(
        `UPDATE samples SET ${fields.join(', ')} WHERE id = ?`,
        values
      );

      logger.info('Sample updated', { sampleId: id });
    } catch (error) {
      logger.error('Failed to update sample', error);
      throw error;
    }
  }

  async closeDatabase(): Promise<void> {
    if (this.db) {
      await this.db.close();
      this.db = null;
      logger.info('Database closed');
    }
  }

  private mapRowToSample(row: any): Sample {
    return {
      id: row.id,
      type: row.type,
      locationLatitude: row.locationLatitude,
      locationLongitude: row.locationLongitude,
      locationDescription: row.locationDescription,
      locationHierarchy: row.locationHierarchy,
      collectionDate: row.collectionDate,
      collectorName: row.collectorName,
      notes: row.notes,
      status: row.status,
      isSynced: row.isSynced === 1,
      createdDate: row.createdDate,
      lastModified: row.lastModified,
    };
  }

  private generateUUID(): string {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
      const r = (Math.random() * 16) | 0;
      const v = c === 'x' ? r : (r & 0x3) | 0x8;
      return v.toString(16);
    });
  }
}

export const databaseService = new DatabaseService();

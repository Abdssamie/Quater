export type SampleType =
  | 'DrinkingWater'
  | 'Wastewater'
  | 'SurfaceWater'
  | 'Groundwater'
  | 'IndustrialWater';

export type SampleStatus = 'Pending' | 'Completed' | 'Archived';

export interface Sample {
  id: string;
  type: SampleType;
  locationLatitude: number;
  locationLongitude: number;
  locationDescription?: string;
  locationHierarchy?: string;
  collectionDate: string; // ISO 8601
  collectorName: string;
  notes?: string;
  status: SampleStatus;
  isSynced: boolean;
  createdDate: string;
  lastModified: string;
}

export interface CreateSampleDto {
  type: SampleType;
  locationLatitude: number;
  locationLongitude: number;
  locationDescription?: string;
  locationHierarchy?: string;
  collectionDate: string;
  collectorName: string;
  notes?: string;
}

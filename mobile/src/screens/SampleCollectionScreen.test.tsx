import React from 'react';
import { render, fireEvent, waitFor } from '@testing-library/react-native';
import { SampleCollectionScreen } from '../SampleCollectionScreen';
import { databaseService } from '@/services/DatabaseService';
import { locationService } from '@/services/LocationService';

// Mock navigation
const mockNavigate = jest.fn();
const mockGoBack = jest.fn();
jest.mock('@react-navigation/native', () => ({
  useNavigation: () => ({
    navigate: mockNavigate,
    goBack: mockGoBack,
  }),
}));

// Mock services
jest.mock('@/services/DatabaseService');
jest.mock('@/services/LocationService');
jest.mock('@/services/logger');

describe('SampleCollectionScreen', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders correctly', () => {
    const { getByText } = render(<SampleCollectionScreen />);
    expect(getByText('New Sample Collection')).toBeTruthy();
  });

  it('captures location when button is pressed', async () => {
    const mockLocation = {
      coordinates: { latitude: 33.5731, longitude: -7.5898 },
      source: 'gps',
    };
    (locationService.getCurrentLocation as jest.Mock).mockResolvedValue(mockLocation);

    const { getByText } = render(<SampleCollectionScreen />);
    const captureButton = getByText('Capture Location');
    
    fireEvent.press(captureButton);

    await waitFor(() => {
      expect(locationService.getCurrentLocation).toHaveBeenCalled();
    });
  });

  it('validates required fields before saving', async () => {
    const { getByText, getByPlaceholderText } = render(<SampleCollectionScreen />);
    
    const saveButton = getByText('Save Sample');
    fireEvent.press(saveButton);

    await waitFor(() => {
      expect(getByText('Collector name is required')).toBeTruthy();
    });
  });

  it('saves sample successfully', async () => {
    const mockSample = { id: '123', type: 'DrinkingWater' };
    (databaseService.createSample as jest.Mock).mockResolvedValue(mockSample);
    (locationService.validateCoordinates as jest.Mock).mockReturnValue(true);

    const { getByText, getByPlaceholderText } = render(<SampleCollectionScreen />);
    
    const collectorInput = getByPlaceholderText('Enter your name');
    fireEvent.changeText(collectorInput, 'John Doe');

    const saveButton = getByText('Save Sample');
    fireEvent.press(saveButton);

    await waitFor(() => {
      expect(databaseService.createSample).toHaveBeenCalled();
    });
  });
});

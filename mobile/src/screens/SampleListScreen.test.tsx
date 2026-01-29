import React from 'react';
import { render, fireEvent, waitFor } from '@testing-library/react-native';
import { SampleListScreen } from '../SampleListScreen';
import { databaseService } from '@/services/DatabaseService';

// Mock navigation
const mockNavigate = jest.fn();
jest.mock('@react-navigation/native', () => ({
  useNavigation: () => ({
    navigate: mockNavigate,
  }),
  useFocusEffect: (callback: () => void) => {
    callback();
  },
}));

// Mock services
jest.mock('@/services/DatabaseService');
jest.mock('@/services/logger');

describe('SampleListScreen', () => {
  const mockSamples = [
    {
      id: '1',
      type: 'DrinkingWater',
      locationLatitude: 33.5731,
      locationLongitude: -7.5898,
      locationDescription: 'Test Location',
      collectionDate: '2024-01-01T00:00:00Z',
      collectorName: 'John Doe',
      isSynced: false,
      status: 'Pending',
      createdDate: '2024-01-01T00:00:00Z',
      lastModified: '2024-01-01T00:00:00Z',
    },
  ];

  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders loading state initially', () => {
    (databaseService.getSamples as jest.Mock).mockImplementation(
      () => new Promise(() => {})
    );

    const { getByText } = render(<SampleListScreen />);
    expect(getByText('Loading samples...')).toBeTruthy();
  });

  it('renders sample list after loading', async () => {
    (databaseService.getSamples as jest.Mock).mockResolvedValue(mockSamples);

    const { getByText } = render(<SampleListScreen />);

    await waitFor(() => {
      expect(getByText('Drinking Water')).toBeTruthy();
      expect(getByText('Test Location')).toBeTruthy();
      expect(getByText('By: John Doe')).toBeTruthy();
    });
  });

  it('renders empty state when no samples', async () => {
    (databaseService.getSamples as jest.Mock).mockResolvedValue([]);

    const { getByText } = render(<SampleListScreen />);

    await waitFor(() => {
      expect(getByText('No Samples Yet')).toBeTruthy();
    });
  });

  it('navigates to sample details on press', async () => {
    (databaseService.getSamples as jest.Mock).mockResolvedValue(mockSamples);

    const { getByText } = render(<SampleListScreen />);

    await waitFor(() => {
      const sampleCard = getByText('Drinking Water');
      fireEvent.press(sampleCard.parent!);
    });

    expect(mockNavigate).toHaveBeenCalledWith('Sample', { id: '1' });
  });

  it('navigates to collection screen on FAB press', async () => {
    (databaseService.getSamples as jest.Mock).mockResolvedValue([]);

    const { getByText } = render(<SampleListScreen />);

    await waitFor(() => {
      const fab = getByText('+');
      fireEvent.press(fab);
    });

    expect(mockNavigate).toHaveBeenCalledWith('SampleCollection');
  });
});

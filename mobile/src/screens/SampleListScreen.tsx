import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  StyleSheet,
  FlatList,
  TouchableOpacity,
  RefreshControl,
} from 'react-native';
import { useNavigation, useFocusEffect } from '@react-navigation/native';
import type { StackNavigationProp } from '@react-navigation/stack';
import type { RootStackParamList } from '../types/navigation';
import { Sample } from '../types/Sample';
import { databaseService } from '../services/DatabaseService';
import { LoadingSpinner } from '../components/LoadingSpinner';
import { ErrorMessage } from '../components/ErrorMessage';
import { logger } from '../services/logger';

type NavigationProp = StackNavigationProp<RootStackParamList, 'SampleList'>;

export const SampleListScreen: React.FC = () => {
  const navigation = useNavigation<NavigationProp>();

  const [samples, setSamples] = useState<Sample[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [isRefreshing, setIsRefreshing] = useState<boolean>(false);
  const [error, setError] = useState<string>('');

  const loadSamples = async (isRefresh: boolean = false): Promise<void> => {
    if (isRefresh) {
      setIsRefreshing(true);
    } else {
      setIsLoading(true);
    }
    setError('');

    try {
      const loadedSamples = await databaseService.getSamples();
      setSamples(loadedSamples);
      logger.info('Samples loaded', { count: loadedSamples.length });
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to load samples';
      setError(errorMessage);
      logger.error('Failed to load samples', err);
    } finally {
      setIsLoading(false);
      setIsRefreshing(false);
    }
  };

  useFocusEffect(
    React.useCallback(() => {
      loadSamples();
    }, [])
  );

  const handleRefresh = (): void => {
    loadSamples(true);
  };

  const handleSamplePress = (sample: Sample): void => {
    navigation.navigate('Sample', { id: sample.id });
  };

  const handleAddSample = (): void => {
    navigation.navigate('SampleCollection');
  };

  const formatDate = (isoString: string): string => {
    const date = new Date(isoString);
    return date.toLocaleDateString() + ' ' + date.toLocaleTimeString();
  };

  const getSampleTypeLabel = (type: string): string => {
    switch (type) {
      case 'DrinkingWater':
        return 'Drinking Water';
      case 'Wastewater':
        return 'Wastewater';
      case 'SurfaceWater':
        return 'Surface Water';
      case 'Groundwater':
        return 'Groundwater';
      case 'IndustrialWater':
        return 'Industrial Water';
      default:
        return type;
    }
  };

  const renderSampleItem = ({ item }: { item: Sample }): JSX.Element => (
    <TouchableOpacity
      style={styles.sampleCard}
      onPress={() => handleSamplePress(item)}
    >
      <View style={styles.cardHeader}>
        <Text style={styles.sampleType}>{getSampleTypeLabel(item.type)}</Text>
        <View style={[styles.statusBadge, !item.isSynced && styles.unsyncedBadge]}>
          <Text style={styles.statusText}>
            {item.isSynced ? 'Synced' : 'Not Synced'}
          </Text>
        </View>
      </View>

      <Text style={styles.locationText}>
        {item.locationDescription || `${item.locationLatitude.toFixed(4)}, ${item.locationLongitude.toFixed(4)}`}
      </Text>

      <View style={styles.cardFooter}>
        <Text style={styles.dateText}>{formatDate(item.collectionDate)}</Text>
        <Text style={styles.collectorText}>By: {item.collectorName}</Text>
      </View>
    </TouchableOpacity>
  );

  const renderEmptyState = (): JSX.Element => (
    <View style={styles.emptyState}>
      <Text style={styles.emptyStateTitle}>No Samples Yet</Text>
      <Text style={styles.emptyStateText}>
        Tap the + button to collect your first sample
      </Text>
    </View>
  );

  if (isLoading) {
    return (
      <View style={styles.centerContainer}>
        <LoadingSpinner message="Loading samples..." />
      </View>
    );
  }

  return (
    <View style={styles.container}>
      {error && <ErrorMessage message={error} />}

      <FlatList
        data={samples}
        renderItem={renderSampleItem}
        keyExtractor={(item) => item.id}
        contentContainerStyle={samples.length === 0 ? styles.emptyContainer : styles.listContent}
        ListEmptyComponent={renderEmptyState}
        refreshControl={
          <RefreshControl refreshing={isRefreshing} onRefresh={handleRefresh} />
        }
      />

      <TouchableOpacity style={styles.fab} onPress={handleAddSample}>
        <Text style={styles.fabText}>+</Text>
      </TouchableOpacity>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#F5F5F5',
  },
  centerContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
  },
  listContent: {
    padding: 16,
  },
  emptyContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
  },
  sampleCard: {
    backgroundColor: '#FFF',
    borderRadius: 12,
    padding: 16,
    marginBottom: 12,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  cardHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 8,
  },
  sampleType: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#333',
  },
  statusBadge: {
    backgroundColor: '#4CAF50',
    paddingHorizontal: 8,
    paddingVertical: 4,
    borderRadius: 12,
  },
  unsyncedBadge: {
    backgroundColor: '#FF9800',
  },
  statusText: {
    color: '#FFF',
    fontSize: 12,
    fontWeight: '600',
  },
  locationText: {
    fontSize: 14,
    color: '#666',
    marginBottom: 8,
  },
  cardFooter: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  dateText: {
    fontSize: 12,
    color: '#999',
  },
  collectorText: {
    fontSize: 12,
    color: '#999',
  },
  emptyState: {
    alignItems: 'center',
    padding: 32,
  },
  emptyStateTitle: {
    fontSize: 20,
    fontWeight: '600',
    color: '#333',
    marginBottom: 8,
  },
  emptyStateText: {
    fontSize: 14,
    color: '#666',
    textAlign: 'center',
  },
  fab: {
    position: 'absolute',
    right: 16,
    bottom: 16,
    width: 56,
    height: 56,
    borderRadius: 28,
    backgroundColor: '#007AFF',
    justifyContent: 'center',
    alignItems: 'center',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.3,
    shadowRadius: 4,
    elevation: 8,
  },
  fabText: {
    fontSize: 32,
    color: '#FFF',
    fontWeight: '300',
  },
});

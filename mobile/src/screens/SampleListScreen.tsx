import React, { useState, useCallback, useMemo } from 'react';
import {
  View,
  Text,
  FlatList,
  TouchableOpacity,
  RefreshControl,
} from 'react-native';
import { useNavigation, useFocusEffect } from '@react-navigation/native';
import type { StackNavigationProp } from '@react-navigation/stack';
import type { RootStackParamList } from '@/types/navigation';
import type { Sample } from '@/types/Sample';
import { databaseService } from '@/services/DatabaseService';
import { LoadingSpinner } from '@/components/LoadingSpinner';
import { ErrorMessage } from '@/components/ErrorMessage';
import { logger } from '@/services/logger';
import { styles } from './SampleListScreen.styles';

type NavigationProp = StackNavigationProp<RootStackParamList, 'SampleList'>;

export const SampleListScreen = React.memo(() => {
  const navigation = useNavigation<NavigationProp>();

  const [samples, setSamples] = useState<Sample[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [isRefreshing, setIsRefreshing] = useState<boolean>(false);
  const [error, setError] = useState<string>('');

  const loadSamples = useCallback(async (isRefresh: boolean = false): Promise<void> => {
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
  }, []);

  useFocusEffect(
    useCallback(() => {
      loadSamples();
    }, [loadSamples])
  );

  const handleRefresh = useCallback((): void => {
    loadSamples(true);
  }, [loadSamples]);

  const handleSamplePress = useCallback((sample: Sample): void => {
    navigation.navigate('Sample', { id: sample.id });
  }, [navigation]);

  const handleAddSample = useCallback((): void => {
    navigation.navigate('SampleCollection');
  }, [navigation]);

  const formatDate = useCallback((isoString: string): string => {
    const date = new Date(isoString);
    return date.toLocaleDateString() + ' ' + date.toLocaleTimeString();
  }, []);

  const getSampleTypeLabel = useCallback((type: string): string => {
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
  }, []);

  const renderSampleItem = useCallback(({ item }: { item: Sample }): JSX.Element => (
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
  ), [handleSamplePress, getSampleTypeLabel, formatDate]);

  const renderEmptyState = useCallback((): JSX.Element => (
    <View style={styles.emptyState}>
      <Text style={styles.emptyStateTitle}>No Samples Yet</Text>
      <Text style={styles.emptyStateText}>
        Tap the + button to collect your first sample
      </Text>
    </View>
  ), []);

  const keyExtractor = useCallback((item: Sample) => item.id, []);

  const listContentStyle = useMemo(
    () => (samples.length === 0 ? styles.emptyContainer : styles.listContent),
    [samples.length]
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
        keyExtractor={keyExtractor}
        contentContainerStyle={listContentStyle}
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
});

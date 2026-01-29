import React, { useState, useCallback, useMemo } from 'react';
import {
  View,
  Text,
  TextInput,
  ScrollView,
  TouchableOpacity,
  Alert,
  KeyboardAvoidingView,
  Platform,
} from 'react-native';
import { useNavigation } from '@react-navigation/native';
import type { StackNavigationProp } from '@react-navigation/stack';
import type { RootStackParamList } from '@/types/navigation';
import type { SampleType } from '@/types/Sample';
import { databaseService } from '@/services/DatabaseService';
import { locationService } from '@/services/LocationService';
import { SampleTypePicker } from '@/components/SampleTypePicker';
import { LocationDisplay } from '@/components/LocationDisplay';
import { LoadingSpinner } from '@/components/LoadingSpinner';
import { ErrorMessage } from '@/components/ErrorMessage';
import { logger } from '@/services/logger';
import { styles } from './SampleCollectionScreen.styles';

type NavigationProp = StackNavigationProp<RootStackParamList, 'SampleCollection'>;

export const SampleCollectionScreen = React.memo(() => {
  const navigation = useNavigation<NavigationProp>();

  // Form state
  const [sampleType, setSampleType] = useState<SampleType>('DrinkingWater');
  const [latitude, setLatitude] = useState<number>(0);
  const [longitude, setLongitude] = useState<number>(0);
  const [locationSource, setLocationSource] = useState<string>('');
  const [locationDescription, setLocationDescription] = useState<string>('');
  const [locationHierarchy, setLocationHierarchy] = useState<string>('');
  const [collectorName, setCollectorName] = useState<string>('');
  const [notes, setNotes] = useState<string>('');

  // UI state
  const [isLoadingLocation, setIsLoadingLocation] = useState<boolean>(false);
  const [isSaving, setIsSaving] = useState<boolean>(false);
  const [error, setError] = useState<string>('');

  const handleCaptureLocation = useCallback(async (): Promise<void> => {
    setIsLoadingLocation(true);
    setError('');

    try {
      const result = await locationService.getCurrentLocation();

      if (result.error) {
        setError(result.error.message);
        // Allow manual entry
        setLocationSource('manual');
      } else {
        setLatitude(result.coordinates.latitude);
        setLongitude(result.coordinates.longitude);
        setLocationSource(result.source);
        logger.info('Location captured', {
          source: result.source,
          latitude: result.coordinates.latitude,
          longitude: result.coordinates.longitude,
        });
      }
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to capture location';
      setError(errorMessage);
      logger.error('Location capture failed', err);
    } finally {
      setIsLoadingLocation(false);
    }
  }, []);

  const validateForm = useCallback((): string | null => {
    if (!collectorName.trim()) {
      return 'Collector name is required';
    }

    if (collectorName.length > 100) {
      return 'Collector name must be 100 characters or less';
    }

    if (!locationService.validateCoordinates(latitude, longitude)) {
      return 'Invalid coordinates. Please capture location or enter valid coordinates.';
    }

    if (locationDescription && locationDescription.length > 200) {
      return 'Location description must be 200 characters or less';
    }

    if (locationHierarchy && locationHierarchy.length > 500) {
      return 'Location hierarchy must be 500 characters or less';
    }

    if (notes && notes.length > 1000) {
      return 'Notes must be 1000 characters or less';
    }

    return null;
  }, [collectorName, latitude, longitude, locationDescription, locationHierarchy, notes]);

  const handleSaveSample = useCallback(async (): Promise<void> => {
    setError('');

    // Validate form
    const validationError = validateForm();
    if (validationError) {
      setError(validationError);
      return;
    }

    setIsSaving(true);

    try {
      const sample = await databaseService.createSample({
        type: sampleType,
        locationLatitude: latitude,
        locationLongitude: longitude,
        locationDescription: locationDescription.trim() || undefined,
        locationHierarchy: locationHierarchy.trim() || undefined,
        collectionDate: new Date().toISOString(),
        collectorName: collectorName.trim(),
        notes: notes.trim() || undefined,
      });

      logger.info('Sample saved successfully', { sampleId: sample.id });

      Alert.alert(
        'Success',
        'Sample saved successfully',
        [
          {
            text: 'OK',
            onPress: () => navigation.navigate('SampleList'),
          },
        ]
      );
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to save sample';
      setError(errorMessage);
      logger.error('Failed to save sample', err);
    } finally {
      setIsSaving(false);
    }
  }, [validateForm, sampleType, latitude, longitude, locationDescription, locationHierarchy, collectorName, notes, navigation]);

  const handleCancel = useCallback((): void => {
    Alert.alert(
      'Cancel',
      'Are you sure you want to cancel? All data will be lost.',
      [
        { text: 'No', style: 'cancel' },
        {
          text: 'Yes',
          style: 'destructive',
          onPress: () => navigation.goBack(),
        },
      ]
    );
  }, [navigation]);

  const handleLatitudeChange = useCallback((text: string): void => {
    setLatitude(parseFloat(text) || 0);
  }, []);

  const handleLongitudeChange = useCallback((text: string): void => {
    setLongitude(parseFloat(text) || 0);
  }, []);

  const textAreaStyle = useMemo(() => [styles.input, styles.textArea], []);
  const cancelButtonStyle = useMemo(() => [styles.button, styles.cancelButton], []);
  const cancelButtonTextStyle = useMemo(() => [styles.buttonText, styles.cancelButtonText], []);
  const saveButtonStyle = useMemo(() => [styles.button, styles.saveButton], []);
  const showLocationDisplay = latitude !== 0 && longitude !== 0;
  const showManualLocation = locationSource === 'manual';
  const captureButtonText = latitude === 0 ? 'Capture Location' : 'Refresh Location';

  return (
    <KeyboardAvoidingView
      style={styles.container}
      behavior={Platform.OS === 'ios' ? 'padding' : undefined}
    >
      <ScrollView style={styles.scrollView} contentContainerStyle={styles.scrollContent}>
        <Text style={styles.title}>New Sample Collection</Text>

        {error && <ErrorMessage message={error} />}

        <SampleTypePicker value={sampleType} onValueChange={setSampleType} />

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Location</Text>

          {isLoadingLocation ? (
            <LoadingSpinner message="Capturing location..." />
          ) : (
            <>
              {showLocationDisplay && (
                <LocationDisplay
                  latitude={latitude}
                  longitude={longitude}
                  source={locationSource}
                />
              )}

              <TouchableOpacity
                style={styles.button}
                onPress={handleCaptureLocation}
                disabled={isLoadingLocation}
              >
                <Text style={styles.buttonText}>
                  {captureButtonText}
                </Text>
              </TouchableOpacity>

              {showManualLocation && (
                <View style={styles.manualLocationContainer}>
                  <Text style={styles.label}>Manual Coordinates</Text>
                  <View style={styles.row}>
                    <View style={styles.halfWidth}>
                      <Text style={styles.inputLabel}>Latitude</Text>
                      <TextInput
                        style={styles.input}
                        value={latitude.toString()}
                        onChangeText={handleLatitudeChange}
                        keyboardType="numeric"
                        placeholder="-90 to 90"
                      />
                    </View>
                    <View style={styles.halfWidth}>
                      <Text style={styles.inputLabel}>Longitude</Text>
                      <TextInput
                        style={styles.input}
                        value={longitude.toString()}
                        onChangeText={handleLongitudeChange}
                        keyboardType="numeric"
                        placeholder="-180 to 180"
                      />
                    </View>
                  </View>
                </View>
              )}
            </>
          )}
        </View>

        <View style={styles.inputGroup}>
          <Text style={styles.label}>Location Description</Text>
          <TextInput
            style={styles.input}
            value={locationDescription}
            onChangeText={setLocationDescription}
            placeholder="e.g., Municipal Well #3"
            maxLength={200}
          />
        </View>

        <View style={styles.inputGroup}>
          <Text style={styles.label}>Location Hierarchy</Text>
          <TextInput
            style={styles.input}
            value={locationHierarchy}
            onChangeText={setLocationHierarchy}
            placeholder="e.g., Morocco/Casablanca/District 5"
            maxLength={500}
          />
        </View>

        <View style={styles.inputGroup}>
          <Text style={styles.label}>Collector Name *</Text>
          <TextInput
            style={styles.input}
            value={collectorName}
            onChangeText={setCollectorName}
            placeholder="Enter your name"
            maxLength={100}
          />
        </View>

        <View style={styles.inputGroup}>
          <Text style={styles.label}>Notes</Text>
          <TextInput
            style={textAreaStyle}
            value={notes}
            onChangeText={setNotes}
            placeholder="Additional notes about this sample"
            multiline
            numberOfLines={4}
            maxLength={1000}
          />
        </View>

        <View style={styles.buttonContainer}>
          <TouchableOpacity
            style={cancelButtonStyle}
            onPress={handleCancel}
            disabled={isSaving}
          >
            <Text style={cancelButtonTextStyle}>Cancel</Text>
          </TouchableOpacity>

          <TouchableOpacity
            style={saveButtonStyle}
            onPress={handleSaveSample}
            disabled={isSaving}
          >
            {isSaving ? (
              <LoadingSpinner />
            ) : (
              <Text style={styles.buttonText}>Save Sample</Text>
            )}
          </TouchableOpacity>
        </View>
      </ScrollView>
    </KeyboardAvoidingView>
  );
});

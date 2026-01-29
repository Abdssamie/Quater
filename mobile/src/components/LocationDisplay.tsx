import React from 'react';
import { View, Text, StyleSheet } from 'react-native';

interface LocationDisplayProps {
  latitude: number;
  longitude: number;
  source?: string;
}

export const LocationDisplay: React.FC<LocationDisplayProps> = ({
  latitude,
  longitude,
  source,
}) => {
  return (
    <View style={styles.container}>
      <Text style={styles.label}>Location</Text>
      <View style={styles.coordsContainer}>
        <Text style={styles.coords}>
          Lat: {latitude.toFixed(6)}, Long: {longitude.toFixed(6)}
        </Text>
        {source && <Text style={styles.source}>Source: {source}</Text>}
      </View>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    marginVertical: 8,
  },
  label: {
    fontSize: 14,
    fontWeight: '600',
    color: '#333',
    marginBottom: 4,
  },
  coordsContainer: {
    backgroundColor: '#F5F5F5',
    padding: 12,
    borderRadius: 8,
    borderWidth: 1,
    borderColor: '#E0E0E0',
  },
  coords: {
    fontSize: 14,
    color: '#666',
    fontFamily: 'monospace',
  },
  source: {
    fontSize: 12,
    color: '#999',
    marginTop: 4,
  },
});

import React from 'react';
import { View, Text } from 'react-native';
import { styles } from './LocationDisplay.styles';

interface LocationDisplayProps {
  latitude: number;
  longitude: number;
  source?: string;
}

export const LocationDisplay = React.memo<LocationDisplayProps>(({
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
});

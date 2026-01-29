import React from 'react';
import { View, Text, ActivityIndicator } from 'react-native';
import { styles } from './LoadingSpinner.styles';

interface LoadingSpinnerProps {
  message?: string;
}

export const LoadingSpinner = React.memo<LoadingSpinnerProps>(({ message }) => {
  return (
    <View style={styles.container}>
      <ActivityIndicator size="large" color="#007AFF" />
      {message && <Text style={styles.message}>{message}</Text>}
    </View>
  );
});

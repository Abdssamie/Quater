import React from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { useRoute } from '@react-navigation/native';
import type { RouteProp } from '@react-navigation/native';
import type { RootStackParamList } from '../types/navigation';

type SampleScreenRouteProp = RouteProp<RootStackParamList, 'Sample'>;

export const SampleScreen: React.FC = () => {
  const route = useRoute<SampleScreenRouteProp>();
  const { id } = route.params;

  return (
    <View style={styles.container}>
      <Text style={styles.title}>Sample Details</Text>
      <Text style={styles.subtitle}>ID: {id}</Text>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: 20,
  },
  title: {
    fontSize: 24,
    fontWeight: 'bold',
    marginBottom: 10,
  },
  subtitle: {
    fontSize: 16,
    color: '#666',
  },
});

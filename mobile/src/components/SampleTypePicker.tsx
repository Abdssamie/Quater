import React from 'react';
import { View, Text, StyleSheet, Platform } from 'react-native';
import { Picker } from '@react-native-picker/picker';
import { SampleType } from '../types/Sample';

interface SampleTypePickerProps {
  value: SampleType;
  onValueChange: (value: SampleType) => void;
}

export const SampleTypePicker: React.FC<SampleTypePickerProps> = ({
  value,
  onValueChange,
}) => {
  return (
    <View style={styles.container}>
      <Text style={styles.label}>Sample Type *</Text>
      <View style={styles.pickerContainer}>
        <Picker
          selectedValue={value}
          onValueChange={onValueChange}
          style={styles.picker}
        >
          <Picker.Item label="Drinking Water" value="DrinkingWater" />
          <Picker.Item label="Wastewater" value="Wastewater" />
          <Picker.Item label="Surface Water" value="SurfaceWater" />
          <Picker.Item label="Groundwater" value="Groundwater" />
          <Picker.Item label="Industrial Water" value="IndustrialWater" />
        </Picker>
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
  pickerContainer: {
    borderWidth: 1,
    borderColor: '#E0E0E0',
    borderRadius: 8,
    backgroundColor: '#FFF',
    ...Platform.select({
      android: {
        overflow: 'hidden',
      },
    }),
  },
  picker: {
    height: 50,
  },
});

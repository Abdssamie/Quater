import React from 'react';
import { View, Text } from 'react-native';
import { Picker } from '@react-native-picker/picker';
import type { SampleType } from '@/types/Sample';
import { styles } from './SampleTypePicker.styles';

interface SampleTypePickerProps {
  value: SampleType;
  onValueChange: (value: SampleType) => void;
}

export const SampleTypePicker = React.memo<SampleTypePickerProps>(({
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
});

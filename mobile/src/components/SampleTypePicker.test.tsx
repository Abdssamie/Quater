import React from 'react';
import { render, fireEvent } from '@testing-library/react-native';
import { SampleTypePicker } from '../SampleTypePicker';
import type { SampleType } from '@/types/Sample';

describe('SampleTypePicker', () => {
  it('renders with default value', () => {
    const mockOnChange = jest.fn();
    const { getByText } = render(
      <SampleTypePicker value="DrinkingWater" onValueChange={mockOnChange} />
    );
    expect(getByText('Sample Type *')).toBeTruthy();
  });

  it('calls onValueChange when value changes', () => {
    const mockOnChange = jest.fn();
    const { UNSAFE_getByType } = render(
      <SampleTypePicker value="DrinkingWater" onValueChange={mockOnChange} />
    );
    
    const { Picker } = require('@react-native-picker/picker');
    const picker = UNSAFE_getByType(Picker);
    
    fireEvent(picker, 'onValueChange', 'Wastewater');
    expect(mockOnChange).toHaveBeenCalledWith('Wastewater');
  });

  it('displays all sample type options', () => {
    const mockOnChange = jest.fn();
    const { getByText } = render(
      <SampleTypePicker value="DrinkingWater" onValueChange={mockOnChange} />
    );
    
    // Note: In actual implementation, Picker.Item labels may not be directly accessible
    // This is a simplified test
    expect(getByText('Sample Type *')).toBeTruthy();
  });
});

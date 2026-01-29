import React from 'react';
import { render } from '@testing-library/react-native';
import { LocationDisplay } from '../LocationDisplay';

describe('LocationDisplay', () => {
  it('renders coordinates correctly', () => {
    const { getByText } = render(
      <LocationDisplay latitude={33.5731} longitude={-7.5898} />
    );
    expect(getByText(/Lat: 33.573100, Long: -7.589800/)).toBeTruthy();
  });

  it('renders source when provided', () => {
    const { getByText } = render(
      <LocationDisplay latitude={33.5731} longitude={-7.5898} source="gps" />
    );
    expect(getByText('Source: gps')).toBeTruthy();
  });

  it('does not render source when not provided', () => {
    const { queryByText } = render(
      <LocationDisplay latitude={33.5731} longitude={-7.5898} />
    );
    expect(queryByText(/Source:/)).toBeNull();
  });
});

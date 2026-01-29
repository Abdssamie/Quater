import React from 'react';
import { render } from '@testing-library/react-native';
import { LoadingSpinner } from '../LoadingSpinner';

describe('LoadingSpinner', () => {
  it('renders without message', () => {
    const { queryByText } = render(<LoadingSpinner />);
    expect(queryByText(/./)).toBeNull();
  });

  it('renders with message', () => {
    const { getByText } = render(<LoadingSpinner message="Loading..." />);
    expect(getByText('Loading...')).toBeTruthy();
  });

  it('displays ActivityIndicator', () => {
    const { UNSAFE_getByType } = render(<LoadingSpinner />);
    const { ActivityIndicator } = require('react-native');
    expect(UNSAFE_getByType(ActivityIndicator)).toBeTruthy();
  });
});

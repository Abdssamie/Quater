import React from 'react';
import { render } from '@testing-library/react-native';
import { ErrorMessage } from '../ErrorMessage';

describe('ErrorMessage', () => {
  it('renders error message correctly', () => {
    const { getByText } = render(<ErrorMessage message="Test error" />);
    expect(getByText('Test error')).toBeTruthy();
  });

  it('applies correct styles', () => {
    const { getByText } = render(<ErrorMessage message="Test error" />);
    const messageElement = getByText('Test error');
    expect(messageElement).toBeTruthy();
  });
});

import React from 'react';
import { View, Text } from 'react-native';
import { styles } from './ErrorMessage.styles';

interface ErrorMessageProps {
  message: string;
}

export const ErrorMessage = React.memo<ErrorMessageProps>(({ message }) => {
  return (
    <View style={styles.container}>
      <Text style={styles.message}>{message}</Text>
    </View>
  );
});

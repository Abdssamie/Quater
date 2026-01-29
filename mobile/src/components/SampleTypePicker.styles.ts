import { StyleSheet, Platform } from 'react-native';

export const styles = StyleSheet.create({
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

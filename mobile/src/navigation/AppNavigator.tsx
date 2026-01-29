import React, { useEffect } from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createStackNavigator } from '@react-navigation/stack';
import type { RootStackParamList } from '../types/navigation';
import { HomeScreen } from '../screens/HomeScreen';
import { SampleListScreen } from '../screens/SampleListScreen';
import { SampleCollectionScreen } from '../screens/SampleCollectionScreen';
import { SampleScreen } from '../screens/SampleScreen';
import { databaseService } from '../services/DatabaseService';
import { logger } from '../services/logger';

const Stack = createStackNavigator<RootStackParamList>();

export const AppNavigator: React.FC = () => {
  useEffect(() => {
    const initializeDatabase = async (): Promise<void> => {
      try {
        await databaseService.initDatabase();
        logger.info('Database initialized in AppNavigator');
      } catch (error) {
        logger.error('Failed to initialize database', error);
      }
    };

    initializeDatabase();

    return () => {
      databaseService.closeDatabase();
    };
  }, []);

  return (
    <NavigationContainer>
      <Stack.Navigator
        initialRouteName="SampleList"
        screenOptions={{
          headerStyle: {
            backgroundColor: '#007AFF',
          },
          headerTintColor: '#FFF',
          headerTitleStyle: {
            fontWeight: 'bold',
          },
        }}
      >
        <Stack.Screen
          name="SampleList"
          component={SampleListScreen}
          options={{ title: 'Samples' }}
        />
        <Stack.Screen
          name="SampleCollection"
          component={SampleCollectionScreen}
          options={{ title: 'New Sample' }}
        />
        <Stack.Screen
          name="Sample"
          component={SampleScreen}
          options={{ title: 'Sample Details' }}
        />
        <Stack.Screen
          name="Home"
          component={HomeScreen}
          options={{ title: 'Home' }}
        />
      </Stack.Navigator>
    </NavigationContainer>
  );
};

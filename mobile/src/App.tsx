import React from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { HomeScreen } from './screens/HomeScreen';
import { SampleScreen } from './screens/SampleScreen';
import type { RootStackParamList } from './types/navigation';

const Stack = createNativeStackNavigator<RootStackParamList>();

const App: React.FC = () => {
  return (
    <NavigationContainer>
      <Stack.Navigator initialRouteName="Home">
        <Stack.Screen name="Home" component={HomeScreen} options={{ title: 'Quater' }} />
        <Stack.Screen name="Sample" component={SampleScreen} options={{ title: 'New Sample' }} />
      </Stack.Navigator>
    </NavigationContainer>
  );
};

export default App;

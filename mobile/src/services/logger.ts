import { logger, consoleTransport } from 'react-native-logs';

const config = {
  transport: consoleTransport,
  severity: __DEV__ ? 'debug' : 'info',
  transportOptions: {
    colors: {
      info: 'blueBright',
      warn: 'yellowBright',
      error: 'redBright',
    },
  },
} as const;

const log = logger.createLogger(config);

export default log;

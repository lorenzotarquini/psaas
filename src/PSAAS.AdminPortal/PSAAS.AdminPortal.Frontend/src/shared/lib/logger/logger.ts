import pino from 'pino'

const rootLogger = pino({
  level: import.meta.env.DEV ? 'debug' : 'info',
  browser: { asObject: true },
})

// One named logger per module, like the static NLog Logger in the backend.
export const getLogger = (module: string) => rootLogger.child({ module })

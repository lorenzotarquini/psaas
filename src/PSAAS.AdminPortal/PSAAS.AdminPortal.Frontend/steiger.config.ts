import fsd from '@feature-sliced/steiger-plugin'
import { defineConfig } from 'steiger'

// Feature-Sliced Design rules: layer imports, public APIs, slice structure.
export default defineConfig([
  ...fsd.configs.recommended,
  {
    // shortcut: walking skeleton has one page, so every slice has one consumer; restore 'error' once pages multiply.
    rules: { 'fsd/insignificant-slice': 'warn' },
  },
])

import js from '@eslint/js';
import tseslint from 'typescript-eslint';
import pluginVue from 'eslint-plugin-vue';
import eslintConfigPrettier from 'eslint-config-prettier';
import globals from 'globals';

export default [
  {
    ignores: ['dist/**', 'node_modules/**', 'coverage/**']
  },
  js.configs.recommended,
  ...tseslint.configs.recommended,
  ...pluginVue.configs['flat/recommended'],
  {
    files: ['**/*.{ts,mts,tsx,vue}'],
    languageOptions: {
      ecmaVersion: 'latest',
      sourceType: 'module',
      globals: {
        ...globals.browser,
        ...globals.node
      },
      parserOptions: {
        parser: tseslint.parser
      }
    },
    rules: {
      'vue/multi-word-component-names': 'off',
      '@typescript-eslint/no-unused-vars': [
        'error',
        { argsIgnorePattern: '^_', varsIgnorePattern: '^_' }
      ]
    }
  },
  // Architectural boundary 1: shared/ cannot import from features/ or app/
  {
    files: ['src/shared/**/*.{ts,tsx,vue}'],
    rules: {
      'no-restricted-imports': [
        'error',
        {
          patterns: [
            {
              group: ['@/features/*', '@/features/**', '../features/*', '../../features/*', '@/app/*', '@/app/**', '../app/*', '../../app/*'],
              message: 'Shared layer (src/shared/) must not import from features/ or app/.'
            }
          ]
        }
      ]
    }
  },
  // Architectural boundary 2: features/ cannot cross-import other features
  {
    files: ['src/features/**/*.{ts,tsx,vue}'],
    rules: {
      'no-restricted-imports': [
        'error',
        {
          patterns: [
            {
              group: ['@/features/*', '@/features/**', '@/app/*', '@/app/**'],
              message:
                'Features must not cross-import other features or import from app/. Use relative imports within the same feature, shared contracts, or named routes.'
            }
          ]
        }
      ]
    }
  },
  eslintConfigPrettier
];

import Aura from '@primeuix/themes/aura';
import type { PrimeVueConfiguration } from 'primevue/config';

export const primeVueConfig: PrimeVueConfiguration = {
  theme: {
    preset: Aura,
    options: {
      darkModeSelector: '.dark-mode',
      cssLayer: {
        name: 'primevue',
        order: 'theme, base, primevue, utilities, custom'
      }
    }
  },
  pt: {
    button: {
      root: {
        class: 'font-medium transition-colors duration-150'
      }
    },
    dialog: {
      root: {
        class: 'rounded-xl shadow-xl border border-slate-200 overflow-hidden'
      },
      header: {
        class: 'px-6 py-4 border-b border-slate-200 bg-white'
      },
      content: {
        class: 'px-6 py-5 bg-white'
      },
      footer: {
        class: 'px-6 py-4 border-t border-slate-200 bg-slate-50 flex justify-end gap-2'
      }
    },
    datatable: {
      root: {
        class: 'border border-slate-200 rounded-lg overflow-hidden bg-white'
      },
      headerCell: {
        class: 'bg-slate-50 text-slate-600 text-xs font-semibold uppercase tracking-wider px-4 py-3 border-b border-slate-200'
      },
      bodyCell: {
        class: 'px-4 py-3 text-sm text-slate-700 border-b border-slate-100'
      }
    }
  }
};

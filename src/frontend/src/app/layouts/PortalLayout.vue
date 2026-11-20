<script setup lang="ts">
import { computed } from 'vue';
import { useRouter } from 'vue-router';
import { GraduationCap, LogOut, User as UserIcon } from 'lucide-vue-next';
import { useAuthStore } from '@/app/stores/auth.store';

const router = useRouter();
const authStore = useAuthStore();

const isParent = computed(() => authStore.roles.includes('PARENT'));
const isStudent = computed(() => authStore.roles.includes('STUDENT'));

const roleBadgeLabel = computed(() => {
  if (isParent.value) return 'Portal Orang Tua';
  if (isStudent.value) return 'Portal Siswa';
  return 'Portal';
});

const displayName = computed(
  () => authStore.user?.displayName ?? authStore.user?.username ?? 'Pengguna'
);

async function handleLogout(): Promise<void> {
  await authStore.logout();
  await router.push('/login');
}
</script>

<template>
  <div class="flex min-h-screen flex-col bg-slate-50 text-slate-900" data-testid="portal-layout">
    <!-- Header Bar -->
    <header
      class="sticky top-0 z-30 flex h-14 shrink-0 items-center justify-between border-b border-slate-200 bg-white px-4 shadow-xs sm:px-6 lg:px-8"
    >
      <!-- Left: Application Brand Logo, Title & Role Badge -->
      <div class="flex items-center gap-3">
        <div
          class="flex h-9 w-9 items-center justify-center rounded-lg bg-brand-primary-900 text-white shadow-xs"
        >
          <GraduationCap class="h-5 w-5" />
        </div>
        <div class="flex items-center gap-2">
          <span class="text-base font-bold tracking-tight text-slate-900">ELSASA-SIS</span>
          <!-- Role Badge Chip -->
          <span
            data-testid="portal-role-badge"
            class="inline-flex items-center rounded-full bg-brand-accent-50 px-2.5 py-0.5 text-xs font-semibold text-brand-accent-700 ring-1 ring-inset ring-brand-accent-600/20"
          >
            {{ roleBadgeLabel }}
          </span>
        </div>
      </div>

      <!-- Right: User Display Name & Sign-Out Button -->
      <div class="flex items-center gap-3 sm:gap-4">
        <div class="flex items-center gap-2">
          <span
            class="flex h-7 w-7 items-center justify-center rounded-full bg-slate-100 text-slate-600"
          >
            <UserIcon class="h-4 w-4" />
          </span>
          <span
            data-testid="portal-user-name"
            class="hidden max-w-[160px] truncate text-sm font-medium text-slate-700 sm:inline"
          >
            {{ displayName }}
          </span>
        </div>

        <button
          type="button"
          data-testid="portal-logout-button"
          class="inline-flex items-center gap-1.5 rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-xs font-medium text-slate-700 transition-colors hover:bg-slate-50 hover:text-rose-600 focus:outline-none focus:ring-2 focus:ring-brand-accent-500"
          title="Sign Out"
          @click="handleLogout"
        >
          <LogOut class="h-3.5 w-3.5" />
          <span>Sign Out</span>
        </button>
      </div>
    </header>

    <!-- Content Container -->
    <main class="mx-auto w-full max-w-5xl flex-1 px-4 py-6 sm:px-6 lg:px-8">
      <slot>
        <router-view />
      </slot>
    </main>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, watch, type Component } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useQueryClient } from '@tanstack/vue-query';
import {
  AlertTriangle,
  BookOpenCheck,
  CalendarDays,
  ChevronDown,
  GraduationCap,
  LogOut,
  Megaphone,
  Menu,
  PanelLeftClose,
  PanelLeftOpen,
  User as UserIcon,
  UserPlus,
  Users
} from 'lucide-vue-next';
import { useAuthStore } from '@/app/stores/auth.store';
import { useAppContextStore } from '@/app/stores/appContext.store';
import { useUIStore } from '@/app/stores/ui.store';
import { useAcademicYearsQuery } from '@/features/operations/calendar/api/useAcademicYearQueries';

interface ContextOption {
  key: string;
  label: string;
  academicYearId: string;
  periodId: string;
  academicYearName: string;
  periodName: string;
  isActive: boolean;
}

interface NavItem {
  label: string;
  to: { name: string };
  icon: Component;
  permissions: string[];
}

interface NavGroup {
  group: string;
  items: NavItem[];
}

const route = useRoute();
const router = useRouter();
const queryClient = useQueryClient();
const authStore = useAuthStore();
const appContextStore = useAppContextStore();
const uiStore = useUIStore();

const isProfileMenuOpen = ref(false);

const canLoadAcademicYears = computed(
  () =>
    authStore.isAuthenticated &&
    (authStore.hasPermission('Permissions.Operations.Calendar.View') ||
      authStore.hasPermission('Permissions.Operations.Calendar.Manage'))
);

const academicYearsQuery = useAcademicYearsQuery(
  { page: 1, pageSize: 100 },
  { enabled: canLoadAcademicYears }
);

const contextOptions = computed<ContextOption[]>(() => {
  const items = academicYearsQuery.data.value?.items ?? [];
  const options: ContextOption[] = [];

  for (const year of items) {
    if (!year.id) continue;
    const yearName = year.name ?? 'Unnamed Year';
    for (const period of year.periods ?? []) {
      if (!period.id) continue;
      const periodName = period.name ?? 'Unnamed Period';
      options.push({
        key: `${year.id}:${period.id}`,
        label: `${yearName} • ${periodName}`,
        academicYearId: year.id,
        periodId: period.id,
        academicYearName: yearName,
        periodName,
        isActive: year.status === 'ACTIVE' && period.status === 'ACTIVE'
      });
    }
  }

  return options;
});

watch(
  () => academicYearsQuery.data.value,
  (response) => {
    const items = response?.items ?? [];
    const activeYear = items.find((year) => year.status === 'ACTIVE') ?? null;
    const activePeriod = activeYear?.periods?.find((period) => period.status === 'ACTIVE') ?? null;

    const prevActiveYearId = appContextStore.activeAcademicYearId;
    const prevActivePeriodId = appContextStore.activePeriodId;
    const wasFollowingActive =
      !appContextStore.selectedPeriodId || appContextStore.selectedPeriodId === prevActivePeriodId;

    appContextStore.setActiveContext(activeYear?.id ?? null, activePeriod?.id ?? null, {
      academicYearName: activeYear?.name ?? null,
      periodName: activePeriod?.name ?? null
    });

    const activeChanged =
      (activeYear?.id ?? null) !== prevActiveYearId ||
      (activePeriod?.id ?? null) !== prevActivePeriodId;

    if (activeChanged && wasFollowingActive) {
      appContextStore.resetToActiveContext();
    }
  },
  { immediate: true }
);

const selectedContextKey = computed<string>({
  get: () => {
    const yearId = appContextStore.selectedAcademicYearId ?? appContextStore.activeAcademicYearId;
    const periodId = appContextStore.selectedPeriodId ?? appContextStore.activePeriodId;
    return yearId && periodId ? `${yearId}:${periodId}` : '';
  },
  set: (key) => {
    const matched = contextOptions.value.find((option) => option.key === key);
    if (!matched) return;
    appContextStore.selectContext(matched.academicYearId, matched.periodId, {
      academicYearName: matched.academicYearName,
      periodName: matched.periodName
    });
  }
});

const navGroups: NavGroup[] = [
  {
    group: 'Admissions',
    items: [
      {
        label: 'Admissions Queue',
        to: { name: 'admissions.queue' },
        icon: UserPlus,
        permissions: [
          'Permissions.Students.Admissions.View',
          'Permissions.Students.Admissions.Register',
          'Permissions.Students.Admissions.Decide',
          'Permissions.Students.Admissions.Approve',
          'Permissions.Students.Admissions.Enroll'
        ]
      }
    ]
  },
  {
    group: 'Students & Classes',
    items: [
      {
        label: 'Student Directory & Placements',
        to: { name: 'students.directory' },
        icon: Users,
        permissions: [
          'Permissions.Students.Directory.View',
          'Permissions.Students.Directory.Manage',
          'Permissions.Students.Roster.ViewOwn'
        ]
      }
    ]
  },
  {
    group: 'Faculty & Teaching',
    items: [
      {
        label: 'Teachers & Assignments',
        to: { name: 'teachers.assignments' },
        icon: BookOpenCheck,
        permissions: [
          'Permissions.Teaching.Assignments.View',
          'Permissions.Teaching.Assignments.Manage',
          'Permissions.Teaching.Assignments.Oversight',
          'Permissions.Teaching.Directory.Manage'
        ]
      }
    ]
  },
  {
    group: 'Operations & Comms',
    items: [
      {
        label: 'School Calendar & Academic Cycle',
        to: { name: 'operations.calendar' },
        icon: CalendarDays,
        permissions: [
          'Permissions.Operations.Calendar.View',
          'Permissions.Operations.Calendar.Manage'
        ]
      },
      {
        label: 'Announcements & Bulletin',
        to: { name: 'operations.announcements' },
        icon: Megaphone,
        permissions: [
          'Permissions.Operations.Announcements.View',
          'Permissions.Operations.Announcements.Create',
          'Permissions.Operations.Announcements.Publish'
        ]
      }
    ]
  }
];

const visibleNavGroups = computed(() =>
  navGroups
    .map((group) => ({
      ...group,
      items: group.items.filter((item) =>
        item.permissions.some((permission) => authStore.hasPermission(permission))
      )
    }))
    .filter((group) => group.items.length > 0)
);

const primaryRole = computed(() => authStore.roles[0] ?? 'USER');
const displayName = computed(
  () => authStore.user?.displayName ?? authStore.user?.username ?? 'Account'
);

async function handleLogout(): Promise<void> {
  isProfileMenuOpen.value = false;
  await authStore.logout();
  appContextStore.clearContext();
  queryClient.clear();
  await router.push({ name: 'auth.login' });
}
</script>

<template>
  <div class="flex min-h-screen flex-col bg-slate-50 text-slate-900">
    <!-- Top Bar (56px / h-14) -->
    <header
      class="sticky top-0 z-30 flex h-14 shrink-0 items-center justify-between gap-4 bg-brand-primary-900 px-4 text-white shadow-sm"
    >
      <div class="flex items-center gap-3">
        <button
          type="button"
          class="inline-flex h-9 w-9 items-center justify-center rounded-lg text-slate-200 transition-colors hover:bg-brand-primary-800 hover:text-white focus:outline-none focus:ring-2 focus:ring-brand-accent-500"
          :aria-label="uiStore.isSidebarCollapsed ? 'Expand sidebar' : 'Collapse sidebar'"
          @click="uiStore.toggleSidebar()"
        >
          <PanelLeftOpen v-if="uiStore.isSidebarCollapsed" class="h-5 w-5" />
          <PanelLeftClose v-else class="hidden h-5 w-5 md:block" />
          <Menu class="h-5 w-5 md:hidden" />
        </button>

        <router-link
          to="/"
          class="flex items-center gap-2 text-sm font-semibold tracking-tight text-white"
        >
          <GraduationCap class="h-5 w-5 text-brand-accent-500" />
          <span>Elsasa SIS</span>
        </router-link>
      </div>

      <!-- Academic Year & Semester Switcher -->
      <div class="hidden items-center gap-2 sm:flex">
        <slot name="context-switcher">
          <div
            class="flex items-center gap-2 rounded-lg border border-slate-700 bg-brand-primary-800 px-3 py-1.5 text-xs"
            data-testid="top-bar-context-switcher"
          >
            <CalendarDays class="h-3.5 w-3.5 shrink-0 text-brand-accent-500" />
            <select
              v-if="contextOptions.length > 0"
              v-model="selectedContextKey"
              aria-label="Academic Year and Period"
              data-testid="context-switcher-select"
              class="cursor-pointer bg-transparent font-medium text-slate-100 focus:outline-none"
            >
              <option
                v-for="option in contextOptions"
                :key="option.key"
                :value="option.key"
                class="bg-white text-slate-900"
              >
                {{ option.label }}{{ option.isActive ? ' (Active)' : '' }}
              </option>
            </select>
            <span v-else class="font-medium text-slate-100">{{
              appContextStore.currentContextLabel
            }}</span>
            <span
              v-if="appContextStore.isHistoricalPeriod"
              class="rounded bg-amber-500/20 px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-amber-300"
            >
              Historical
            </span>
            <span
              v-else-if="appContextStore.activePeriodId"
              class="rounded bg-teal-500/20 px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-teal-300"
            >
              Active
            </span>
          </div>
        </slot>
      </div>

      <!-- Role Badge & User Profile Menu -->
      <div class="relative flex items-center gap-2.5">
        <span
          class="hidden rounded-md bg-brand-primary-800 px-2 py-1 text-[11px] font-semibold uppercase tracking-wider text-teal-300 sm:inline-block"
        >
          {{ primaryRole }}
        </span>

        <button
          type="button"
          class="inline-flex items-center gap-2 rounded-lg px-2.5 py-1.5 text-sm font-medium text-slate-100 transition-colors hover:bg-brand-primary-800 focus:outline-none focus:ring-2 focus:ring-brand-accent-500"
          aria-haspopup="menu"
          :aria-expanded="isProfileMenuOpen"
          data-testid="user-menu-trigger"
          @click="isProfileMenuOpen = !isProfileMenuOpen"
        >
          <span
            class="flex h-7 w-7 items-center justify-center rounded-full bg-brand-accent-600 text-xs font-semibold text-white"
          >
            <UserIcon class="h-4 w-4" />
          </span>
          <span class="hidden max-w-[140px] truncate md:inline">{{ displayName }}</span>
          <ChevronDown class="h-4 w-4 text-slate-400" />
        </button>

        <div
          v-if="isProfileMenuOpen"
          class="absolute right-0 top-12 z-40 w-56 rounded-xl border border-slate-200 bg-white py-1.5 text-slate-800 shadow-lg"
          role="menu"
        >
          <div class="border-b border-slate-100 px-4 py-2">
            <p class="truncate text-sm font-semibold text-slate-900">{{ displayName }}</p>
            <p class="truncate text-xs text-slate-500">{{ primaryRole }}</p>
          </div>
          <button
            type="button"
            class="flex w-full items-center gap-2 px-4 py-2 text-left text-sm text-rose-600 transition-colors hover:bg-rose-50"
            role="menuitem"
            data-testid="logout-button"
            @click="handleLogout"
          >
            <LogOut class="h-4 w-4" />
            <span>Sign Out</span>
          </button>
        </div>
      </div>
    </header>

    <!-- Historical Period Warning Banner -->
    <div
      v-if="appContextStore.isHistoricalPeriod"
      class="flex items-center justify-between gap-3 border-b border-amber-300 bg-amber-50 px-4 py-2.5 text-sm text-amber-900"
      role="alert"
      data-testid="historical-period-banner"
    >
      <div class="flex items-center gap-2">
        <AlertTriangle class="h-4 w-4 shrink-0 text-amber-600" />
        <p class="font-medium">
          Viewing Historical Period: {{ appContextStore.currentContextLabel }}. Operational edits
          are locked in Read-Only Mode.
        </p>
      </div>
      <button
        type="button"
        class="shrink-0 rounded-md border border-amber-400 bg-white px-2.5 py-1 text-xs font-semibold text-amber-900 transition-colors hover:bg-amber-100"
        @click="appContextStore.resetToActiveContext()"
      >
        Return to Active
      </button>
    </div>

    <!-- Main Shell Body -->
    <div class="flex flex-1">
      <!-- Collapsible Role-Filtered Sidebar -->
      <aside
        :class="[
          'shrink-0 border-r border-slate-200 bg-white transition-all duration-200',
          uiStore.isSidebarCollapsed ? 'w-16' : 'w-64'
        ]"
        aria-label="Primary Navigation"
      >
        <nav class="flex flex-col gap-5 p-3">
          <div v-for="group in visibleNavGroups" :key="group.group" class="space-y-1.5">
            <p
              v-if="!uiStore.isSidebarCollapsed"
              class="px-2.5 text-[11px] font-semibold uppercase tracking-wider text-slate-400"
            >
              {{ group.group }}
            </p>
            <ul class="space-y-1">
              <li v-for="item in group.items" :key="item.label">
                <router-link
                  :to="item.to"
                  class="flex items-center gap-2.5 rounded-lg px-2.5 py-2 text-sm font-medium transition-colors"
                  :class="
                    route.name === item.to.name
                      ? 'bg-brand-accent-50 text-brand-accent-600'
                      : 'text-slate-700 hover:bg-slate-100 hover:text-slate-900'
                  "
                  :title="uiStore.isSidebarCollapsed ? item.label : undefined"
                  :data-testid="`nav-item-${item.to.name}`"
                >
                  <component :is="item.icon" class="h-4 w-4 shrink-0" />
                  <span v-if="!uiStore.isSidebarCollapsed" class="truncate">
                    {{ item.label }}
                  </span>
                </router-link>
              </li>
            </ul>
          </div>
        </nav>
      </aside>

      <!-- Main Content Region -->
      <main class="min-w-0 flex-1 p-4 lg:p-6">
        <nav
          v-if="uiStore.breadcrumbs.length > 0"
          aria-label="Breadcrumb"
          class="mb-4 flex items-center gap-1.5 text-xs text-slate-500"
        >
          <template v-for="(crumb, index) in uiStore.breadcrumbs" :key="crumb.label">
            <span v-if="index > 0" aria-hidden="true">/</span>
            <router-link v-if="crumb.to" :to="crumb.to" class="hover:text-slate-800">
              {{ crumb.label }}
            </router-link>
            <span v-else class="font-medium text-slate-800">{{ crumb.label }}</span>
          </template>
        </nav>

        <slot>
          <router-view />
        </slot>
      </main>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import { useRoute } from 'vue-router';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import {
  Award,
  Calendar,
  HeartPulse,
  Plus,
  RefreshCw,
  Search,
  ShieldAlert,
  ShieldBan,
  ThumbsDown,
  ThumbsUp,
  Trophy,
  User,
  Users,
  X
} from 'lucide-vue-next';
import AppErrorBoundary from '@/shared/components/feedback/AppErrorBoundary.vue';
import EmptyState from '@/shared/components/feedback/EmptyState.vue';
import StatusPill, { type StatusIntent } from '@/shared/components/feedback/StatusPill.vue';
import { useAppContextBridge } from '@/shared/services/appContextBridge';
import { useCurrentSession } from '@/shared/services/authSession';
import type {
  DossierTimelineItemDto,
  StudentListItemDto
} from '../api/pastoral.api';
import {
  useDevelopmentOverviewQuery,
  usePastoralStudentSearchQuery,
  useStudentDossierQuery
} from '../api/usePastoralQueries';
import type { PastoralStudentTarget } from '../schemas/pastoral.schema';
import AchievementAwardModal from '../components/AchievementAwardModal.vue';
import BehaviourNoteComposerModal from '../components/BehaviourNoteComposerModal.vue';
import DisciplineIncidentModal from '../components/DisciplineIncidentModal.vue';
import EscalatedSanctionModal from '../components/EscalatedSanctionModal.vue';

type TimelineCategoryFilter =
  | 'ALL'
  | 'BEHAVIOUR'
  | 'DISCIPLINE'
  | 'ACHIEVEMENT'
  | 'EXTRACURRICULAR';

const PERMISSIONS = {
  BEHAVIOUR_RECORD: 'Permissions.Development.Behaviour.Record',
  DISCIPLINE_RECORD: 'Permissions.Development.Discipline.Record',
  DISCIPLINE_ESCALATE: 'Permissions.Development.Discipline.Escalate',
  ACHIEVEMENTS_RECORD: 'Permissions.Development.Achievements.Record',
  DOSSIER_VIEW: 'Permissions.Development.Dossier.View'
} as const;

const route = useRoute();
const { hasPermission } = useCurrentSession();
const { activeAcademicYearId, activeAcademicYearName } = useAppContextBridge();

const canRecordBehaviour = computed(() => hasPermission(PERMISSIONS.BEHAVIOUR_RECORD));
const canRecordDiscipline = computed(() => hasPermission(PERMISSIONS.DISCIPLINE_RECORD));
const canEscalateDiscipline = computed(() => hasPermission(PERMISSIONS.DISCIPLINE_ESCALATE));
const canRecordAchievement = computed(() => hasPermission(PERMISSIONS.ACHIEVEMENTS_RECORD));
const canViewDossier = computed(() => hasPermission(PERMISSIONS.DOSSIER_VIEW));

// ---------------------------------------------------------------------------
// Student Search & Autocomplete
// ---------------------------------------------------------------------------
const searchInput = ref('');
const debouncedSearch = ref('');
const isAutocompleteOpen = ref(false);
const highlightedIndex = ref(-1);
const searchContainerRef = ref<HTMLElement | null>(null);
let searchDebounceTimer: ReturnType<typeof setTimeout> | null = null;

watch(searchInput, (value) => {
  if (searchDebounceTimer) {
    clearTimeout(searchDebounceTimer);
  }
  searchDebounceTimer = setTimeout(() => {
    debouncedSearch.value = value;
  }, 200);
});

const studentSearchQuery = usePastoralStudentSearchQuery(debouncedSearch, {
  enabled: () => isAutocompleteOpen.value || searchInput.value.trim().length > 0
});

const studentSuggestions = computed(() => studentSearchQuery.data.value ?? []);

const selectedStudentId = ref<string | null>(
  typeof route.query.studentId === 'string' && route.query.studentId.trim().length > 0
    ? route.query.studentId.trim()
    : null
);
const selectedStudentMeta = ref<PastoralStudentTarget | null>(null);

function selectStudent(student: StudentListItemDto): void {
  if (!student.id) return;
  selectedStudentId.value = student.id;
  selectedStudentMeta.value = {
    id: student.id,
    fullName: student.fullName ?? 'Unknown Student',
    nis: student.nis ?? '-',
    academicYearId: student.academicYearId ?? activeAcademicYearId.value,
    academicYearName: student.academicYearName ?? activeAcademicYearName.value,
    schoolLevel: student.schoolLevel ?? null,
    grade: student.grade ?? null,
    studyGroupName: student.studyGroupName ?? null
  };
  searchInput.value = '';
  debouncedSearch.value = '';
  isAutocompleteOpen.value = false;
  highlightedIndex.value = -1;
}

function clearSelectedStudent(): void {
  selectedStudentId.value = null;
  selectedStudentMeta.value = null;
  activeCategory.value = 'ALL';
}

function handleSearchFocus(): void {
  isAutocompleteOpen.value = true;
}

function handleSearchKeydown(event: KeyboardEvent): void {
  const items = studentSuggestions.value;
  if (!isAutocompleteOpen.value && (event.key === 'ArrowDown' || event.key === 'Enter')) {
    isAutocompleteOpen.value = true;
  }

  if (event.key === 'Escape') {
    isAutocompleteOpen.value = false;
    highlightedIndex.value = -1;
    return;
  }

  if (items.length === 0) return;

  if (event.key === 'ArrowDown') {
    event.preventDefault();
    highlightedIndex.value = (highlightedIndex.value + 1) % items.length;
  } else if (event.key === 'ArrowUp') {
    event.preventDefault();
    highlightedIndex.value = (highlightedIndex.value - 1 + items.length) % items.length;
  } else if (event.key === 'Enter' && highlightedIndex.value >= 0) {
    event.preventDefault();
    const chosen = items[highlightedIndex.value];
    if (chosen) {
      selectStudent(chosen);
    }
  }
}

function handleDocumentClick(event: MouseEvent): void {
  if (searchContainerRef.value && !searchContainerRef.value.contains(event.target as Node)) {
    isAutocompleteOpen.value = false;
  }
}

onMounted(() => {
  document.addEventListener('click', handleDocumentClick);
});

onBeforeUnmount(() => {
  document.removeEventListener('click', handleDocumentClick);
  if (searchDebounceTimer) {
    clearTimeout(searchDebounceTimer);
  }
});

// ---------------------------------------------------------------------------
// Dossier & Overview Queries
// ---------------------------------------------------------------------------
const dossierQuery = useStudentDossierQuery(selectedStudentId, {
  enabled: () => Boolean(selectedStudentId.value) && canViewDossier.value
});

const overviewQuery = useDevelopmentOverviewQuery({
  enabled: () => !selectedStudentId.value
});

const dossier = computed(() => dossierQuery.data.value ?? null);

const activeStudentTarget = computed<PastoralStudentTarget | null>(() => {
  if (!selectedStudentId.value) return null;
  if (selectedStudentMeta.value && selectedStudentMeta.value.id === selectedStudentId.value) {
    return selectedStudentMeta.value;
  }
  if (dossier.value?.studentId) {
    return {
      id: dossier.value.studentId,
      fullName: dossier.value.studentName || 'Selected Student',
      nis: dossier.value.studentNis || '-',
      academicYearId: activeAcademicYearId.value,
      academicYearName: activeAcademicYearName.value
    };
  }
  return null;
});

// ---------------------------------------------------------------------------
// Metric Cards
// ---------------------------------------------------------------------------
const positiveObservationsCount = computed(
  () =>
    (dossier.value?.behaviourNotes ?? []).filter((n) => n.behaviourType === 'POSITIVE').length
);

const negativeObservationsCount = computed(
  () =>
    (dossier.value?.behaviourNotes ?? []).filter((n) => n.behaviourType === 'NEGATIVE').length
);

const disciplinaryActionsCount = computed(() => (dossier.value?.disciplineRecords ?? []).length);

const awardsCount = computed(() => (dossier.value?.achievements ?? []).length);

const activeClubsCount = computed(
  () => (dossier.value?.extracurriculars ?? []).filter((e) => e.status === 'ACTIVE').length
);

// ---------------------------------------------------------------------------
// 360° Chronological Timeline & Category Filter Pills
// ---------------------------------------------------------------------------
const activeCategory = ref<TimelineCategoryFilter>('ALL');

const timelineItems = computed(() => dossier.value?.timeline ?? []);

const categoryPills = computed<
  Array<{ key: TimelineCategoryFilter; label: string; count: number }>
>(() => {
  const items = timelineItems.value;
  return [
    { key: 'ALL', label: 'All', count: items.length },
    {
      key: 'BEHAVIOUR',
      label: 'Behaviour',
      count: items.filter((i) => i.category === 'BEHAVIOUR').length
    },
    {
      key: 'DISCIPLINE',
      label: 'Discipline',
      count: items.filter((i) => i.category === 'DISCIPLINE').length
    },
    {
      key: 'ACHIEVEMENT',
      label: 'Awards',
      count: items.filter((i) => i.category === 'ACHIEVEMENT').length
    },
    {
      key: 'EXTRACURRICULAR',
      label: 'Clubs',
      count: items.filter((i) => i.category === 'EXTRACURRICULAR').length
    }
  ];
});

const filteredTimeline = computed(() => {
  if (activeCategory.value === 'ALL') {
    return timelineItems.value;
  }
  return timelineItems.value.filter((item) => item.category === activeCategory.value);
});

function formatTimelineDate(dateStr?: string): string {
  if (!dateStr) return '-';
  const parsed = new Date(`${dateStr}T00:00:00`);
  if (Number.isNaN(parsed.getTime())) return dateStr;
  return parsed.toLocaleDateString('en-GB', {
    day: '2-digit',
    month: 'short',
    year: 'numeric'
  });
}

function getTimelineCategoryMeta(item: DossierTimelineItemDto) {
  switch (item.category) {
    case 'BEHAVIOUR': {
      const isPositive = item.badge === 'POSITIVE';
      return {
        icon: isPositive ? ThumbsUp : ThumbsDown,
        badgeClass: isPositive
          ? 'bg-emerald-50 text-emerald-600 border-emerald-200'
          : 'bg-rose-50 text-rose-600 border-rose-200',
        label: 'Behaviour'
      };
    }
    case 'DISCIPLINE':
      return {
        icon: ShieldAlert,
        badgeClass: 'bg-amber-50 text-amber-600 border-amber-200',
        label: 'Discipline'
      };
    case 'ACHIEVEMENT':
      return {
        icon: Trophy,
        badgeClass: 'bg-sky-50 text-sky-600 border-sky-200',
        label: 'Award'
      };
    case 'EXTRACURRICULAR':
      return {
        icon: Award,
        badgeClass: 'bg-indigo-50 text-indigo-600 border-indigo-200',
        label: 'Club'
      };
    default:
      return {
        icon: HeartPulse,
        badgeClass: 'bg-slate-100 text-slate-600 border-slate-200',
        label: 'Event'
      };
  }
}

function getTimelineBadgeIntent(item: DossierTimelineItemDto): StatusIntent | undefined {
  if (item.category === 'ACHIEVEMENT') return 'info';
  return undefined;
}

// ---------------------------------------------------------------------------
// Recording Modals State
// ---------------------------------------------------------------------------
const behaviourModalOpen = ref(false);
const disciplineModalOpen = ref(false);
const escalatedModalOpen = ref(false);
const achievementModalOpen = ref(false);
</script>

<template>
  <AppErrorBoundary>
    <div class="space-y-6" data-testid="pastoral-dossier-view">
      <!-- Screen Header & Student Search Autocomplete -->
      <div
        class="flex flex-col gap-4 rounded-xl border border-slate-200 bg-white p-5 lg:flex-row lg:items-center lg:justify-between"
      >
        <div class="space-y-1">
          <h1 class="text-xl font-semibold tracking-tight text-slate-900">
            Pastoral Care &amp; 360° Student Dossier
          </h1>
          <p class="text-sm text-slate-600">
            Track holistic student conduct, disciplinary records, achievements, and club engagement.
          </p>
        </div>

        <!-- Student Search Bar with Quick Autocomplete -->
        <div ref="searchContainerRef" class="relative w-full lg:w-96">
          <label for="pastoral-student-search" class="sr-only">Search student by name or NIS</label>
          <div class="relative">
            <Search
              class="pointer-events-none absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2 text-slate-400"
            />
            <InputText
              id="pastoral-student-search"
              v-model="searchInput"
              class="w-full pl-9 pr-8"
              placeholder="Search student by name or NIS..."
              autocomplete="off"
              data-testid="student-search-input"
              @focus="handleSearchFocus"
              @keydown="handleSearchKeydown"
            />
            <button
              v-if="searchInput"
              type="button"
              aria-label="Clear search text"
              class="absolute top-1/2 right-2.5 -translate-y-1/2 text-slate-400 hover:text-slate-600"
              @click="searchInput = ''"
            >
              <X class="h-4 w-4" />
            </button>
          </div>

          <!-- Autocomplete Dropdown -->
          <div
            v-if="isAutocompleteOpen"
            role="listbox"
            aria-label="Student search suggestions"
            data-testid="student-autocomplete-list"
            class="absolute z-30 mt-1.5 max-h-72 w-full overflow-y-auto rounded-xl border border-slate-200 bg-white py-1.5 shadow-lg"
          >
            <div
              v-if="studentSearchQuery.isFetching.value"
              class="px-3.5 py-2.5 text-xs text-slate-500"
            >
              Searching active students...
            </div>
            <div
              v-else-if="studentSuggestions.length === 0"
              class="px-3.5 py-2.5 text-xs text-slate-500"
            >
              No matching active students found.
            </div>
            <button
              v-for="(student, index) in studentSuggestions"
              v-else
              :key="student.id"
              type="button"
              role="option"
              :aria-selected="selectedStudentId === student.id"
              data-testid="student-autocomplete-option"
              class="flex w-full items-center justify-between gap-3 px-3.5 py-2 text-left text-sm transition-colors hover:bg-slate-50"
              :class="highlightedIndex === index ? 'bg-slate-100' : ''"
              @click="selectStudent(student)"
            >
              <div class="min-w-0">
                <div class="truncate font-medium text-slate-900">{{ student.fullName }}</div>
                <div class="text-xs text-slate-500">NIS: {{ student.nis || '-' }}</div>
              </div>
              <span
                v-if="student.studyGroupName"
                class="shrink-0 rounded-md border border-slate-200 bg-slate-50 px-2 py-0.5 text-xs font-medium text-slate-600"
              >
                {{ student.studyGroupName }}
              </span>
            </button>
          </div>
        </div>
      </div>

      <!-- Selected Student Dossier Workspace -->
      <template v-if="selectedStudentId">
        <!-- Student Header & Role-Based Recording Action Bar -->
        <div
          class="flex flex-col gap-4 rounded-xl border border-slate-200 bg-white p-5 lg:flex-row lg:items-center lg:justify-between"
          data-testid="selected-student-header"
        >
          <div class="flex items-center gap-3.5">
            <div
              class="flex h-12 w-12 shrink-0 items-center justify-center rounded-full bg-slate-100 text-slate-600"
            >
              <User class="h-6 w-6" />
            </div>
            <div>
              <div class="flex flex-wrap items-center gap-2">
                <h2 class="text-lg font-semibold text-slate-900" data-testid="dossier-student-name">
                  {{ activeStudentTarget?.fullName || 'Loading student...' }}
                </h2>
                <span
                  v-if="activeStudentTarget?.studyGroupName"
                  class="rounded-full border border-slate-200 bg-slate-50 px-2.5 py-0.5 text-xs font-medium text-slate-700"
                >
                  Class {{ activeStudentTarget.studyGroupName }}
                </span>
              </div>
              <div class="mt-0.5 flex flex-wrap items-center gap-x-3 text-xs text-slate-500">
                <span data-testid="dossier-student-nis">
                  NIS: <strong class="font-medium text-slate-700">{{ activeStudentTarget?.nis || '-' }}</strong>
                </span>
                <span v-if="activeStudentTarget?.schoolLevel || activeStudentTarget?.grade">
                  {{ activeStudentTarget.schoolLevel }} Grade {{ activeStudentTarget.grade }}
                </span>
                <button
                  type="button"
                  class="font-medium text-brand-accent-600 hover:underline"
                  @click="clearSelectedStudent"
                >
                  Switch student
                </button>
              </div>
            </div>
          </div>

          <!-- Action Buttons Based on Role Permissions -->
          <div class="flex flex-wrap items-center gap-2">
            <Button
              v-if="canRecordBehaviour"
              type="button"
              label="Behaviour Note"
              size="small"
              outlined
              :disabled="!activeStudentTarget"
              data-testid="btn-record-behaviour"
              @click="behaviourModalOpen = true"
            >
              <template #icon>
                <Plus class="mr-1.5 h-3.5 w-3.5" />
              </template>
            </Button>

            <Button
              v-if="canRecordDiscipline"
              type="button"
              label="Discipline Incident"
              severity="warn"
              size="small"
              outlined
              :disabled="!activeStudentTarget"
              data-testid="btn-record-discipline"
              @click="disciplineModalOpen = true"
            >
              <template #icon>
                <ShieldAlert class="mr-1.5 h-3.5 w-3.5" />
              </template>
            </Button>

            <Button
              v-if="canEscalateDiscipline"
              type="button"
              label="Escalate Sanction"
              severity="danger"
              size="small"
              :disabled="!activeStudentTarget"
              data-testid="btn-escalate-sanction"
              @click="escalatedModalOpen = true"
            >
              <template #icon>
                <ShieldBan class="mr-1.5 h-3.5 w-3.5" />
              </template>
            </Button>

            <Button
              v-if="canRecordAchievement"
              type="button"
              label="Record Award"
              severity="success"
              size="small"
              outlined
              :disabled="!activeStudentTarget"
              data-testid="btn-record-award"
              @click="achievementModalOpen = true"
            >
              <template #icon>
                <Trophy class="mr-1.5 h-3.5 w-3.5" />
              </template>
            </Button>
          </div>
        </div>

        <!-- Permission Guard -->
        <EmptyState
          v-if="!canViewDossier"
          variant="no-permission"
          title="Dossier View Restricted"
          description="You can record observations using the action buttons above, but viewing the complete historical dossier requires dossier view permission."
        />

        <!-- Dossier Loading State -->
        <div
          v-else-if="dossierQuery.isLoading.value"
          class="rounded-xl border border-slate-200 bg-white p-8 text-center text-sm text-slate-500"
          data-testid="dossier-loading"
        >
          Loading 360° student development dossier...
        </div>

        <!-- Dossier Error State -->
        <EmptyState
          v-else-if="dossierQuery.isError.value"
          variant="error"
          title="Unable to Load Student Dossier"
          description="There was a problem retrieving the pastoral dossier for this student."
        >
          <Button
            type="button"
            label="Retry"
            size="small"
            outlined
            @click="dossierQuery.refetch()"
          >
            <template #icon>
              <RefreshCw class="mr-1.5 h-3.5 w-3.5" />
            </template>
          </Button>
        </EmptyState>

        <!-- Loaded Metric Cards & 360° Timeline -->
        <template v-else-if="dossier">
          <!-- 5 Metric Cards -->
          <div class="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-5">
            <div
              class="rounded-xl border border-slate-200 bg-white p-4"
              data-testid="metric-positive"
            >
              <div class="flex items-center justify-between text-xs font-medium text-slate-500">
                <span>Positive Observations</span>
                <ThumbsUp class="h-4 w-4 text-emerald-600" />
              </div>
              <div class="mt-2 text-2xl font-bold text-emerald-700">
                {{ positiveObservationsCount }}
              </div>
            </div>

            <div
              class="rounded-xl border border-slate-200 bg-white p-4"
              data-testid="metric-negative"
            >
              <div class="flex items-center justify-between text-xs font-medium text-slate-500">
                <span>Negative Observations</span>
                <ThumbsDown class="h-4 w-4 text-rose-600" />
              </div>
              <div class="mt-2 text-2xl font-bold text-rose-700">
                {{ negativeObservationsCount }}
              </div>
            </div>

            <div
              class="rounded-xl border border-slate-200 bg-white p-4"
              data-testid="metric-discipline"
            >
              <div class="flex items-center justify-between text-xs font-medium text-slate-500">
                <span>Disciplinary Actions</span>
                <ShieldAlert class="h-4 w-4 text-amber-600" />
              </div>
              <div class="mt-2 text-2xl font-bold text-amber-700">
                {{ disciplinaryActionsCount }}
              </div>
            </div>

            <div
              class="rounded-xl border border-slate-200 bg-white p-4"
              data-testid="metric-awards"
            >
              <div class="flex items-center justify-between text-xs font-medium text-slate-500">
                <span>Awards</span>
                <Trophy class="h-4 w-4 text-sky-600" />
              </div>
              <div class="mt-2 text-2xl font-bold text-sky-700">
                {{ awardsCount }}
              </div>
            </div>

            <div
              class="col-span-2 rounded-xl border border-slate-200 bg-white p-4 sm:col-span-1"
              data-testid="metric-clubs"
            >
              <div class="flex items-center justify-between text-xs font-medium text-slate-500">
                <span>Active Clubs</span>
                <Users class="h-4 w-4 text-indigo-600" />
              </div>
              <div class="mt-2 text-2xl font-bold text-indigo-700">
                {{ activeClubsCount }}
              </div>
            </div>
          </div>

          <!-- 360° Chronological Timeline -->
          <div class="rounded-xl border border-slate-200 bg-white">
            <div
              class="flex flex-col gap-3 border-b border-slate-200 px-5 py-4 sm:flex-row sm:items-center sm:justify-between"
            >
              <div>
                <h3 class="text-base font-semibold text-slate-900">
                  360° Development Timeline
                </h3>
                <p class="text-xs text-slate-500">
                  Unified chronological log of pastoral observations, discipline, awards, and clubs
                </p>
              </div>

              <!-- Category Filter Pills -->
              <div class="flex flex-wrap items-center gap-1.5" role="group" aria-label="Filter timeline by category">
                <button
                  v-for="pill in categoryPills"
                  :key="pill.key"
                  type="button"
                  :aria-pressed="activeCategory === pill.key"
                  :data-testid="`filter-pill-${pill.key}`"
                  class="inline-flex items-center gap-1.5 rounded-full border px-3 py-1 text-xs font-medium transition-colors"
                  :class="
                    activeCategory === pill.key
                      ? 'border-slate-900 bg-slate-900 text-white'
                      : 'border-slate-200 bg-slate-50 text-slate-600 hover:bg-slate-100'
                  "
                  @click="activeCategory = pill.key"
                >
                  <span>{{ pill.label }}</span>
                  <span
                    class="rounded-full px-1.5 py-0.2 text-[11px]"
                    :class="
                      activeCategory === pill.key
                        ? 'bg-white/20 text-white'
                        : 'bg-white text-slate-600'
                    "
                  >
                    {{ pill.count }}
                  </span>
                </button>
              </div>
            </div>

            <div class="p-5" data-testid="dossier-timeline">
              <EmptyState
                v-if="filteredTimeline.length === 0"
                title="No Events Recorded"
                :description="
                  activeCategory === 'ALL'
                    ? 'This student does not have any pastoral, discipline, award, or extracurricular entries yet.'
                    : 'No records match the selected category filter.'
                "
              />

              <ol v-else class="relative space-y-5 border-l border-slate-200 pl-6">
                <li
                  v-for="item in filteredTimeline"
                  :key="`${item.category}-${item.id}`"
                  data-testid="timeline-item"
                  class="relative"
                >
                  <span
                    class="absolute -left-[35px] flex h-6 w-6 items-center justify-center rounded-full border bg-white"
                    :class="getTimelineCategoryMeta(item).badgeClass"
                  >
                    <component :is="getTimelineCategoryMeta(item).icon" class="h-3.5 w-3.5" />
                  </span>

                  <div class="rounded-lg border border-slate-200 bg-slate-50/60 p-4">
                    <div class="flex flex-wrap items-start justify-between gap-2">
                      <div class="space-y-1">
                        <div class="flex flex-wrap items-center gap-2">
                          <span class="text-sm font-semibold text-slate-900">
                            {{ item.title }}
                          </span>
                          <StatusPill
                            v-if="item.badge"
                            :status="item.badge"
                            :intent="getTimelineBadgeIntent(item)"
                          />
                        </div>
                        <div class="flex flex-wrap items-center gap-x-3 text-xs text-slate-500">
                          <span class="inline-flex items-center gap-1">
                            <Calendar class="h-3.5 w-3.5" />
                            {{ formatTimelineDate(item.date) }}
                          </span>
                          <span v-if="item.recordedBy">
                            Recorded by <strong class="font-medium text-slate-700">{{ item.recordedBy }}</strong>
                          </span>
                        </div>
                      </div>

                      <span
                        class="rounded-md border border-slate-200 bg-white px-2 py-0.5 text-[11px] font-medium text-slate-600"
                      >
                        {{ getTimelineCategoryMeta(item).label }}
                      </span>
                    </div>

                    <p
                      v-if="item.description"
                      class="mt-2.5 text-sm leading-relaxed text-slate-700"
                    >
                      {{ item.description }}
                    </p>
                  </div>
                </li>
              </ol>
            </div>
          </div>
        </template>
      </template>

      <!-- Initial State: No Student Selected Yet -->
      <template v-else>
        <EmptyState
          title="Select a Student to View 360° Pastoral Dossier"
          description="Use the student search bar above to find a student by full name or NIS, inspect their development timeline, and record behaviour, discipline, or award entries."
        />

        <!-- School-Wide Development Overview Summary -->
        <div
          v-if="overviewQuery.data.value"
          class="space-y-3"
          data-testid="development-overview-panel"
        >
          <h2 class="text-sm font-semibold tracking-wide text-slate-500 uppercase">
            School-Wide Pastoral Pulse
          </h2>
          <div class="grid grid-cols-2 gap-3 sm:grid-cols-4">
            <div class="rounded-xl border border-slate-200 bg-white p-4">
              <div class="text-xs font-medium text-slate-500">Total Behaviour Notes</div>
              <div class="mt-1.5 text-2xl font-bold text-slate-900">
                {{ overviewQuery.data.value.totalBehaviourNotes ?? 0 }}
              </div>
              <div class="mt-1 text-xs text-slate-500">
                {{ overviewQuery.data.value.positiveBehaviourNotes ?? 0 }} positive ·
                {{ overviewQuery.data.value.negativeBehaviourNotes ?? 0 }} negative
              </div>
            </div>

            <div class="rounded-xl border border-slate-200 bg-white p-4">
              <div class="text-xs font-medium text-slate-500">Total Discipline Incidents</div>
              <div class="mt-1.5 text-2xl font-bold text-slate-900">
                {{ overviewQuery.data.value.totalDisciplineIncidents ?? 0 }}
              </div>
              <div class="mt-1 text-xs text-slate-500">
                {{ (overviewQuery.data.value.parentMeetings ?? 0) + (overviewQuery.data.value.suspensions ?? 0) }}
                escalated sanctions
              </div>
            </div>

            <div class="rounded-xl border border-slate-200 bg-white p-4">
              <div class="text-xs font-medium text-slate-500">Student Awards</div>
              <div class="mt-1.5 text-2xl font-bold text-slate-900">
                {{ overviewQuery.data.value.totalAchievements ?? 0 }}
              </div>
              <div class="mt-1 text-xs text-slate-500">Academic &amp; non-academic honors</div>
            </div>

            <div class="rounded-xl border border-slate-200 bg-white p-4">
              <div class="text-xs font-medium text-slate-500">Active Clubs</div>
              <div class="mt-1.5 text-2xl font-bold text-slate-900">
                {{ overviewQuery.data.value.activeExtracurricularActivities ?? 0 }}
              </div>
              <div class="mt-1 text-xs text-slate-500">
                {{ overviewQuery.data.value.totalExtracurricularParticipants ?? 0 }} total members
              </div>
            </div>
          </div>
        </div>
      </template>

      <!-- Recording Modals -->
      <BehaviourNoteComposerModal
        v-model:visible="behaviourModalOpen"
        :student="activeStudentTarget"
      />

      <DisciplineIncidentModal
        v-model:visible="disciplineModalOpen"
        :student="activeStudentTarget"
      />

      <EscalatedSanctionModal
        v-model:visible="escalatedModalOpen"
        :student="activeStudentTarget"
      />

      <AchievementAwardModal
        v-model:visible="achievementModalOpen"
        :student="activeStudentTarget"
      />
    </div>
  </AppErrorBoundary>
</template>

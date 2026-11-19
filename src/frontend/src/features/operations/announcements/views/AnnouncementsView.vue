<script setup lang="ts">
import { computed, ref } from 'vue';
import Button from 'primevue/button';
import Column from 'primevue/column';
import InputText from 'primevue/inputtext';
import Select from 'primevue/select';
import type { DataTablePageEvent } from 'primevue/datatable';
import { Eye, Megaphone, Pencil, Plus, Search, Send, Users } from 'lucide-vue-next';
import AppErrorBoundary from '@/shared/components/feedback/AppErrorBoundary.vue';
import ConfirmActionDialog from '@/shared/components/feedback/ConfirmActionDialog.vue';
import StatusPill from '@/shared/components/feedback/StatusPill.vue';
import AppDataTable from '@/shared/components/tables/AppDataTable.vue';
import { formatDisplayDateTime } from '@/shared/utils/dateUtils';
import { useCurrentSession } from '@/shared/services/authSession';
import { useAppContextBridge } from '@/shared/services/appContextBridge';
import type { AnnouncementDto } from '../api/announcements.api';
import { usePublishAnnouncementMutation } from '../api/useAnnouncementMutations';
import { useAnnouncementsQuery } from '../api/useAnnouncementQueries';
import AnnouncementDetailDialog from '../components/AnnouncementDetailDialog.vue';
import AnnouncementFormDrawer from '../components/AnnouncementFormDrawer.vue';

const session = useCurrentSession();
const { selectedAcademicYearId } = useAppContextBridge();

const canCreate = computed(() =>
  session.hasPermission('Permissions.Operations.Announcements.Create')
);
const canPublish = computed(() =>
  session.hasPermission('Permissions.Operations.Announcements.Publish')
);
const canManage = computed(() => canCreate.value || canPublish.value);

const page = ref(1);
const pageSize = ref(10);
const search = ref('');
const selectedAudience = ref('');
const selectedStatus = ref('');

const AUDIENCE_FILTERS = [
  { label: 'All Audiences', value: '' },
  { label: 'School-Wide', value: 'ALL' },
  { label: 'Teachers', value: 'TEACHERS' },
  { label: 'Students', value: 'STUDENTS' },
  { label: 'Parents', value: 'PARENTS' }
];

const STATUS_OPTIONS = [
  { label: 'All Statuses', value: '' },
  { label: 'Drafts', value: 'DRAFT' },
  { label: 'Published', value: 'PUBLISHED' }
];

const queryParams = computed(() => ({
  page: page.value,
  pageSize: pageSize.value,
  search: search.value.trim() || undefined,
  targetAudience: selectedAudience.value || undefined,
  status: canManage.value ? selectedStatus.value || undefined : undefined,
  academicYearId: selectedAcademicYearId.value || undefined
}));

const { data, isLoading, isError, refetch } = useAnnouncementsQuery(queryParams);

const announcements = computed(() => data.value?.items ?? []);
const totalRecords = computed(() => data.value?.totalCount ?? 0);
const firstRecordIndex = computed(() => (page.value - 1) * pageSize.value);

const publishMutation = usePublishAnnouncementMutation();

// Drawer & Dialog state
const isDrawerVisible = ref(false);
const editingAnnouncement = ref<AnnouncementDto | null>(null);

const isDetailVisible = ref(false);
const selectedAnnouncement = ref<AnnouncementDto | null>(null);

const isPublishConfirmVisible = ref(false);
const announcementToPublish = ref<AnnouncementDto | null>(null);

function handlePageChange(event: DataTablePageEvent): void {
  page.value = event.page + 1;
  pageSize.value = event.rows;
}

function handleSearchInput(value: string | undefined): void {
  search.value = value ?? '';
  page.value = 1;
}

function selectAudience(audience: string): void {
  selectedAudience.value = audience;
  page.value = 1;
}

function handleStatusChange(): void {
  page.value = 1;
}

function openCreateDrawer(): void {
  editingAnnouncement.value = null;
  isDrawerVisible.value = true;
}

function openEditDrawer(announcement: AnnouncementDto): void {
  editingAnnouncement.value = announcement;
  isDrawerVisible.value = true;
}

function openDetailDialog(announcement: AnnouncementDto): void {
  selectedAnnouncement.value = announcement;
  isDetailVisible.value = true;
}

function promptPublish(announcement: AnnouncementDto): void {
  announcementToPublish.value = announcement;
  isPublishConfirmVisible.value = true;
}

const publishConfirmMessage = computed(() => {
  const title = announcementToPublish.value?.title ?? 'this announcement';
  return `Are you sure you want to publish "${title}"? Once published, it will become immediately visible on the bulletin to its target audience.`;
});

async function handleConfirmPublish(): Promise<void> {
  const target = announcementToPublish.value;
  if (!target?.id) return;
  try {
    await publishMutation.mutateAsync(target.id);
    isPublishConfirmVisible.value = false;
    announcementToPublish.value = null;
    if (selectedAnnouncement.value?.id === target.id) {
      isDetailVisible.value = false;
    }
  } catch {
    // Handled by mutation onError toast
  }
}

function formatAudience(raw?: string | null): string {
  switch (raw?.toUpperCase()) {
    case 'ALL':
      return 'All (School-Wide)';
    case 'TEACHERS':
      return 'Teachers & Staff';
    case 'STUDENTS':
      return 'Students';
    case 'PARENTS':
      return 'Parents';
    default:
      return raw ?? 'All';
  }
}
</script>

<template>
  <AppErrorBoundary>
    <div class="space-y-6">
      <!-- Header -->
      <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div class="space-y-1">
          <div class="flex items-center gap-2">
            <Megaphone class="h-5 w-5 text-brand-accent-600" />
            <h1
              class="text-xl font-semibold tracking-tight text-slate-900"
              data-testid="announcements-header-title"
            >
              School Announcements &amp; Bulletin
            </h1>
          </div>
          <p class="text-sm text-slate-600">
            Author, publish, and browse school-wide announcements targeted to staff, students, and
            parents.
          </p>
        </div>

        <div v-if="canCreate" class="flex items-center gap-2">
          <Button
            type="button"
            label="New Announcement"
            data-testid="create-announcement-button"
            @click="openCreateDrawer"
          >
            <template #icon>
              <Plus class="mr-1.5 h-4 w-4" />
            </template>
          </Button>
        </div>
      </div>

      <!-- Filters & Search Toolbar -->
      <div class="space-y-3 rounded-lg border border-slate-200 bg-white p-4 shadow-xs">
        <!-- Top Toolbar: Search + Status Filter (Status shown only for authors/admins) -->
        <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <div class="relative w-full max-w-md">
            <Search class="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
            <InputText
              :model-value="search"
              placeholder="Search announcements by title or content..."
              class="w-full pl-9 text-sm"
              data-testid="announcement-search-input"
              @update:model-value="handleSearchInput"
            />
          </div>

          <div
            v-if="canManage"
            class="flex items-center gap-2"
            data-testid="status-filter-container"
          >
            <span class="text-xs font-medium text-slate-500 whitespace-nowrap">Status:</span>
            <Select
              v-model="selectedStatus"
              :options="STATUS_OPTIONS"
              option-label="label"
              option-value="value"
              placeholder="All Statuses"
              class="w-40 text-sm"
              data-testid="status-filter-select"
              @change="handleStatusChange"
            />
          </div>
        </div>

        <!-- Audience Filter Pills -->
        <div
          class="flex flex-wrap items-center gap-2 border-t border-slate-100 pt-3"
          data-testid="audience-filter-pills"
        >
          <span class="text-xs font-medium text-slate-500 mr-1 flex items-center gap-1">
            <Users class="h-3.5 w-3.5 text-slate-400" />
            Audience:
          </span>
          <button
            v-for="pill in AUDIENCE_FILTERS"
            :key="pill.value"
            type="button"
            class="inline-flex items-center rounded-full px-3 py-1 text-xs font-medium transition-colors"
            :class="
              selectedAudience === pill.value
                ? 'bg-brand-accent-600 text-white shadow-xs'
                : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
            "
            :data-testid="`audience-pill-${pill.value || 'all'}`"
            @click="selectAudience(pill.value)"
          >
            {{ pill.label }}
          </button>
        </div>
      </div>

      <!-- Announcements Table -->
      <div v-if="isError" class="rounded-lg border border-rose-200 bg-rose-50 p-6 text-center">
        <p class="text-sm font-medium text-rose-800">Failed to load announcements.</p>
        <Button
          type="button"
          label="Retry"
          severity="danger"
          text
          class="mt-2"
          @click="() => refetch()"
        />
      </div>

      <AppDataTable
        v-else
        :value="announcements"
        :loading="isLoading"
        paginator
        lazy
        :rows="pageSize"
        :first="firstRecordIndex"
        :total-records="totalRecords"
        empty-title="No announcements found"
        empty-description="There are no announcements matching your filter criteria."
        data-testid="announcements-table"
        @page="handlePageChange"
      >
        <Column header="Announcement" style="min-width: 280px">
          <template #body="{ data: item }">
            <div class="space-y-1">
              <button
                type="button"
                class="text-left font-medium text-slate-900 hover:text-brand-accent-600 transition-colors"
                data-testid="announcement-title-link"
                @click="openDetailDialog(item)"
              >
                {{ item.title }}
              </button>
              <p class="line-clamp-2 text-xs text-slate-500 whitespace-pre-line">
                {{ item.content }}
              </p>
            </div>
          </template>
        </Column>

        <Column header="Audience" style="width: 160px">
          <template #body="{ data: item }">
            <span
              class="inline-flex items-center gap-1 rounded-md bg-slate-100 px-2 py-0.5 text-xs font-medium text-slate-700 border border-slate-200"
              data-testid="row-audience-badge"
            >
              {{ formatAudience(item.targetAudience) }}
            </span>
          </template>
        </Column>

        <Column header="Author" style="width: 160px">
          <template #body="{ data: item }">
            <span class="text-sm text-slate-700" data-testid="row-author-name">
              {{ item.authorName || 'Staff Member' }}
            </span>
          </template>
        </Column>

        <Column header="Date" style="width: 160px">
          <template #body="{ data: item }">
            <span class="text-xs text-slate-600" data-testid="row-date">
              {{ formatDisplayDateTime(item.publishedAt ?? item.createdAt) }}
            </span>
          </template>
        </Column>

        <Column header="Status" style="width: 120px">
          <template #body="{ data: item }">
            <StatusPill :status="item.status" data-testid="row-status-pill" />
          </template>
        </Column>

        <Column header="Actions" style="width: 130px" align-header="right">
          <template #body="{ data: item }">
            <div class="flex items-center justify-end gap-1">
              <Button
                type="button"
                severity="secondary"
                text
                rounded
                size="small"
                title="View Announcement"
                aria-label="View Announcement"
                data-testid="row-view-button"
                @click="openDetailDialog(item)"
              >
                <template #icon>
                  <Eye class="h-4 w-4" />
                </template>
              </Button>

              <Button
                v-if="canCreate && item.status === 'DRAFT'"
                type="button"
                severity="secondary"
                text
                rounded
                size="small"
                title="Edit Draft"
                aria-label="Edit Draft"
                data-testid="row-edit-button"
                @click="openEditDrawer(item)"
              >
                <template #icon>
                  <Pencil class="h-4 w-4" />
                </template>
              </Button>

              <Button
                v-if="canPublish && item.status === 'DRAFT'"
                type="button"
                severity="primary"
                text
                rounded
                size="small"
                title="Publish Announcement"
                aria-label="Publish Announcement"
                data-testid="row-publish-button"
                @click="promptPublish(item)"
              >
                <template #icon>
                  <Send class="h-4 w-4 text-brand-accent-600" />
                </template>
              </Button>
            </div>
          </template>
        </Column>
      </AppDataTable>

      <!-- Drawers & Dialogs -->
      <AnnouncementFormDrawer
        :visible="isDrawerVisible"
        :announcement="editingAnnouncement"
        @update:visible="isDrawerVisible = $event"
        @saved="() => refetch()"
      />

      <AnnouncementDetailDialog
        :visible="isDetailVisible"
        :announcement="selectedAnnouncement"
        :can-publish="canPublish"
        :can-edit="canCreate"
        @update:visible="isDetailVisible = $event"
        @publish="promptPublish"
        @edit="openEditDrawer"
      />

      <ConfirmActionDialog
        :visible="isPublishConfirmVisible"
        title="Publish Announcement"
        :message="publishConfirmMessage"
        confirm-label="Publish Announcement"
        cancel-label="Cancel"
        severity="primary"
        :loading="publishMutation.isPending.value"
        @update:visible="isPublishConfirmVisible = $event"
        @confirm="handleConfirmPublish"
        @cancel="isPublishConfirmVisible = false"
      />
    </div>
  </AppErrorBoundary>
</template>

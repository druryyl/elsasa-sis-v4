<script setup lang="ts">
import { computed } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import { Calendar, Send, User, Users } from 'lucide-vue-next';
import StatusPill from '@/shared/components/feedback/StatusPill.vue';
import { formatDisplayDateTime } from '@/shared/utils/dateUtils';
import type { AnnouncementDto } from '../api/announcements.api';

const props = withDefaults(
  defineProps<{
    visible: boolean;
    announcement: AnnouncementDto | null;
    canPublish?: boolean;
    canEdit?: boolean;
  }>(),
  {
    canPublish: false,
    canEdit: false
  }
);

const emit = defineEmits<{
  'update:visible': [value: boolean];
  publish: [announcement: AnnouncementDto];
  edit: [announcement: AnnouncementDto];
}>();

const isDraft = computed(() => props.announcement?.status?.toUpperCase() === 'DRAFT');

const audienceLabel = computed(() => {
  const raw = props.announcement?.targetAudience?.toUpperCase();
  switch (raw) {
    case 'ALL':
      return 'All (School-Wide)';
    case 'TEACHERS':
      return 'Teachers & Staff';
    case 'STUDENTS':
      return 'Students';
    case 'PARENTS':
      return 'Parents & Guardians';
    default:
      return raw ?? 'All';
  }
});

function handleClose(): void {
  emit('update:visible', false);
}

function handlePublish(): void {
  if (props.announcement) {
    emit('publish', props.announcement);
  }
}

function handleEdit(): void {
  if (props.announcement) {
    emit('edit', props.announcement);
  }
}
</script>

<template>
  <Dialog
    :visible="visible"
    modal
    :draggable="false"
    class="w-full max-w-2xl"
    data-testid="announcement-detail-dialog"
    @update:visible="emit('update:visible', $event)"
  >
    <template #header>
      <div class="flex items-center gap-3 pr-4">
        <div class="space-y-1">
          <div class="flex flex-wrap items-center gap-2">
            <span
              class="inline-flex items-center gap-1 rounded-md bg-brand-accent-50 px-2 py-0.5 text-xs font-medium text-brand-accent-700 border border-brand-accent-200"
              data-testid="dialog-audience-badge"
            >
              <Users class="h-3 w-3" />
              {{ audienceLabel }}
            </span>
            <StatusPill
              v-if="announcement?.status"
              :status="announcement.status"
              data-testid="dialog-status-pill"
            />
          </div>
          <h2 class="text-lg font-semibold text-slate-900" data-testid="dialog-title">
            {{ announcement?.title ?? 'Announcement Details' }}
          </h2>
        </div>
      </div>
    </template>

    <div v-if="announcement" class="space-y-5">
      <!-- Meta attributes bar -->
      <div
        class="flex flex-wrap items-center gap-y-2 gap-x-5 rounded-lg bg-slate-50 p-3 text-xs text-slate-600 border border-slate-200"
      >
        <div class="flex items-center gap-1.5" data-testid="dialog-author">
          <User class="h-3.5 w-3.5 text-slate-400" />
          <span>Author:</span>
          <strong class="font-medium text-slate-800">{{
            announcement.authorName || 'Staff Member'
          }}</strong>
        </div>

        <div v-if="announcement.academicYearName" class="flex items-center gap-1.5">
          <span class="text-slate-400">|</span>
          <span>Academic Year:</span>
          <strong class="font-medium text-slate-800">{{ announcement.academicYearName }}</strong>
        </div>

        <div class="flex items-center gap-1.5" data-testid="dialog-date">
          <span class="text-slate-400">|</span>
          <Calendar class="h-3.5 w-3.5 text-slate-400" />
          <span v-if="announcement.publishedAt">Published:</span>
          <span v-else>Created:</span>
          <strong class="font-medium text-slate-800">
            {{ formatDisplayDateTime(announcement.publishedAt ?? announcement.createdAt) }}
          </strong>
        </div>
      </div>

      <!-- Plain Text Content (NO v-html) -->
      <div
        class="min-h-[140px] rounded-lg border border-slate-100 bg-white p-4 text-sm text-slate-800 leading-relaxed whitespace-pre-wrap break-words font-sans selection:bg-brand-accent-100"
        data-testid="dialog-content-body"
      >
        {{ announcement.content }}
      </div>
    </div>

    <template #footer>
      <div class="flex w-full items-center justify-between border-t border-slate-200 pt-4">
        <div>
          <Button
            v-if="canEdit && isDraft"
            type="button"
            label="Edit Draft"
            severity="secondary"
            text
            size="small"
            data-testid="dialog-edit-button"
            @click="handleEdit"
          />
        </div>
        <div class="flex items-center gap-2">
          <Button
            type="button"
            label="Close"
            severity="secondary"
            text
            data-testid="dialog-close-button"
            @click="handleClose"
          />
          <Button
            v-if="canPublish && isDraft"
            type="button"
            label="Publish Announcement"
            icon="pi pi-send"
            data-testid="dialog-publish-button"
            @click="handlePublish"
          >
            <template #icon>
              <Send class="mr-1.5 h-4 w-4" />
            </template>
          </Button>
        </div>
      </div>
    </template>
  </Dialog>
</template>

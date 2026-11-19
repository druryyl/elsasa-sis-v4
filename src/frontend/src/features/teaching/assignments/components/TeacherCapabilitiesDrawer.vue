<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { useQuery } from '@tanstack/vue-query';
import Drawer from 'primevue/drawer';
import Button from 'primevue/button';
import Select from 'primevue/select';
import { AlertCircle, BookOpen, Plus, Trash2 } from 'lucide-vue-next';
import StatusPill from '@/shared/components/feedback/StatusPill.vue';
import { subjectsLookupQuery, type SubjectLookupItem } from '@/shared/services/lookups.api';
import { ApiError } from '@/shared/types/common';
import type { TeacherDto } from '../api/teachers.api';
import { useTeacherDetailQuery } from '../api/useTeacherQueries';
import {
  useAddCapabilityMutation,
  useRemoveCapabilityMutation
} from '../api/useTeacherMutations';

const props = withDefaults(
  defineProps<{
    visible: boolean;
    teacher: TeacherDto | null;
    canManage?: boolean;
  }>(),
  {
    canManage: true
  }
);

const emit = defineEmits<{
  'update:visible': [value: boolean];
  updated: [];
}>();

const teacherId = computed(() => props.teacher?.id);

// Fetch fresh teacher detail to ensure capabilities list is reactive to additions/removals
const { data: teacherDetail, isLoading: isDetailLoading } = useTeacherDetailQuery(teacherId, {
  enabled: computed(() => props.visible && Boolean(props.teacher?.id))
});

const currentCapabilities = computed(() => {
  if (teacherDetail.value?.capabilities) {
    return teacherDetail.value.capabilities;
  }
  return props.teacher?.capabilities ?? [];
});

// Authoritative subjects lookup from acd.subjects
const { data: allSubjects, isLoading: isSubjectsLoading } = useQuery({
  ...subjectsLookupQuery(),
  enabled: computed(() => props.visible)
});

const selectedSubjectId = ref<string>('');
const conflictBanner = ref<string | null>(null);

const addMutation = useAddCapabilityMutation();
const removeMutation = useRemoveCapabilityMutation();

const isMutating = computed(() => addMutation.isPending.value || removeMutation.isPending.value);

// Filter out subjects already tagged
const availableSubjects = computed<SubjectLookupItem[]>(() => {
  const existingIds = new Set(currentCapabilities.value.map((c) => c.subjectId));
  return (allSubjects.value ?? []).filter((s) => s.id && !existingIds.has(s.id));
});

watch(
  () => props.visible,
  (isOpen) => {
    if (isOpen) {
      selectedSubjectId.value = '';
      conflictBanner.value = null;
    }
  }
);

function closeDrawer(): void {
  if (isMutating.value) return;
  emit('update:visible', false);
}

async function handleAddCapability(): Promise<void> {
  if (!props.teacher?.id || !selectedSubjectId.value) return;
  conflictBanner.value = null;

  try {
    await addMutation.mutateAsync({
      teacherId: props.teacher.id,
      payload: { subjectId: selectedSubjectId.value }
    });
    selectedSubjectId.value = '';
    emit('updated');
  } catch (error) {
    if (error instanceof ApiError) {
      conflictBanner.value =
        error.detail || error.title || 'Failed to add teaching capability.';
    } else if (error instanceof Error) {
      conflictBanner.value = error.message;
    }
  }
}

async function handleRemoveCapability(subjectId?: string): Promise<void> {
  if (!props.teacher?.id || !subjectId) return;
  conflictBanner.value = null;

  try {
    await removeMutation.mutateAsync({
      teacherId: props.teacher.id,
      subjectId
    });
    emit('updated');
  } catch (error) {
    if (error instanceof ApiError) {
      conflictBanner.value =
        error.detail || error.title || 'Failed to remove teaching capability.';
    } else if (error instanceof Error) {
      conflictBanner.value = error.message;
    }
  }
}
</script>

<template>
  <Drawer
    :visible="visible"
    position="right"
    class="w-full sm:w-[500px]"
    :modal="true"
    :dismissable="!isMutating"
    data-testid="teacher-capabilities-drawer"
    @update:visible="closeDrawer"
  >
    <template #header>
      <div class="space-y-1">
        <div class="flex items-center gap-2">
          <BookOpen class="h-5 w-5 text-brand-accent-600" />
          <h2
            class="text-lg font-semibold text-slate-900"
            data-testid="teacher-capabilities-drawer-title"
          >
            Subject Capabilities
          </h2>
        </div>
        <p class="text-xs text-slate-500">
          Tag subjects this teacher is qualified to teach. Only qualified subjects may be assigned
          to study groups (BR-TCH-005).
        </p>
      </div>
    </template>

    <div class="space-y-6 py-2">
      <!-- Teacher Header Card -->
      <div
        v-if="teacher"
        class="rounded-lg border border-slate-200 bg-slate-50/70 p-3.5 space-y-1"
        data-testid="teacher-capabilities-header"
      >
        <div class="flex items-center justify-between">
          <span class="text-sm font-semibold text-slate-900">
            {{ teacher.fullName }}
          </span>
          <StatusPill :status="teacher.status ?? 'ACTIVE'" />
        </div>
        <div class="flex flex-wrap items-center gap-3 text-xs text-slate-500">
          <span v-if="teacher.nip">NIP: {{ teacher.nip }}</span>
          <span v-if="teacher.nik">NIK: {{ teacher.nik }}</span>
          <span v-if="teacher.phone">{{ teacher.phone }}</span>
        </div>
      </div>

      <!-- Conflict Banner -->
      <div
        v-if="conflictBanner"
        class="flex items-start gap-2.5 rounded-lg border border-amber-200 bg-amber-50 p-3 text-xs text-amber-900"
        role="alert"
        data-testid="capabilities-conflict-banner"
      >
        <AlertCircle class="mt-0.5 h-4 w-4 shrink-0 text-amber-600" />
        <div class="space-y-0.5">
          <p class="font-medium">Capability Warning</p>
          <p>{{ conflictBanner }}</p>
        </div>
      </div>

      <!-- Tag New Subject Section (Only if canManage) -->
      <div
        v-if="canManage"
        class="rounded-lg border border-slate-200 bg-white p-4 space-y-3 shadow-xs"
      >
        <span class="text-xs font-semibold uppercase tracking-wider text-slate-700">
          Tag Subject Qualification
        </span>
        <div class="flex items-center gap-2">
          <Select
            v-model="selectedSubjectId"
            :options="availableSubjects"
            option-label="name"
            option-value="id"
            placeholder="Select a subject from curriculum..."
            class="w-full text-sm"
            :loading="isSubjectsLoading"
            :disabled="isMutating || availableSubjects.length === 0"
            data-testid="subject-select"
          />
          <Button
            type="button"
            label="Tag"
            :disabled="!selectedSubjectId || isMutating"
            :loading="addMutation.isPending.value"
            data-testid="add-capability-button"
            @click="handleAddCapability"
          >
            <template #icon>
              <Plus class="mr-1 h-3.5 w-3.5" />
            </template>
          </Button>
        </div>
        <p v-if="availableSubjects.length === 0 && !isSubjectsLoading" class="text-xs text-slate-400">
          All curriculum subjects have been qualified for this teacher.
        </p>
      </div>

      <!-- Current Capabilities List -->
      <div class="space-y-3">
        <div class="flex items-center justify-between">
          <h3 class="text-xs font-semibold uppercase tracking-wider text-slate-700">
            Qualified Subjects ({{ currentCapabilities.length }})
          </h3>
          <span v-if="isDetailLoading" class="text-xs text-slate-400">Loading...</span>
        </div>

        <div
          v-if="currentCapabilities.length === 0 && !isDetailLoading"
          class="rounded-lg border border-dashed border-slate-200 p-6 text-center text-xs text-slate-500"
          data-testid="empty-capabilities"
        >
          No subject capabilities recorded yet. Use the selector above to tag qualified subjects.
        </div>

        <ul v-else class="divide-y divide-slate-100 rounded-lg border border-slate-200 bg-white shadow-xs">
          <li
            v-for="cap in currentCapabilities"
            :key="cap.id || cap.subjectId"
            class="flex items-center justify-between px-3.5 py-2.5 text-sm"
            :data-testid="`capability-item-${cap.subjectId}`"
          >
            <div class="flex items-center gap-2">
              <span class="inline-block h-2 w-2 rounded-full bg-brand-accent-500" />
              <span class="font-medium text-slate-800">
                {{ cap.subjectName }}
              </span>
            </div>

            <Button
              v-if="canManage"
              type="button"
              severity="danger"
              text
              rounded
              size="small"
              :disabled="isMutating"
              :loading="removeMutation.isPending.value && removeMutation.variables.value?.subjectId === cap.subjectId"
              :data-testid="`remove-capability-${cap.subjectId}`"
              title="Remove capability"
              @click="handleRemoveCapability(cap.subjectId)"
            >
              <template #icon>
                <Trash2 class="h-4 w-4 text-rose-500" />
              </template>
            </Button>
          </li>
        </ul>
      </div>

      <!-- Close Drawer Action -->
      <div class="flex justify-end border-t border-slate-200 pt-4">
        <Button
          type="button"
          label="Done"
          severity="secondary"
          :disabled="isMutating"
          data-testid="close-capabilities-button"
          @click="closeDrawer"
        />
      </div>
    </div>
  </Drawer>
</template>

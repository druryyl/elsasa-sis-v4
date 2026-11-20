<script setup lang="ts">
import { AlertCircle, User } from 'lucide-vue-next';
import type { PastoralStudentTarget } from '../schemas/pastoral.schema';

defineProps<{
  student: PastoralStudentTarget | null;
  serverError?: string | null;
}>();
</script>

<template>
  <div class="space-y-3">
    <div
      v-if="serverError"
      role="alert"
      data-testid="modal-server-error"
      class="flex items-start gap-2.5 rounded-lg border border-rose-200 bg-rose-50 px-3.5 py-2.5 text-sm text-rose-800"
    >
      <AlertCircle class="mt-0.5 h-4 w-4 shrink-0 text-rose-600" />
      <span>{{ serverError }}</span>
    </div>

    <div
      v-if="student"
      class="flex items-center justify-between gap-3 rounded-lg border border-slate-200 bg-slate-50 px-3.5 py-2.5 text-sm"
    >
      <div class="flex min-w-0 items-center gap-2.5">
        <div
          class="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-slate-200 text-slate-600"
        >
          <User class="h-4 w-4" />
        </div>
        <div class="min-w-0">
          <div class="truncate font-semibold text-slate-900">{{ student.fullName }}</div>
          <div class="text-xs text-slate-500">
            NIS: <span class="font-medium text-slate-700">{{ student.nis }}</span>
            <template v-if="student.studyGroupName">
              · Class <span class="font-medium text-slate-700">{{ student.studyGroupName }}</span>
            </template>
          </div>
        </div>
      </div>
      <span
        v-if="student.academicYearName"
        class="shrink-0 rounded-md border border-slate-200 bg-white px-2 py-0.5 text-xs font-medium text-slate-600"
      >
        {{ student.academicYearName }}
      </span>
    </div>
  </div>
</template>

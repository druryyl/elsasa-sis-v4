<script setup lang="ts">
import { computed } from 'vue';
import { User, School, BookOpen, Hash } from 'lucide-vue-next';
import type { StudentTimetableInfoDto } from '../api/studentPortal.api';

const props = defineProps<{
  studentInfo?: StudentTimetableInfoDto | null;
}>();

const studentName = computed(() => props.studentInfo?.fullName || 'Siswa Elsasa');
const nis = computed(() => props.studentInfo?.nis || '-');
const studyGroup = computed(() => props.studentInfo?.studyGroupName || '-');
const schoolLevel = computed(() => props.studentInfo?.schoolLevel || '-');
const grade = computed(() => props.studentInfo?.grade || '');
</script>

<template>
  <div
    data-testid="student-banner"
    class="rounded-xl border border-slate-200 bg-white p-5 shadow-xs transition sm:p-6"
  >
    <div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
      <div class="flex items-start gap-4">
        <div
          class="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-brand-primary-900 text-white shadow-xs"
        >
          <User class="h-6 w-6" />
        </div>
        <div>
          <h2
            data-testid="student-name"
            class="text-xl font-bold tracking-tight text-slate-900 sm:text-2xl"
          >
            {{ studentName }}
          </h2>
          <div class="mt-1 flex flex-wrap items-center gap-2 text-xs text-slate-500 sm:text-sm">
            <span class="inline-flex items-center gap-1 font-medium text-slate-700">
              <Hash class="h-3.5 w-3.5 text-slate-400" />
              <span>NIS:</span>
              <span data-testid="student-nis" class="font-mono">{{ nis }}</span>
            </span>
            <span class="text-slate-300">•</span>
            <span class="inline-flex items-center gap-1 font-medium text-slate-700">
              <School class="h-3.5 w-3.5 text-slate-400" />
              <span data-testid="student-school-level">{{ schoolLevel }}</span>
              <span v-if="grade">({{ grade }})</span>
            </span>
          </div>
        </div>
      </div>

      <div class="flex items-center gap-2 sm:self-center">
        <span
          data-testid="student-study-group"
          class="inline-flex items-center gap-1.5 rounded-lg bg-slate-100 px-3 py-1.5 text-xs font-semibold text-slate-800 ring-1 ring-inset ring-slate-200 sm:text-sm"
        >
          <BookOpen class="h-4 w-4 text-slate-500" />
          <span>Rombel: {{ studyGroup }}</span>
        </span>
      </div>
    </div>
  </div>
</template>

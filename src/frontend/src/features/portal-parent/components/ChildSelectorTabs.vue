<script setup lang="ts">
import { User, GraduationCap } from 'lucide-vue-next';
import type { LinkedChildDto } from '../api/parentPortal.api';

const props = withDefaults(
  defineProps<{
    children: LinkedChildDto[];
    selectedChildId: string | null;
    loading?: boolean;
  }>(),
  {
    loading: false
  }
);

const emit = defineEmits<{
  'update:selectedChildId': [childId: string];
  selectChild: [child: LinkedChildDto];
}>();

function handleSelect(child: LinkedChildDto): void {
  if (child.studentId && child.studentId !== props.selectedChildId) {
    emit('update:selectedChildId', child.studentId);
    emit('selectChild', child);
  }
}
</script>

<template>
  <div
    class="w-full space-y-2"
    data-testid="child-selector-tabs"
  >
    <div class="flex items-center justify-between">
      <span class="text-xs font-semibold uppercase tracking-wider text-slate-500">
        Pilih Anak
      </span>
      <span
        v-if="children.length > 1"
        class="text-xs text-slate-500"
      >
        {{ children.length }} anak terdaftar
      </span>
    </div>

    <!-- Skeleton loader -->
    <div
      v-if="loading"
      class="flex gap-3 overflow-x-auto pb-1"
      data-testid="child-tabs-skeleton"
    >
      <div
        v-for="i in 2"
        :key="i"
        class="h-16 w-52 shrink-0 animate-pulse rounded-xl bg-slate-200"
      />
    </div>

    <!-- Empty state -->
    <div
      v-else-if="children.length === 0"
      class="rounded-xl border border-dashed border-slate-300 p-4 text-center text-sm text-slate-500"
      data-testid="no-children-banner"
    >
      Tidak ada data anak yang terhubung dengan akun ini.
    </div>

    <!-- Horizontal Pill / Tab Selector -->
    <div
      v-else
      class="flex gap-3 overflow-x-auto pb-2 pt-1 no-scrollbar"
      role="tablist"
      aria-label="Pilihan Anak"
    >
      <button
        v-for="child in children"
        :key="child.studentId"
        type="button"
        role="tab"
        :aria-selected="child.studentId === selectedChildId"
        :data-testid="`child-tab-${child.studentId}`"
        class="group relative flex min-w-[200px] shrink-0 items-center gap-3 rounded-xl border p-3 text-left transition-all duration-200 focus:outline-none focus-visible:ring-2 focus-visible:ring-brand-accent-500"
        :class="[
          child.studentId === selectedChildId
            ? 'border-brand-primary-600 bg-brand-primary-50/80 shadow-sm ring-1 ring-brand-primary-600'
            : 'border-slate-200 bg-white hover:border-slate-300 hover:bg-slate-50'
        ]"
        @click="handleSelect(child)"
      >
        <!-- Child Avatar / Icon -->
        <div
          class="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg transition-colors"
          :class="[
            child.studentId === selectedChildId
              ? 'bg-brand-primary-600 text-white'
              : 'bg-slate-100 text-slate-600 group-hover:bg-slate-200'
          ]"
        >
          <User class="h-5 w-5" />
        </div>

        <!-- Child Info -->
        <div class="min-w-0 flex-1">
          <p
            class="truncate text-sm font-semibold"
            :class="[
              child.studentId === selectedChildId
                ? 'text-brand-primary-950'
                : 'text-slate-900'
            ]"
          >
            {{ child.fullName || child.nickname || 'Siswa' }}
          </p>

          <div class="mt-0.5 flex flex-wrap items-center gap-1.5 text-xs text-slate-500">
            <span
              v-if="child.studyGroupName"
              class="inline-flex items-center gap-0.5 font-medium text-slate-700"
            >
              <GraduationCap class="h-3 w-3 text-slate-400" />
              {{ child.studyGroupName }}
            </span>
            <span
              v-if="child.grade"
              class="rounded bg-slate-100 px-1.5 py-0.2 text-[10px] font-medium text-slate-600"
            >
              {{ child.grade }}
            </span>
            <span
              v-if="child.schoolLevel"
              class="rounded bg-slate-100 px-1.5 py-0.2 text-[10px] font-medium text-slate-600"
            >
              {{ child.schoolLevel }}
            </span>
          </div>
        </div>

        <!-- Active Indicator Dot -->
        <div
          v-if="child.studentId === selectedChildId"
          class="absolute -right-1 -top-1 h-3 w-3 rounded-full border-2 border-white bg-brand-primary-600"
          aria-hidden="true"
        />
      </button>
    </div>
  </div>
</template>

<style scoped>
.no-scrollbar::-webkit-scrollbar {
  display: none;
}
.no-scrollbar {
  -ms-overflow-style: none;
  scrollbar-width: none;
}
</style>

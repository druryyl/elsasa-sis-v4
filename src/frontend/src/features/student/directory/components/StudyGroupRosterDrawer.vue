<script setup lang="ts">
import { computed } from 'vue';
import Drawer from 'primevue/drawer';
import Button from 'primevue/button';
import Column from 'primevue/column';
import DataTable from 'primevue/datatable';
import { Phone, Users } from 'lucide-vue-next';
import EmptyState from '@/shared/components/feedback/EmptyState.vue';
import StatusPill from '@/shared/components/feedback/StatusPill.vue';
import { useStudyGroupRosterQuery } from '../api/useStudentQueries';

const props = withDefaults(
  defineProps<{
    visible: boolean;
    studyGroupId: string | null;
    canManagePlacements?: boolean;
  }>(),
  {
    canManagePlacements: true
  }
);

const emit = defineEmits<{
  'update:visible': [value: boolean];
  placeStudent: [studyGroupId: string];
}>();

const { data: roster, isLoading } = useStudyGroupRosterQuery(
  () => props.studyGroupId,
  { enabled: () => props.visible && Boolean(props.studyGroupId) }
);

const students = computed(() => roster.value?.students ?? []);
const totalCount = computed(() => roster.value?.studentCount ?? students.value.length);

function closeDrawer(): void {
  emit('update:visible', false);
}

function handlePlaceClick(): void {
  if (props.studyGroupId) {
    emit('placeStudent', props.studyGroupId);
  }
}
</script>

<template>
  <Drawer
    :visible="visible"
    position="right"
    class="!w-full sm:!w-[760px] md:!w-[880px]"
    @update:visible="closeDrawer"
  >
    <template #header>
      <div class="flex items-center gap-2">
        <Users class="h-5 w-5 text-brand-accent-600" />
        <div>
          <h2 class="text-base font-semibold text-slate-900">
            {{ roster?.name ? `Class Roster — ${roster.name}` : 'Class Roster' }}
          </h2>
          <p class="text-xs text-slate-500">
            {{ roster?.academicYearName ? `${roster.academicYearName} • ` : '' }}
            {{ roster?.schoolLevel ? `${roster.schoolLevel} Grade ${roster.grade}` : '' }}
            {{ roster?.homeroomTeacherName ? ` • Wali: ${roster.homeroomTeacherName}` : '' }}
          </p>
        </div>
      </div>
    </template>

    <div class="flex h-full flex-col justify-between">
      <div class="space-y-4 overflow-y-auto pr-1">
        <!-- Roster Summary Bar -->
        <div class="flex items-center justify-between rounded-lg border border-slate-200 bg-slate-50 p-3 text-xs">
          <div class="flex items-center gap-3">
            <span class="font-medium text-slate-700">Enrolled Students:</span>
            <span class="inline-flex items-center rounded-full bg-brand-accent-50 px-2.5 py-0.5 text-xs font-semibold text-brand-accent-700">
              {{ totalCount }} students
            </span>
          </div>

          <Button
            v-if="canManagePlacements"
            size="small"
            icon="pi pi-plus"
            label="Place Student Here"
            @click="handlePlaceClick"
          />
        </div>

        <!-- Roster Table with Guardian Emergency Contacts -->
        <DataTable
          :value="students"
          :loading="isLoading"
          data-key="id"
          responsive-layout="scroll"
          striped-rows
          class="text-xs"
        >
          <template #empty>
            <div class="py-8">
              <EmptyState
                title="No students placed yet"
                description="This study group has no enrolled students for this academic year."
                variant="empty"
              />
            </div>
          </template>

          <!-- Student Identifier -->
          <Column header="NIS / NISN" class="w-36">
            <template #body="{ data }">
              <div class="font-mono font-medium text-slate-900">{{ data.nis }}</div>
              <div v-if="data.nisn" class="font-mono text-[11px] text-slate-500">
                NISN: {{ data.nisn }}
              </div>
            </template>
          </Column>

          <!-- Student Name & Demographics -->
          <Column header="Student Name" class="min-w-44">
            <template #body="{ data }">
              <div class="font-semibold text-slate-900">{{ data.fullName }}</div>
              <div class="text-[11px] text-slate-500">
                {{ data.gender === 'MALE' ? 'Laki-laki (L)' : 'Perempuan (P)' }}
                <span v-if="data.nickname"> • "{{ data.nickname }}"</span>
              </div>
            </template>
          </Column>

          <!-- Status -->
          <Column header="Status" class="w-24">
            <template #body="{ data }">
              <StatusPill :status="data.status ?? 'ACTIVE'" />
            </template>
          </Column>

          <!-- Guardian Emergency Contacts (UC-HRM-05 prominent display) -->
          <Column header="Guardian Emergency Contact" class="min-w-64">
            <template #body="{ data }">
              <div v-if="data.guardians && data.guardians.length > 0" class="space-y-1.5 py-1">
                <div
                  v-for="(guardian, idx) in data.guardians"
                  :key="guardian.id ?? idx"
                  class="flex items-start gap-1.5 rounded border border-slate-100 bg-slate-50/70 p-1.5"
                >
                  <Phone class="mt-0.5 h-3.5 w-3.5 shrink-0 text-brand-accent-600" />
                  <div class="min-w-0 flex-1">
                    <div class="flex items-center gap-1.5">
                      <span class="font-medium text-slate-900">{{ guardian.fullName }}</span>
                      <span class="rounded bg-slate-200 px-1 py-0.2 text-[9px] font-semibold text-slate-700">
                        {{ guardian.relationshipType }}
                      </span>
                    </div>
                    <div class="font-mono font-semibold text-brand-neutral-900">
                      {{ guardian.phone || 'No phone recorded' }}
                    </div>
                  </div>
                </div>
              </div>
              <div v-else class="italic text-slate-400">
                No guardian on file
              </div>
            </template>
          </Column>
        </DataTable>
      </div>

      <div class="mt-4 flex justify-end border-t border-slate-200 pt-3">
        <Button label="Close" severity="secondary" text @click="closeDrawer" />
      </div>
    </div>
  </Drawer>
</template>

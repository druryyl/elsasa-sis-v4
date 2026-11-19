<script setup lang="ts">
import { computed, ref } from 'vue';
import Drawer from 'primevue/drawer';
import Button from 'primevue/button';
import Tabs from 'primevue/tabs';
import TabList from 'primevue/tablist';
import Tab from 'primevue/tab';
import TabPanels from 'primevue/tabpanels';
import TabPanel from 'primevue/tabpanel';
import { GraduationCap, MapPin, Phone, Mail, User } from 'lucide-vue-next';
import StatusPill from '@/shared/components/feedback/StatusPill.vue';
import EmptyState from '@/shared/components/feedback/EmptyState.vue';
import { formatDisplayDate } from '@/shared/utils/dateUtils';
import { useStudentDetailQuery } from '../api/useStudentQueries';
import StudentEditDrawer from './StudentEditDrawer.vue';
import GuardianManagerPanel from './GuardianManagerPanel.vue';
import PlaceStudentModal from './PlaceStudentModal.vue';

const props = withDefaults(
  defineProps<{
    visible: boolean;
    studentId: string | null;
    canManage?: boolean;
    canPlace?: boolean;
  }>(),
  {
    canManage: true,
    canPlace: true
  }
);

const emit = defineEmits<{
  'update:visible': [value: boolean];
  changed: [];
}>();

const { data: studentDetail, isLoading, refetch } = useStudentDetailQuery(
  () => props.studentId,
  { enabled: () => props.visible && Boolean(props.studentId) }
);

const activeTab = ref('demographics');
const isEditDrawerOpen = ref(false);
const isPlaceModalOpen = ref(false);

const student = computed(() => studentDetail.value ?? null);
const guardians = computed(() => student.value?.guardians ?? []);
const placements = computed(() => student.value?.placements ?? []);

function closeDrawer(): void {
  emit('update:visible', false);
}

function handleSaved(): void {
  refetch();
  emit('changed');
}

function handlePlaced(): void {
  refetch();
  emit('changed');
}
</script>

<template>
  <Drawer
    :visible="visible"
    position="right"
    class="!w-full sm:!w-[700px] md:!w-[800px]"
    @update:visible="closeDrawer"
  >
    <template #header>
      <div class="flex items-center gap-3">
        <div class="flex h-10 w-10 items-center justify-center rounded-full bg-brand-accent-100 text-brand-accent-700">
          <User class="h-5 w-5" />
        </div>
        <div>
          <div class="flex items-center gap-2">
            <h2 class="text-base font-semibold text-slate-900">
              {{ student?.fullName ?? 'Student Dossier' }}
            </h2>
            <StatusPill v-if="student?.status" :status="student.status" />
          </div>
          <div class="text-xs text-slate-500">
            NIS: <span class="font-mono font-medium text-slate-800">{{ student?.nis ?? '—' }}</span>
            <span v-if="student?.nisn"> • NISN: <span class="font-mono">{{ student.nisn }}</span></span>
          </div>
        </div>
      </div>
    </template>

    <div v-if="isLoading" class="flex h-64 items-center justify-center">
      <div class="text-xs text-slate-500">Loading student dossier...</div>
    </div>

    <div v-else-if="!student" class="py-12">
      <EmptyState
        title="Student not found"
        description="The requested student profile could not be loaded."
        variant="empty"
      />
    </div>

    <div v-else class="flex h-full flex-col justify-between">
      <div class="overflow-y-auto pr-1">
        <!-- Sub-Tabs: Demographics, Guardians, Placements -->
        <Tabs v-model:value="activeTab">
          <TabList class="mb-4">
            <Tab value="demographics">Profile &amp; Demographics</Tab>
            <Tab value="guardians">
              Guardians &amp; Contacts ({{ guardians.length }})
            </Tab>
            <Tab value="placements">
              Placement History ({{ placements.length }})
            </Tab>
          </TabList>

          <TabPanels>
            <!-- Tab 1: Profile & Demographics -->
            <TabPanel value="demographics" class="space-y-5 pt-1">
              <div class="flex items-center justify-between">
                <h3 class="text-sm font-semibold text-slate-800">Student Personal Details</h3>
                <Button
                  v-if="canManage"
                  size="small"
                  icon="pi pi-pencil"
                  label="Edit Profile"
                  severity="secondary"
                  @click="isEditDrawerOpen = true"
                />
              </div>

              <!-- Identification & Bio Grid -->
              <div class="grid grid-cols-1 gap-4 rounded-lg border border-slate-200 bg-white p-4 shadow-xs sm:grid-cols-2">
                <div>
                  <div class="text-xs text-slate-500">Full Legal Name</div>
                  <div class="text-sm font-medium text-slate-900">{{ student.fullName }}</div>
                </div>

                <div>
                  <div class="text-xs text-slate-500">Nickname</div>
                  <div class="text-sm font-medium text-slate-900">{{ student.nickname || '—' }}</div>
                </div>

                <div>
                  <div class="text-xs text-slate-500">Date of Birth</div>
                  <div class="text-sm font-medium text-slate-900">
                    {{ student.dateOfBirth ? formatDisplayDate(student.dateOfBirth) : '—' }}
                  </div>
                </div>

                <div>
                  <div class="text-xs text-slate-500">Gender</div>
                  <div class="text-sm font-medium text-slate-900">
                    {{ student.gender === 'MALE' ? 'Male (Laki-laki)' : 'Female (Perempuan)' }}
                  </div>
                </div>

                <div>
                  <div class="text-xs text-slate-500">National ID (NISN)</div>
                  <div class="font-mono text-sm font-medium text-slate-900">{{ student.nisn || 'Not Registered' }}</div>
                </div>

                <div>
                  <div class="text-xs text-slate-500">Enrollment Date</div>
                  <div class="text-sm font-medium text-slate-900">
                    {{ student.enrollmentDate ? formatDisplayDate(student.enrollmentDate) : '—' }}
                  </div>
                </div>
              </div>

              <!-- Contact & Residence Info -->
              <div class="rounded-lg border border-slate-200 bg-white p-4 shadow-xs">
                <h4 class="mb-3 text-xs font-semibold uppercase tracking-wider text-slate-500">Contact &amp; Residence</h4>
                <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
                  <div class="flex items-start gap-2">
                    <Phone class="mt-0.5 h-4 w-4 text-slate-400" />
                    <div>
                      <div class="text-xs text-slate-500">Student Phone</div>
                      <div class="text-sm font-medium text-slate-800">{{ student.phone || '—' }}</div>
                    </div>
                  </div>

                  <div class="flex items-start gap-2">
                    <Mail class="mt-0.5 h-4 w-4 text-slate-400" />
                    <div>
                      <div class="text-xs text-slate-500">Student Email</div>
                      <div class="text-sm font-medium text-slate-800">{{ student.email || '—' }}</div>
                    </div>
                  </div>

                  <div class="sm:col-span-2 flex items-start gap-2">
                    <MapPin class="mt-0.5 h-4 w-4 text-slate-400" />
                    <div>
                      <div class="text-xs text-slate-500">Home Address</div>
                      <div class="text-sm text-slate-800">{{ student.address || 'No address recorded' }}</div>
                    </div>
                  </div>
                </div>
              </div>
            </TabPanel>

            <!-- Tab 2: Guardians -->
            <TabPanel value="guardians" class="pt-1">
              <GuardianManagerPanel
                :student-id="student.id!"
                :guardians="guardians"
                :can-manage="canManage"
                @changed="handleSaved"
              />
            </TabPanel>

            <!-- Tab 3: Placements -->
            <TabPanel value="placements" class="space-y-4 pt-1">
              <div class="flex items-center justify-between border-b border-slate-200 pb-3">
                <div>
                  <h3 class="text-sm font-semibold text-slate-800">Class Placement History</h3>
                  <p class="text-xs text-slate-500">Cohort study group allocations per academic year (BR-STU-004).</p>
                </div>

                <Button
                  v-if="canPlace"
                  size="small"
                  icon="pi pi-plus"
                  label="Place in Study Group"
                  @click="isPlaceModalOpen = true"
                />
              </div>

              <div v-if="placements.length === 0" class="rounded-lg border border-dashed border-slate-300 p-6 text-center">
                <GraduationCap class="mx-auto h-8 w-8 text-slate-400" />
                <p class="mt-2 text-xs text-slate-500">No study group placements recorded for this student.</p>
              </div>

              <div v-else class="space-y-3">
                <div
                  v-for="(placement, idx) in placements"
                  :key="placement.id ?? idx"
                  class="flex items-center justify-between rounded-lg border border-slate-200 bg-white p-4 shadow-xs"
                >
                  <div class="space-y-1">
                    <div class="flex items-center gap-2">
                      <span class="text-sm font-semibold text-slate-900">
                        {{ placement.studyGroupName ? `Class ${placement.studyGroupName}` : 'Unassigned Class' }}
                      </span>
                      <span class="rounded bg-slate-100 px-2 py-0.5 text-xs font-medium text-slate-700">
                        {{ placement.schoolLevel }} Grade {{ placement.grade }}
                      </span>
                      <span v-if="idx === 0" class="rounded bg-emerald-50 px-2 py-0.5 text-xs font-semibold text-emerald-700">
                        Current
                      </span>
                    </div>
                    <div class="text-xs text-slate-600">
                      Academic Year: <span class="font-medium text-slate-800">{{ placement.academicYearName }}</span>
                      <span v-if="placement.homeroomTeacherName">
                        • Wali Kelas: <span class="font-medium text-slate-800">{{ placement.homeroomTeacherName }}</span>
                      </span>
                    </div>
                  </div>
                </div>
              </div>
            </TabPanel>
          </TabPanels>
        </Tabs>
      </div>

      <div class="mt-4 flex justify-end border-t border-slate-200 pt-3">
        <Button label="Close" severity="secondary" text @click="closeDrawer" />
      </div>
    </div>

    <!-- Edit Student Profile Drawer -->
    <StudentEditDrawer
      v-model:visible="isEditDrawerOpen"
      :student="student"
      @saved="handleSaved"
    />

    <!-- Quick Place Student Modal -->
    <PlaceStudentModal
      v-if="student?.id"
      v-model:visible="isPlaceModalOpen"
      :student-id="student.id"
      :student-name="student.fullName"
      @placed="handlePlaced"
    />
  </Drawer>
</template>

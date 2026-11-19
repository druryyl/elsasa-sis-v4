<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { toTypedSchema } from '@vee-validate/zod';
import { useForm } from 'vee-validate';
import Drawer from 'primevue/drawer';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import { AlertCircle } from 'lucide-vue-next';
import FormField from '@/shared/components/forms/FormField.vue';
import StatusPill from '@/shared/components/feedback/StatusPill.vue';
import { ApiError } from '@/shared/types/common';
import type { RoomDto } from '../api/rooms.api';
import { useCreateRoomMutation, useUpdateRoomMutation } from '../api/useRoomMutations';
import { roomSchema, type RoomFormValues } from '../schemas/room.schema';

const props = withDefaults(
  defineProps<{
    visible: boolean;
    room?: RoomDto | null;
  }>(),
  {
    room: null
  }
);

const emit = defineEmits<{
  'update:visible': [value: boolean];
  saved: [room: RoomDto];
}>();

const isEditMode = computed(() => Boolean(props.room?.id));
const drawerTitle = computed(() => (isEditMode.value ? 'Edit Room' : 'Add Room'));

const createMutation = useCreateRoomMutation();
const updateMutation = useUpdateRoomMutation();

const isSubmitting = computed(
  () => createMutation.isPending.value || updateMutation.isPending.value
);

const conflictBanner = ref<string | null>(null);

function buildInitialValues(room?: RoomDto | null): RoomFormValues {
  if (room) {
    return {
      name: room.name ?? '',
      roomType: (room.roomType as 'CLASSROOM' | 'LABORATORY' | 'HALL' | 'OTHER') || 'CLASSROOM',
      capacity: room.capacity ?? 30
    };
  }

  return {
    name: '',
    roomType: 'CLASSROOM',
    capacity: 30
  };
}

const { defineField, errors, handleSubmit, resetForm, setErrors } = useForm<RoomFormValues>({
  validationSchema: toTypedSchema(roomSchema),
  initialValues: buildInitialValues(props.room)
});

const [name, nameAttrs] = defineField('name');
const [roomType, roomTypeAttrs] = defineField('roomType');
const [capacity, capacityAttrs] = defineField('capacity');

watch(
  () => [props.visible, props.room] as const,
  ([isOpen, room]) => {
    if (isOpen) {
      conflictBanner.value = null;
      resetForm({
        values: buildInitialValues(room)
      });
    }
  }
);

function closeDrawer(): void {
  if (isSubmitting.value) return;
  emit('update:visible', false);
}

function mapValidationErrors(fieldErrors: Record<string, string[]>): Record<string, string> {
  const mapped: Record<string, string> = {};
  for (const [key, messages] of Object.entries(fieldErrors)) {
    const first = messages[0];
    if (first) {
      mapped[key] = first;
    }
  }
  return mapped;
}

const onSubmit = handleSubmit(async (values) => {
  conflictBanner.value = null;

  try {
    let saved: RoomDto;
    if (isEditMode.value && props.room?.id) {
      saved = await updateMutation.mutateAsync({
        id: props.room.id,
        payload: {
          name: values.name.trim(),
          roomType: values.roomType,
          capacity: Number(values.capacity)
        }
      });
    } else {
      saved = await createMutation.mutateAsync({
        name: values.name.trim(),
        roomType: values.roomType,
        capacity: Number(values.capacity)
      });
    }

    emit('saved', saved);
    emit('update:visible', false);
  } catch (error) {
    if (error instanceof ApiError) {
      if (error.status === 422 && error.errors) {
        setErrors(mapValidationErrors(error.errors));
        if (error.detail && Object.keys(error.errors).length === 0) {
          conflictBanner.value = error.detail;
        }
        return;
      }

      if (error.status === 409 || error.status === 422 || error.status >= 400) {
        conflictBanner.value = error.detail || error.title;
        return;
      }
    }

    conflictBanner.value = 'Unable to save room. Please try again.';
  }
});
</script>

<template>
  <Drawer
    :visible="visible"
    position="right"
    :header="drawerTitle"
    :dismissable="!isSubmitting"
    :closable="!isSubmitting"
    class="w-full sm:w-[480px]"
    data-testid="room-drawer"
    @update:visible="emit('update:visible', $event)"
  >
    <form id="room-form" class="space-y-6" novalidate @submit.prevent="onSubmit">
      <!-- Conflict Banner (e.g. 409 Duplicate Room Name) -->
      <div
        v-if="conflictBanner"
        role="alert"
        data-testid="drawer-conflict-alert"
        class="flex items-start gap-2.5 rounded-lg border border-rose-200 bg-rose-50 px-3.5 py-3 text-sm text-rose-800"
      >
        <AlertCircle class="mt-0.5 h-4 w-4 shrink-0 text-rose-600" />
        <span>{{ conflictBanner }}</span>
      </div>

      <div class="space-y-4">
        <div v-if="isEditMode && room?.roomType" class="flex items-center justify-between">
          <h3 class="text-sm font-semibold text-slate-900">Room Details</h3>
          <StatusPill :status="room.roomType" />
        </div>

        <!-- Room Name -->
        <FormField
          label="Room Name / Identifier"
          field-id="room-name"
          required
          :error="errors.name"
          hint="Example: Classroom 7A, Science Lab 1, Main Hall"
        >
          <InputText
            id="room-name"
            v-model="name"
            v-bind="nameAttrs"
            placeholder="Classroom 7A"
            class="w-full"
            :invalid="Boolean(errors.name)"
            :disabled="isSubmitting"
            data-testid="room-name-input"
          />
        </FormField>

        <!-- Room Type -->
        <FormField
          label="Room Classification"
          field-id="room-type"
          required
          :error="errors.roomType"
          hint="Functional category of the physical facility"
        >
          <select
            id="room-type"
            v-model="roomType"
            v-bind="roomTypeAttrs"
            class="w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm text-slate-800 shadow-sm focus:border-brand-accent-500 focus:outline-none focus:ring-1 focus:ring-brand-accent-500"
            :disabled="isSubmitting"
            data-testid="room-type-select"
          >
            <option value="CLASSROOM">Classroom</option>
            <option value="LABORATORY">Laboratory</option>
            <option value="HALL">Hall / Auditorium</option>
            <option value="OTHER">Other Facility</option>
          </select>
        </FormField>

        <!-- Capacity -->
        <FormField
          label="Student / Seat Capacity"
          field-id="room-capacity"
          required
          :error="errors.capacity"
          hint="Maximum capacity (must be greater than 0)"
        >
          <input
            id="room-capacity"
            v-model.number="capacity"
            v-bind="capacityAttrs"
            type="number"
            min="1"
            placeholder="30"
            class="w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm text-slate-800 shadow-sm focus:border-brand-accent-500 focus:outline-none focus:ring-1 focus:ring-brand-accent-500"
            :disabled="isSubmitting"
            data-testid="room-capacity-input"
          />
        </FormField>
      </div>
    </form>

    <template #footer>
      <div class="flex w-full items-center justify-end gap-2 border-t border-slate-200 pt-4">
        <Button
          type="button"
          label="Cancel"
          severity="secondary"
          text
          :disabled="isSubmitting"
          @click="closeDrawer"
        />
        <Button
          type="submit"
          form="room-form"
          :label="isEditMode ? 'Save Changes' : 'Create Room'"
          :loading="isSubmitting"
          :disabled="isSubmitting"
          data-testid="submit-room-button"
        />
      </div>
    </template>
  </Drawer>
</template>

import { z } from 'zod';

export const ROOM_TYPES = ['CLASSROOM', 'LABORATORY', 'HALL', 'OTHER'] as const;
export type RoomType = (typeof ROOM_TYPES)[number];

export const roomSchema = z.object({
  name: z
    .string()
    .trim()
    .min(1, 'Room name is required')
    .max(100, 'Room name must be 100 characters or fewer'),
  roomType: z.enum(ROOM_TYPES, {
    errorMap: () => ({ message: 'Room type must be CLASSROOM, LABORATORY, HALL, or OTHER' })
  }),
  capacity: z.coerce
    .number({ invalid_type_error: 'Capacity must be a number' })
    .int('Capacity must be a whole number')
    .positive('Capacity must be greater than zero')
});

export type RoomFormValues = z.infer<typeof roomSchema>;

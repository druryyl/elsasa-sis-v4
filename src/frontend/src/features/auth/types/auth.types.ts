import type { components } from '@/shared/types/api/schema';
import type { UserSession } from '@/shared/services/authSession';

export type { UserSession };
export type AuthUserDto = components['schemas']['AuthUserDto'];
export type LoginRequestDto = components['schemas']['LoginRequest'];
export type LoginResponseDto = components['schemas']['LoginResponse'];
export type RefreshResponseDto = components['schemas']['RefreshResponse'];

export interface JwtClaims {
  sub: string;
  preferred_username: string;
  name: string;
  roles: string[];
  permissions: string[];
  teacherId?: string;
  studentId?: string;
  guardianId?: string;
  exp: number;
}

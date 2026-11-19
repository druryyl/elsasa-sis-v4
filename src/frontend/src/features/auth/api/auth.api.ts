import { apiClient } from '@/shared/services/http';
import type {
  AuthUserDto,
  LoginRequestDto,
  LoginResponseDto,
  RefreshResponseDto,
  UserSession
} from '../types/auth.types';

export function mapAuthUserDtoToSession(dto: AuthUserDto): UserSession {
  return {
    id: dto.id ?? '',
    username: dto.username ?? '',
    displayName: dto.displayName ?? dto.username ?? '',
    email: dto.email ?? null,
    roles: dto.roles ?? [],
    permissions: dto.permissions ?? [],
    teacherId: dto.teacherId ?? null,
    studentId: dto.studentId ?? null,
    guardianId: dto.guardianId ?? null
  };
}

export const authApi = {
  async login(payload: LoginRequestDto): Promise<LoginResponseDto> {
    return (await apiClient.post('/auth/login', payload)) as unknown as LoginResponseDto;
  },

  async refresh(): Promise<RefreshResponseDto> {
    return (await apiClient.post('/auth/refresh')) as unknown as RefreshResponseDto;
  },

  async logout(): Promise<void> {
    await apiClient.post('/auth/logout');
  },

  async me(): Promise<AuthUserDto> {
    return (await apiClient.get('/auth/me')) as unknown as AuthUserDto;
  }
};

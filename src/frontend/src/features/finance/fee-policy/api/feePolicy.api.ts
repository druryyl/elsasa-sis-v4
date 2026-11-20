import { apiClient } from '@/shared/services/http';
import type { components, paths } from '@/shared/types/api/schema';
import type {
  ChargeTypeListParams,
  DiscountStudentSearchParams,
  StudentDiscountsParams
} from './keys';

export type ChargeTypeDto = components['schemas']['ChargeTypeDto'];
export type CreateChargeTypeRequest = components['schemas']['CreateChargeTypeRequest'];
export type UpdateChargeTypeRequest = components['schemas']['UpdateChargeTypeRequest'];
export type StudentDiscountDto = components['schemas']['StudentDiscountDto'];
export type CreateStudentDiscountRequest = components['schemas']['CreateStudentDiscountRequest'];
export type StudentLookupItem = components['schemas']['StudentListItemDto'];

type GetChargeTypesQuery = paths['/api/fin/charge-types']['get']['parameters']['query'];
type GetDiscountsQuery = paths['/api/fin/discounts']['get']['parameters']['query'];
type GetStudentsQuery = paths['/api/std/students']['get']['parameters']['query'];

export const feePolicyApi = {
  async listChargeTypes(params?: ChargeTypeListParams): Promise<ChargeTypeDto[]> {
    const query: GetChargeTypesQuery = {
      AcademicYearId: params?.academicYearId || undefined,
      Frequency: params?.frequency || undefined,
      Status: params?.status || undefined
    };

    return (await apiClient.get('/fin/charge-types', {
      params: query
    })) as unknown as ChargeTypeDto[];
  },

  async createChargeType(payload: CreateChargeTypeRequest): Promise<ChargeTypeDto> {
    return (await apiClient.post('/fin/charge-types', payload)) as unknown as ChargeTypeDto;
  },

  async updateChargeType(id: string, payload: UpdateChargeTypeRequest): Promise<ChargeTypeDto> {
    return (await apiClient.put(`/fin/charge-types/${id}`, payload)) as unknown as ChargeTypeDto;
  },

  async activateChargeType(id: string): Promise<ChargeTypeDto> {
    return (await apiClient.post(`/fin/charge-types/${id}/activate`)) as unknown as ChargeTypeDto;
  },

  async listStudentDiscounts(params?: StudentDiscountsParams): Promise<StudentDiscountDto[]> {
    const query: GetDiscountsQuery = {
      AcademicYearId: params?.academicYearId || undefined,
      StudentId: params?.studentId || undefined,
      ChargeTypeId: params?.chargeTypeId || undefined,
      IsActive: params?.isActive
    };

    return (await apiClient.get('/fin/discounts', {
      params: query
    })) as unknown as StudentDiscountDto[];
  },

  async createStudentDiscount(payload: CreateStudentDiscountRequest): Promise<StudentDiscountDto> {
    return (await apiClient.post('/fin/discounts', payload)) as unknown as StudentDiscountDto;
  },

  async revokeStudentDiscount(id: string): Promise<StudentDiscountDto> {
    return (await apiClient.post(`/fin/discounts/${id}/revoke`)) as unknown as StudentDiscountDto;
  },

  async searchStudents(params?: DiscountStudentSearchParams): Promise<StudentLookupItem[]> {
    const query: GetStudentsQuery = {
      AcademicYearId: params?.academicYearId || undefined,
      Status: 'ACTIVE',
      Search: params?.search?.trim() || undefined,
      Page: 1,
      PageSize: 50,
      SortField: 'FullName',
      SortOrder: 'asc'
    };

    const response = (await apiClient.get('/std/students', {
      params: query
    })) as unknown as components['schemas']['StudentListItemDtoPaginatedResponse'];

    return response?.items ?? [];
  }
};

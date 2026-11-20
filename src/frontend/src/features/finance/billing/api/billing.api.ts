import { apiClient } from '@/shared/services/http';
import type { components, paths } from '@/shared/types/api/schema';
import type { BillingChargesParams, BillingStudentSearchParams } from './keys';

export type StudentChargeDto = components['schemas']['StudentChargeDto'];
export type BatchBillingPreviewRequest = components['schemas']['BatchBillingPreviewRequest'];
export type BatchBillingPreviewResponse = components['schemas']['BatchBillingPreviewResponse'];
export type BatchBillingPreviewItemDto = components['schemas']['BatchBillingPreviewItemDto'];
export type GenerateBatchTuitionChargesRequest =
  components['schemas']['GenerateBatchTuitionChargesRequest'];
export type GenerateBatchTuitionChargesResponse =
  components['schemas']['GenerateBatchTuitionChargesResponse'];
export type CreateAdHocChargeRequest = components['schemas']['CreateAdHocChargeRequest'];
export type CancelStudentChargeRequest = components['schemas']['CancelStudentChargeRequest'];
export type ChargeTypeDto = components['schemas']['ChargeTypeDto'];
export type StudentLookupItem = components['schemas']['StudentListItemDto'];

type GetChargesQuery = paths['/api/fin/charges']['get']['parameters']['query'];
type GetChargeTypesQuery = paths['/api/fin/charge-types']['get']['parameters']['query'];
type GetStudentsQuery = paths['/api/std/students']['get']['parameters']['query'];

export const billingApi = {
  async listCharges(params?: BillingChargesParams): Promise<StudentChargeDto[]> {
    const query: GetChargesQuery = {
      AcademicYearId: params?.academicYearId || undefined,
      BillingMonth: (params?.billingMonth ?? params?.month)?.trim() || undefined,
      SchoolLevel: params?.schoolLevel || undefined,
      Grade: params?.grade || undefined,
      StudyGroupId: params?.studyGroupId || undefined,
      StudentId: params?.studentId || undefined,
      Status: params?.status || undefined
    };

    return (await apiClient.get('/fin/charges', {
      params: query
    })) as unknown as StudentChargeDto[];
  },

  async previewBatchBilling(
    payload: BatchBillingPreviewRequest
  ): Promise<BatchBillingPreviewResponse> {
    return (await apiClient.post(
      '/fin/charges/batch-preview',
      payload
    )) as unknown as BatchBillingPreviewResponse;
  },

  async generateBatchBilling(
    payload: GenerateBatchTuitionChargesRequest
  ): Promise<GenerateBatchTuitionChargesResponse> {
    return (await apiClient.post(
      '/fin/charges/generate-batch',
      payload
    )) as unknown as GenerateBatchTuitionChargesResponse;
  },

  async createAdHocCharge(payload: CreateAdHocChargeRequest): Promise<StudentChargeDto> {
    return (await apiClient.post('/fin/charges/adhoc', payload)) as unknown as StudentChargeDto;
  },

  async cancelCharge(id: string, payload: CancelStudentChargeRequest): Promise<StudentChargeDto> {
    return (await apiClient.post(
      `/fin/charges/${id}/cancel`,
      payload
    )) as unknown as StudentChargeDto;
  },

  async listActiveChargeTypes(academicYearId?: string): Promise<ChargeTypeDto[]> {
    const query: GetChargeTypesQuery = {
      AcademicYearId: academicYearId || undefined,
      Status: 'ACTIVE'
    };

    return (await apiClient.get('/fin/charge-types', {
      params: query
    })) as unknown as ChargeTypeDto[];
  },

  async searchStudents(params?: BillingStudentSearchParams): Promise<StudentLookupItem[]> {
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

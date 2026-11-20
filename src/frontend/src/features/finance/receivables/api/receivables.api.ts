import { apiClient } from '@/shared/services/http';
import type { components, paths } from '@/shared/types/api/schema';
import type {
  DailyCashierParams,
  ReceivablesAgingParams,
  ReceivablesSummaryParams
} from './keys';

export type ReceivablesSummaryDto = components['schemas']['ReceivablesSummaryDto'];
export type ReceivablesSummaryByLevelDto = components['schemas']['ReceivablesSummaryByLevelDto'];
export type ReceivablesAgingDto = components['schemas']['ReceivablesAgingDto'];
export type ReceivablesAgingSummaryDto = components['schemas']['ReceivablesAgingSummaryDto'];
export type ReceivablesAgingStudentItemDto =
  components['schemas']['ReceivablesAgingStudentItemDto'];
export type DailyCashierSummaryDto = components['schemas']['DailyCashierSummaryDto'];
export type PaymentMethodSummaryDto = components['schemas']['PaymentMethodSummaryDto'];
export type DailyCashierReceiptItemDto = components['schemas']['DailyCashierReceiptItemDto'];
export type StudentStatementDto = components['schemas']['StudentStatementDto'];
export type StudentStatementChargeDto = components['schemas']['StudentStatementChargeDto'];
export type StudentStatementReceiptDto = components['schemas']['StudentStatementReceiptDto'];
export type StudentStatementAllocationDto = components['schemas']['StudentStatementAllocationDto'];

type ReceivablesSummaryQuery =
  paths['/api/fin/reports/receivables-summary']['get']['parameters']['query'];
type ReceivablesAgingQuery =
  paths['/api/fin/reports/receivables-aging']['get']['parameters']['query'];
type DailyCashierQuery = paths['/api/fin/reports/daily-cashier']['get']['parameters']['query'];

export const receivablesApi = {
  async getReceivablesSummary(params?: ReceivablesSummaryParams): Promise<ReceivablesSummaryDto> {
    const query: ReceivablesSummaryQuery = {
      academicYearId: params?.academicYearId || undefined
    };

    return (await apiClient.get('/fin/reports/receivables-summary', {
      params: query
    })) as unknown as ReceivablesSummaryDto;
  },

  async getReceivablesAging(params?: ReceivablesAgingParams): Promise<ReceivablesAgingDto> {
    const query: ReceivablesAgingQuery = {
      academicYearId: params?.academicYearId || undefined,
      schoolLevel: params?.schoolLevel || undefined,
      grade: params?.grade || undefined,
      studyGroupId: params?.studyGroupId || undefined,
      asOfDate: params?.asOfDate || undefined
    };

    return (await apiClient.get('/fin/reports/receivables-aging', {
      params: query
    })) as unknown as ReceivablesAgingDto;
  },

  async getDailyCashierSummary(params: DailyCashierParams): Promise<DailyCashierSummaryDto> {
    const query: DailyCashierQuery = {
      date: params.date || undefined
    };

    return (await apiClient.get('/fin/reports/daily-cashier', {
      params: query
    })) as unknown as DailyCashierSummaryDto;
  },

  async getStudentStatement(studentId: string): Promise<StudentStatementDto> {
    return (await apiClient.get(
      `/fin/students/${studentId}/statement`
    )) as unknown as StudentStatementDto;
  }
};

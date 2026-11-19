export interface ProblemDetailsPayload {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errorCode?: string;
  errors?: Record<string, string[]>;
  extensions?: {
    errorCode?: string;
    errors?: Record<string, string[]>;
  };
}

export class ApiError extends Error {
  public readonly status: number;
  public readonly title: string;
  public readonly detail: string;
  public readonly errorCode?: string;
  public readonly errors?: Record<string, string[]>;

  constructor(
    status: number,
    title: string,
    detail: string,
    errorCode?: string,
    errors?: Record<string, string[]>
  ) {
    super(detail || title);
    this.name = 'ApiError';
    this.status = status;
    this.title = title;
    this.detail = detail;
    this.errorCode = errorCode;
    this.errors = errors;
  }

  static fromProblemDetails(
    status: number,
    payload?: ProblemDetailsPayload | null,
    fallbackMessage = 'An unexpected error occurred'
  ): ApiError {
    const title = payload?.title || 'An unexpected error occurred';
    const detail = payload?.detail || fallbackMessage;
    const errorCode = payload?.errorCode ?? payload?.extensions?.errorCode;
    const errors = payload?.errors ?? payload?.extensions?.errors;
    return new ApiError(status, title, detail, errorCode, errors);
  }
}

export interface PagedQuery {
  pageNumber?: number;
  pageSize?: number;
  search?: string;
  sortBy?: string;
  sortDescending?: boolean;
}

export interface PaginatedResponse<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

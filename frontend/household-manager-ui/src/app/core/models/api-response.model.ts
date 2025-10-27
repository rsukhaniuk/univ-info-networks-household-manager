export interface ApiResponse<T = any> {
  success: boolean;
  message?: string;
  data?: T;
  errors?: { [key: string]: string[] };
  timestamp: Date;
}

export interface PagedResult<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface BaseQueryParameters {
  page?: number;             // Page number (default: 1)
  pageSize?: number;         // Page size (default: 20, max: 100)
  sortBy?: string;           // Sort field
  sortOrder?: 'asc' | 'desc'; // Sort direction (default: 'desc')
  search?: string;           // Search term
}


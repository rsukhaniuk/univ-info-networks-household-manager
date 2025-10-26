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

